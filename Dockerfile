# ---- build ----------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Copy only the project files first so `restore` is cached until a dependency changes.
COPY CarLookup.slnx .
COPY src/CarLookup.Web/CarLookup.Web.csproj src/CarLookup.Web/
COPY tests/CarLookup.Tests/CarLookup.Tests.csproj tests/CarLookup.Tests/
RUN dotnet restore

COPY . .
RUN dotnet publish src/CarLookup.Web/CarLookup.Web.csproj \
    --configuration Release \
    --no-restore \
    --output /app

# ---- runtime --------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# curl is not in the runtime image but the container health check needs it.
RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# APP_UID is the non-root user that ships with the Microsoft runtime images.
USER $APP_UID

HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD curl --fail --silent http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "CarLookup.Web.dll"]
