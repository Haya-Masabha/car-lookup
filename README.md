# Car Lookup

A full-stack web application that lets you pick a **car make** and a **model year**, then shows the
**vehicle types** that manufacturer builds and the **models** it offered that year.

**Angular 21** front end, **ASP.NET Core 10** Web API, both containerised and served behind nginx.
All vehicle data comes from the public [NHTSA vPIC API](https://vpic.nhtsa.dot.gov/api/).

### ▶ Live demo: **http://3.122.193.216**

Running on an AWS EC2 instance in `eu-central-1`. Plain HTTP — no TLS certificate is configured,
so use `http://` rather than `https://`.

---

## Contents

- [What it does](#what-it-does)
- [Architecture](#architecture)
- [Run it with Docker](#run-it-with-docker) — the quickest way
- [Run it for development](#run-it-for-development)
- [Running the tests](#running-the-tests)
- [API reference](#api-reference)
- [Deploying to AWS free tier](#deploying-to-aws-free-tier)
- [Configuration](#configuration)

---

## What it does

1. You start typing a make. vPIC publishes **over 12,000 makes**, far too many for a dropdown, so
   the API searches server-side and returns a short ranked list — exact match first, then names
   that start with the term, then names that contain it.
2. Picking a make loads the **vehicle types** that manufacturer builds.
3. Choosing a model year — and optionally narrowing to one vehicle type — lists the matching
   **models**.

## Architecture

```
browser
   │
   ▼
┌──────────────────────────┐     /api/*      ┌──────────────────────────┐      ┌────────────┐
│  web container           │ ──────────────► │  api container           │ ───► │ NHTSA vPIC │
│  nginx + Angular bundle  │                 │  ASP.NET Core Web API    │      └────────────┘
└──────────────────────────┘                 └──────────────────────────┘
```

nginx serves the compiled Angular bundle and reverse-proxies `/api` to the API container, so the
browser only ever talks to one origin and production needs no CORS configuration.

| Concern | Choice |
| --- | --- |
| Front end | Angular 21 standalone components, signals for state, RxJS for debounced search |
| Back end | ASP.NET Core 10 Web API, OpenAPI description in development |
| Upstream calls | Typed `HttpClient` with retry, timeout and circuit breaker (`Microsoft.Extensions.Http.Resilience`) |
| Caching | `IMemoryCache` decorator around the catalog service — makes for 24 h, per-make lookups for 1 h |
| Tests | 22 xUnit tests, 10 Vitest tests, none needing network access |
| Containers | One image per tier, non-root API, health checks on both |

```
src/CarLookup.Api            ASP.NET Core Web API
├── Controllers/             VehiclesController
├── Services/                vPIC client, caching decorator, make ranking
├── Models/                  domain records and vPIC wire contracts
└── Infrastructure/          maps upstream outages onto a 503 response

src/carlookup-web            Angular application
├── src/app/vehicle-catalog.ts   typed API client
├── src/app/make-picker/         debounced type-ahead component
└── nginx.conf                   static hosting plus the /api proxy

tests/CarLookup.Tests        backend test suite
```

---

## Run it with Docker

**Prerequisite:** [Docker Desktop](https://www.docker.com/products/docker-desktop/) (or Docker
Engine on Linux). Nothing else — you need neither the .NET SDK nor Node.js.

```bash
git clone https://github.com/Haya-Masabha/car-lookup.git
cd car-lookup
docker compose up --build
```

Then open **<http://localhost:8080>**.

The first build takes a few minutes while it downloads the .NET SDK and Node images. When both
containers report `healthy` the app is ready. The API is also published on
<http://localhost:5000> so it can be called directly.

To stop everything:

```bash
docker compose down
```

---

## Run it for development

Useful when you want hot reload. Run the two tiers in **separate terminals**.

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) and
[Node.js 22.12+](https://nodejs.org/).

Terminal 1 — the API:

```bash
dotnet run --project src/CarLookup.Api
```

It listens on `http://localhost:5122`.

Terminal 2 — the Angular dev server:

```bash
cd src/carlookup-web
npm install
npm start
```

Open **<http://localhost:4200>**. The dev server proxies `/api` to the API
(`src/carlookup-web/proxy.conf.json`), so the front-end code is identical in development and in
production.

---

## Running the tests

Backend (22 tests):

```bash
dotnet test
```

Front end (10 tests):

```bash
cd src/carlookup-web
npm test
```

Both suites run offline — upstream responses are served by a stub `HttpMessageHandler` on the
backend and by `HttpTestingController` on the front end.

---

## API reference

| Method | Route | Purpose |
| --- | --- | --- |
| `GET` | `/api/vehicles/makes?query=hon&limit=25` | Ranked make search |
| `GET` | `/api/vehicles/makes/{makeId}/vehicle-types` | Vehicle types for a make |
| `GET` | `/api/vehicles/makes/{makeId}/models?year=2015&vehicleType=Truck` | Models for a make and year |
| `GET` | `/api/vehicles/years` | Selectable model years |
| `GET` | `/health` | Liveness probe used by Docker and AWS |

Example:

```bash
curl "http://localhost:5000/api/vehicles/makes/474/models?year=2015&vehicleType=Truck"
```

```json
[{ "modelId": 1866, "modelName": "Ridgeline", "vehicleTypeName": "Truck" }]
```

Invalid input returns an RFC 7807 problem response (`400`), and an upstream vPIC outage returns
`503` rather than an error page. In development the OpenAPI description is served at
`/openapi/v1.json`.

---

## Deploying to AWS free tier

See **[docs/aws-deployment.md](docs/aws-deployment.md)** for the full step-by-step walkthrough.

---

## Configuration

API settings live under the `Vpic` section of `appsettings.json` and can be overridden with
environment variables, which is how they are set in Docker and on the server:

| Setting | Environment variable | Default |
| --- | --- | --- |
| Upstream base URL | `Vpic__BaseUrl` | `https://vpic.nhtsa.dot.gov/api/vehicles/` |
| Upstream timeout | `Vpic__Timeout` | `00:00:30` |
| Make list cache lifetime | `Vpic__MakesCacheDuration` | `24:00:00` |
| Per-make lookup cache lifetime | `Vpic__LookupCacheDuration` | `01:00:00` |

`Cors__AllowedOrigins__0` allows an origin to call the API cross-site. It is set to
`http://localhost:4200` in development only; in Docker the nginx proxy makes requests same-origin,
so no origin is allowed by default.
