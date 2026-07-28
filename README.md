# PropertyHub

PropertyHub is a local multi-user property marketplace built with a .NET 8 Web API, React 19 with
Vite SSR, EF Core, SQL Server, and Docker Compose.

## Current status

Phase 1 provides the project foundation: layered projects, the initial database schema, health
checks, SSR application shell, automated smoke tests, and Docker configuration. Authentication and
feature workflows are intentionally not implemented yet.

## Architecture

Feature requests will follow this dependency direction:

```text
React/Vite SSR
    -> API controllers
        -> application services
            -> domain-specific repository interfaces
                -> EF Core repository implementations
                    -> SQL Server
```

Controllers must not use EF Core or repositories directly. The API is the only process allowed to
access SQL Server and the uploads volume.

## Prerequisites

- Docker Desktop with Docker Compose
- .NET 8 SDK for host development
- Node.js 24 and npm for host frontend development

The checked-in projects target `net8.0`. A newer SDK may build them, while the container build uses
the .NET 8 SDK explicitly.

## Configuration

Copy `.env.example` to `.env` and replace every example secret before starting containers. Never
commit `.env`. Key settings cover SQL Server, JWT signing, the seeded administrator, local image
storage, Open-Meteo, CORS, and the internal/public API URLs.

## Foundation verification

```powershell
dotnet restore PropertyHub.sln
dotnet build PropertyHub.sln --no-restore
dotnet test PropertyHub.sln --no-build

npm.cmd ci --prefix frontend/propertyhub-web
npm.cmd run test --prefix frontend/propertyhub-web
npm.cmd run build --prefix frontend/propertyhub-web

docker compose --env-file .env.example config --quiet
```

## Docker

The planned local application URL is `http://localhost:3000`, with the API at
`http://localhost:8080`. Phase 1 validates the Docker configuration only; full-stack startup and UAT
will be verified after mandatory feature slices and migration startup are complete.

The final startup command will be:

```powershell
docker compose up --build
```

## Coverage and UAT

Backend coverage must reach at least 80% after mandatory features are implemented. Generated
migrations may be excluded. Coverage evidence will be stored under `docs/coverage/`; executed
acceptance results will be recorded in `docs/UAT_REPORT.md`.

## Known limitations

See `docs/SCOPE_CUTS.md`. Phase 1 contains no authentication, CRUD endpoints, upload workflow,
Open-Meteo call, admin dashboard, moderation, or enquiry functionality.
