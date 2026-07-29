# PropertyHub

PropertyHub is a local multi-user property marketplace built with a .NET 8 Web API, React 19 with
Vite SSR, EF Core, SQL Server, and Docker Compose.

## Current status

Phases 1 through 4 provide the layered foundation, JWT authentication and authorization, City CRUD,
and Property CRUD with ownership, moderation, availability, and public visibility rules. Public
property list and detail pages are server-rendered and hydrated; owners manage only their listings,
and Admins approve or reject pending submissions.

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

## City API

| Method | Endpoint | Access |
|---|---|---|
| `GET` | `/api/cities` | Public; active cities only |
| `GET` | `/api/admin/cities` | Active Admin |
| `GET` | `/api/admin/cities/{cityId}` | Active Admin |
| `POST` | `/api/admin/cities` | Active Admin |
| `PUT` | `/api/admin/cities/{cityId}` | Active Admin |
| `DELETE` | `/api/admin/cities/{cityId}` | Active Admin |

City names are trimmed and unique after invariant normalization. Names must contain 2–100
characters, latitude must be between -90 and 90, and longitude must be between -180 and 180.
Deleting a city referenced by a property returns `409 Conflict`. The migration seeds Lahore,
Karachi, Islamabad, Rawalpindi, Faisalabad, Multan, Peshawar, and Quetta.

After signing in as the configured Admin, open `http://localhost:3000/admin/cities` to use the City
management UI.

## Property API

| Method | Endpoint | Access |
|---|---|---|
| `GET` | `/api/properties` | Public; approved and available only |
| `GET` | `/api/properties/{propertyId}` | Public; approved and available only |
| `GET` | `/api/users/me/properties` | Active owner |
| `GET` | `/api/users/me/properties/{propertyId}` | Active owner; owner-scoped |
| `POST` | `/api/properties` | Active user |
| `PUT` | `/api/properties/{propertyId}` | Active owner |
| `PATCH` | `/api/properties/{propertyId}/availability` | Active owner |
| `DELETE` | `/api/properties/{propertyId}` | Active owner |
| `GET` | `/api/admin/properties` | Active Admin |
| `POST` | `/api/admin/properties/{propertyId}/moderation` | Active Admin |

New and edited listings are `Pending`. Admins may set `Approved` or `Rejected`; rejection requires a
reason. Owners may mark sale listings `Sold` and rental listings `Rented`. Sold, rented,
soft-deleted, unapproved, and disabled-owner listings are excluded from public results. Owner
lookups are scoped by the authenticated user so another owner's identifier returns `404`.

Essential public filters support City, purpose, and property type. Use `/my/properties` for owner
management and `/admin/properties` for moderation.

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

The local application URL is `http://localhost:3000`, with the API at `http://localhost:8081`.
Docker startup, automatic migrations, seeded roles/Admin/cities, and the Phase 2–3 authentication
and City journeys were previously verified. On 2026-07-29, the Phase 4 images built successfully,
all three services became healthy, the Property migration applied to the preserved SQL volume, and
the live Property API and public SSR journeys passed. The live UAT still found a private-route SSR
defect recorded in `docs/UAT_REPORT.md`.

The final startup command will be:

```powershell
docker compose up --build
```

## Coverage and UAT

Backend coverage must reach at least 80% after mandatory features are implemented. Generated
migrations may be excluded. Coverage evidence will be stored under `docs/coverage/`; executed
acceptance results will be recorded in `docs/UAT_REPORT.md`.

## Known limitations

See `docs/SCOPE_CUTS.md`. Property cards currently use an accessible placeholder because secure
property images are Phase 5. Open-Meteo, user administration, dashboard metrics, contact reveal,
and enquiries remain future slices. Direct browser requests to frontend account routes such as
`/login`, `/register`, `/my/properties`, and `/admin/properties` currently return HTTP 500;
client-side navigation to those routes works after entering through a public SSR page.
