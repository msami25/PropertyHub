# PropertyHub

PropertyHub is a local multi-user property marketplace built with a .NET 8 Web API, React 19 with
Vite SSR, EF Core, SQL Server, and Docker Compose.

## Current status

Phases 1 through 5 provide the layered foundation, JWT authentication and authorization, City CRUD,
Property CRUD, and secure local property images. Public property list and detail pages are
server-rendered and hydrated; owners manage only their listings and images, and Admins approve or
reject pending submissions.

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

## Property image API

| Method | Endpoint | Access |
|---|---|---|
| `POST` | `/api/properties/{propertyId}/images` | Active owner; multipart field `images` |
| `PUT` | `/api/properties/{propertyId}/images/{imageId}/primary` | Active owner |
| `DELETE` | `/api/properties/{propertyId}/images/{imageId}` | Active owner |
| `GET` | `/api/properties/{propertyId}/images/{imageId}` | Public listing, owner, or Admin |

Create the property first, then upload one to five JPEG, PNG, or WebP images of at most 5 MB each.
The API checks extension, declared MIME type, magic bytes, successful decode, dimensions (at most
8,000 pixels per side and 40 megapixels), count, ownership, and listing state. It generates storage
names, stores files outside the web root, and streams them with access control and
`X-Content-Type-Options: nosniff`. The first upload is primary. Changing images returns an approved
listing to `Pending`; an Admin cannot approve a listing without an image, and the last image cannot
be deleted.

The React owner page supports upload, primary selection, and deletion. Public cards/details and the
Admin moderation page display the appropriate image set. Runtime files persist in the
`propertyhub-uploads` Docker volume and remain excluded from Git.

## Foundation verification

```powershell
dotnet restore PropertyHub.sln
dotnet build PropertyHub.sln --no-restore
dotnet test PropertyHub.sln --no-build

npm.cmd ci --prefix frontend/propertyhub-web
npm.cmd run test --prefix frontend/propertyhub-web
npm.cmd run build --prefix frontend/propertyhub-web

docker compose --env-file .env.example config --quiet
powershell -NoProfile -ExecutionPolicy Bypass -File tests/Invoke-Phase5DockerUat.ps1
```

## Docker

The local application URL is `http://localhost:3000`, with the API at `http://localhost:8081`.
Docker startup, automatic migrations, seeded roles/Admin/cities, and the Phase 2–3 authentication
and City journeys were previously verified. On 2026-07-29, the Phase 4 images built successfully,
all three services became healthy, the Property migration applied to the preserved SQL volume, and
the live Property API and public SSR journeys passed. A direct-route SSR serialization defect found
during that UAT was fixed and reverified through the production web container. Phase 5 then rebuilt
only API/web, applied the image-dimension migration, and passed the live secure-upload, moderation,
SSR display, controlled retrieval, deletion, and upload-volume persistence journey.

The final startup command will be:

```powershell
docker compose up --build
```

## Coverage and UAT

Backend coverage must reach at least 80% after mandatory features are implemented. Generated
migrations may be excluded. Coverage evidence will be stored under `docs/coverage/`; executed
acceptance results will be recorded in `docs/UAT_REPORT.md`.

## Known limitations

See `docs/SCOPE_CUTS.md`. Open-Meteo, user administration, dashboard metrics, contact reveal, and
enquiries remain future slices. Background retention cleanup for hidden image files remains
deferred until mandatory Gate slices are stable.
