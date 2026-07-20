# Car Lookup

A small ASP.NET Core web application that lets you pick a **car make** and a **model year**, then
shows the **vehicle types** that manufacturer builds and the **models** it offered that year.

All vehicle data comes from the public
[NHTSA vPIC API](https://vpic.nhtsa.dot.gov/api/).

---

## Contents

- [What it does](#what-it-does)
- [How it is built](#how-it-is-built)
- [Run it locally with Docker](#run-it-locally-with-docker) — the quickest way
- [Run it locally with the .NET SDK](#run-it-locally-with-the-net-sdk)
- [Running the tests](#running-the-tests)
- [Internal API](#internal-api)
- [Deploying to AWS free tier](#deploying-to-aws-free-tier)
- [Configuration](#configuration)

---

## What it does

1. You start typing a make. vPIC publishes **over 12,000 makes**, which is far too many for a
   dropdown, so the app searches server-side and returns a short ranked list (exact match first,
   then names that start with what you typed, then names that contain it).
2. Picking a make loads the **vehicle types** that manufacturer builds.
3. Choosing a model year — and optionally narrowing to one vehicle type — lists the matching
   **models**.

## How it is built

| Concern | Choice |
| --- | --- |
| Framework | ASP.NET Core 10 MVC |
| Upstream calls | Typed `HttpClient` with retry, timeout and circuit breaker (`Microsoft.Extensions.Http.Resilience`) |
| Caching | `IMemoryCache` decorator around the catalog service — makes for 24 h, per-make lookups for 1 h |
| Front end | Razor view plus dependency-free JavaScript (debounced, keyboard-navigable combobox) |
| Tests | xUnit, 22 tests, no network access required |
| Container | Multi-stage Dockerfile, non-root user, `/health` health check |

```
src/CarLookup.Web
├── Controllers/     VehiclesController (JSON API) and HomeController (page)
├── Services/        vPIC HTTP client, caching decorator, make ranking
├── Models/          domain records and the vPIC wire contracts
├── Infrastructure/  maps upstream outages onto a 503 response
└── wwwroot/         css/site.css and js/car-lookup.js
tests/CarLookup.Tests
```

The browser never calls vPIC directly. Routing every call through the app means responses can be
cached, the upstream contract lives in one place, and the app does not depend on vPIC's CORS policy.

---

## Run it locally with Docker

**Prerequisite:** [Docker Desktop](https://www.docker.com/products/docker-desktop/) (or Docker
Engine on Linux). Nothing else — you do not need the .NET SDK.

```bash
git clone https://github.com/<your-account>/<your-repo>.git
cd <your-repo>
docker compose up --build
```

Then open **<http://localhost:8080>**.

To stop it:

```bash
docker compose down
```

<details>
<summary>Without Docker Compose</summary>

```bash
docker build -t carlookup:latest .
docker run --rm -p 8080:8080 carlookup:latest
```

</details>

---

## Run it locally with the .NET SDK

**Prerequisite:** [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
git clone https://github.com/<your-account>/<your-repo>.git
cd <your-repo>
dotnet run --project src/CarLookup.Web
```

The console prints the URL it is listening on (by default `http://localhost:5122`).

---

## Running the tests

```bash
dotnet test
```

The suite runs entirely offline: upstream responses are served by a stub `HttpMessageHandler`.

---

## Internal API

The page is driven by these endpoints, which are also usable on their own:

| Method | Route | Purpose |
| --- | --- | --- |
| `GET` | `/api/vehicles/makes?query=hon&limit=25` | Ranked make search |
| `GET` | `/api/vehicles/makes/{makeId}/vehicle-types` | Vehicle types for a make |
| `GET` | `/api/vehicles/makes/{makeId}/models?year=2015&vehicleType=Truck` | Models for a make and year |
| `GET` | `/api/vehicles/years` | Selectable model years |
| `GET` | `/health` | Liveness probe used by Docker and AWS |

Example:

```bash
curl "http://localhost:8080/api/vehicles/makes/474/models?year=2015&vehicleType=Truck"
```

```json
[{ "modelId": 1866, "modelName": "Ridgeline", "vehicleTypeName": "Truck" }]
```

Invalid input returns an RFC 7807 problem response (`400`), and an upstream vPIC outage returns
`503` rather than an error page.

---

## Deploying to AWS free tier

See **[docs/aws-deployment.md](docs/aws-deployment.md)** for the full step-by-step walkthrough.

---

## Configuration

Settings live under the `Vpic` section of `appsettings.json` and can be overridden with environment
variables, which is how they are set in Docker and on the server:

| Setting | Environment variable | Default |
| --- | --- | --- |
| Upstream base URL | `Vpic__BaseUrl` | `https://vpic.nhtsa.dot.gov/api/vehicles/` |
| Upstream timeout | `Vpic__Timeout` | `00:00:30` |
| Make list cache lifetime | `Vpic__MakesCacheDuration` | `24:00:00` |
| Per-make lookup cache lifetime | `Vpic__LookupCacheDuration` | `01:00:00` |
