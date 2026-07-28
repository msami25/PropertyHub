# PropertyHub

PropertyHub is a local multi-user property marketplace built with a .NET 8 Web API, React 19 with
Vite SSR, EF Core, SQL Server, and Docker Compose.

## Current status

Phases 1 and 2 provide the layered foundation plus registration, JWT login, `User`/`Admin` roles,
configured Admin seeding, database-backed disabled-account enforcement, protected API policies,
and browser-memory authentication with protected route states. City and Property CRUD are not
implemented yet.

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

The JWT signing key must contain at least 32 characters. Public registration always creates a
`User`; it never accepts a role. The configured seed account receives `Admin`. JWTs expire after 30
minutes and are held only in React memory, so a browser reload requires sign-in again.

## Authentication API

| Method | Endpoint | Access |
|---|---|---|
| `POST` | `/api/auth/register` | Public, rate limited |
| `POST` | `/api/auth/login` | Public, rate limited |
| `GET` | `/api/auth/me` | Active authenticated user |
| `GET` | `/api/admin/session` | Active Admin only |

Missing or invalid JWTs return `401`. Authenticated Users receive `403` on Admin endpoints.
Disabled accounts receive `403` at login and on protected requests, including requests made with a
token issued before disablement.

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
`http://localhost:8081`. Docker configuration is validated, and the API now applies migrations and
seeds roles/Admin during startup. Full-stack startup and UAT will be verified after mandatory
feature slices are complete.

The final startup command will be:

```powershell
docker compose up --build
```

## Coverage and UAT

Backend coverage must reach at least 80% after mandatory features are implemented. Generated
migrations may be excluded. Coverage evidence will be stored under `docs/coverage/`; executed
acceptance results will be recorded in `docs/UAT_REPORT.md`.

## Known limitations

See `docs/SCOPE_CUTS.md`. City/Property CRUD, image uploads, Open-Meteo, user administration,
dashboard metrics, moderation, and enquiries remain future slices.
