# PropertyHub Architecture

## Purpose

PropertyHub is a local three-container marketplace. React/Vite SSR renders public discovery pages,
the browser hydrates them and drives authenticated workflows, the .NET API owns all business and
security decisions, and SQL Server and protected upload volumes retain state.

The architecture follows the permanent dependency rule:

```text
React/Vite SSR frontend
    -> ASP.NET Core controllers
        -> application services
            -> domain-specific repository interfaces
                -> EF Core repository implementations
                    -> SQL Server
```

## High-level components

```mermaid
flowchart TB
    User["Visitor, User, or Admin"]
    Browser["Browser UI and hydration"]
    Web["React 19 + Vite SSR<br/>Express web container"]
    Api["ASP.NET Core 8 Web API<br/>api container"]
    Controllers["Controllers<br/>HTTP and status mapping"]
    Services["Application services<br/>use cases and business rules"]
    Repositories["Domain-specific repositories<br/>persistence contracts"]
    Infrastructure["Infrastructure<br/>EF Core, Identity, files, HTTP client"]
    Sql["SQL Server 2022"]
    Uploads[("Protected uploads")]
    Weather["Open-Meteo"]

    User --> Browser
    Browser -->|"public HTML"| Web
    Browser -->|"hydrated API requests"| Api
    Web -->|"public SSR API requests"| Api
    Api --> Controllers
    Controllers --> Services
    Services --> Repositories
    Repositories --> Infrastructure
    Infrastructure --> Sql
    Infrastructure --> Uploads
    Infrastructure --> Weather
```

### Frontend

- `frontend/propertyhub-web/src/App.tsx` defines the route tree and client navigation.
- Public `/`, `/properties`, and `/properties/{id}` pages receive server-loaded public DTOs and
  hydrate in the browser.
- `/my/properties`, `/admin`, `/admin/properties`, and `/admin/cities` load protected data only
  after an in-memory authenticated session exists.
- Dedicated API modules under `src/api` isolate browser and SSR HTTP concerns.
- React never connects to SQL Server or the upload volume.

### API and application layers

- Controllers accept DTOs, interpret authenticated claims, call services, and map outcomes to
  RESTful HTTP responses and Problem Details.
- Services contain validation, ownership, moderation, availability, account safety, weather, and
  image use cases.
- Repository interfaces are domain-specific: Property, PropertyImage, City, account, and Admin
  user management.
- Infrastructure implements repositories with EF Core and ASP.NET Core Identity.
- `ApplicationDbContext` is the unit of work and applies entity configurations from the
  Infrastructure assembly.

### Cross-cutting infrastructure

- ASP.NET Core Identity hashes passwords and stores roles.
- JWT bearer authentication validates issuer, audience, signature, expiry, and zero clock skew.
- ActiveUser authorization checks account status and token version against SQL Server.
- Global exception handling returns Problem Details without leaking internal paths or stack traces.
- Fixed-window rate limits protect login, registration, and uploads.
- Health checks separate process liveness from SQL-backed readiness.
- `HttpClientFactory`, memory caching, logging, and configuration support Open-Meteo.

## Complete owner Property request flow

This is the implemented React-to-database path for creating or editing a Property.

```mermaid
sequenceDiagram
    actor Owner
    participant Page as MyPropertiesPage
    participant Client as propertyApi
    participant Controller as PropertiesController
    participant Service as PropertyService
    participant Contract as IPropertyRepository
    participant Repo as PropertyRepository
    participant Db as ApplicationDbContext
    participant Sql as SQL Server

    Owner->>Page: Submit Property form
    Page->>Client: createProperty or updateProperty
    Client->>Controller: POST or PUT with JWT and JSON DTO
    Controller->>Service: CreateAsync or UpdateAsync
    Service->>Contract: Load owner, City, duplicate, or Property data
    Contract->>Repo: Interface dispatch
    Repo->>Db: EF Core query
    Db->>Sql: Parameterized SQL
    Sql-->>Db: Current records
    Db-->>Repo: Tracked or projected entities
    Repo-->>Service: Domain data
    Service->>Service: Validate ownership and business rules
    Service->>Contract: Add or save Property
    Contract->>Repo: Interface dispatch
    Repo->>Db: SaveChangesAsync
    Db->>Sql: Commit transaction
    Sql-->>Db: Success
    Service-->>Controller: Property result
    Controller-->>Client: 201 Created or 200 OK
    Client-->>Page: Managed Property DTO
    Page-->>Owner: Pending status and updated owner card
```

The controller does not query EF Core. The service enforces server-side authorization and rules;
the repository owns persistence-specific expressions.

## Public SSR and hydration flow

```mermaid
sequenceDiagram
    actor Visitor
    participant Web as Vite SSR server
    participant Api as Public Property API
    participant React as React hydration

    Visitor->>Web: GET /properties or /properties/{id}
    Web->>Api: Fetch public DTO over Docker network
    Api-->>Web: Approved and available public data
    Web-->>Visitor: Server-rendered HTML and escaped public state
    Visitor->>React: Load browser bundle
    React->>React: Hydrate matching route tree
    React->>Api: Later public filters or weather request
    Api-->>React: Public response or graceful unavailable result
```

Private routes receive `null` public transfer data. Tokens, contact numbers, owner-only
moderation data, and Admin metrics are never embedded in SSR HTML.

## Data model

```mermaid
erDiagram
    APPLICATION_USER ||--o| SELLER_PROFILE : owns
    APPLICATION_USER ||--o{ USER_STATUS_CHANGE : target
    APPLICATION_USER ||--o{ USER_STATUS_CHANGE : administrator
    SELLER_PROFILE ||--o{ PROPERTY : lists
    CITY ||--o{ PROPERTY : locates
    PROPERTY ||--o{ PROPERTY_IMAGE : contains

    APPLICATION_USER {
        guid Id PK
        string FullName
        string Status
        int TokenVersion
        datetime CreatedAtUtc
        datetime UpdatedAtUtc
    }
    CITY {
        guid Id PK
        string Name
        decimal Latitude
        decimal Longitude
        bool IsActive
    }
    PROPERTY {
        guid Id PK
        guid SellerProfileId FK
        guid CityId FK
        string ModerationStatus
        string AvailabilityStatus
        bool IsDeleted
        datetime UpdatedAtUtc
    }
    PROPERTY_IMAGE {
        guid Id PK
        guid PropertyId FK
        string RelativePath
        byte SortOrder
        bool IsPrimary
    }
    USER_STATUS_CHANGE {
        guid Id PK
        guid TargetUserId FK
        guid AdminUserId FK
        string PreviousStatus
        string NewStatus
        string Reason
    }
```

`ModerationStatus` and `AvailabilityStatus` are independent. Public queries require `Approved`,
`Available`, not deleted, and an active owner. Owners may mark sale listings `Sold` or rental
listings `Rented`. Material edits and image changes return an approved listing to `Pending`.

## Protected image flow

1. The owner creates a Property and then uploads one to five images to its protected image endpoint.
2. The API verifies ownership and listing state.
3. The validator checks extension, declared MIME type, magic bytes, successful decode, file size,
   dimensions, pixel count, and batch count.
4. The service creates server-controlled names and relative paths.
5. `LocalImageStorage` resolves paths under a configured root and rejects rooted or escaping paths.
6. SQL Server stores metadata; the Docker volume stores bytes outside the web root.
7. Retrieval streams through the API with visibility checks and
   `X-Content-Type-Options: nosniff`.

## Open-Meteo flow

1. The weather service loads a publicly visible Property through `IPropertyRepository`.
2. It passes the Property City ID, latitude, and longitude to the typed weather client.
3. The client checks its City-scoped memory cache.
4. A cache miss calls Open-Meteo with the configured finite timeout.
5. Valid observations are mapped to provider-neutral data and cached for 30 minutes by default.
6. Timeout, network, non-success, malformed, unsupported, or invalid responses become an
   unavailable result; the Property page still renders.

## Docker deployment view

```mermaid
flowchart LR
    Host["Developer machine"]
    Browser["Browser"]

    subgraph Network["Docker Compose default network"]
        Web["web<br/>Node 24 Alpine<br/>container 3000"]
        Api["api<br/>ASP.NET 8<br/>container 8080"]
        Sql["sqlserver<br/>SQL Server 2022<br/>container 1433"]
    end

    SqlVolume[("propertyhub-sql-data")]
    UploadVolume[("propertyhub-uploads")]
    OpenMeteo["api.open-meteo.com"]

    Host -->|"localhost:3000"| Web
    Host -->|"localhost:8081"| Api
    Host -->|"localhost:1433"| Sql
    Browser --> Web
    Browser --> Api
    Web -->|"http://api:8080"| Api
    Api -->|"SQL connection"| Sql
    Api -->|"HTTPS"| OpenMeteo
    Sql --- SqlVolume
    Api --- UploadVolume
```

Startup is health-gated: SQL Server becomes healthy before the API starts; API readiness succeeds
before the web service starts. The SQL and upload mounts are named volumes and survive normal
Compose restarts.

## Architecture decisions

- [ADR-0001](../adr/0001-layered-backend-architecture.md): layered controllers, services, and
  domain-specific repositories.
- [ADR-0002](../adr/0002-jwt-authorization-and-token-invalidation.md): Identity, JWT, and immediate
  token invalidation.
- [ADR-0003](../adr/0003-protected-local-image-storage.md): protected local uploads.
- [ADR-0004](../adr/0004-public-route-vite-ssr.md): public-only SSR with client dashboards.
- [ADR-0005](../adr/0005-resilient-open-meteo-integration.md): City-coordinate weather resilience.

## Deliberate boundaries

The canonical Gate took priority over the broader BRD. Contact reveal, enquiries, MailHog/outbox,
favourites, advanced filters, background image cleanup, refresh tokens, and cloud deployment remain
deferred. See [SCOPE_CUTS.md](SCOPE_CUTS.md).
