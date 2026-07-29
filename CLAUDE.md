# CLAUDE.md

## 1. Purpose

This file is the permanent operating guide for AI agents working on PropertyHub.
It defines the approved scope, technical standards, implementation workflow, verification rules,
documentation obligations, and Git practices for the project.

The agent must read this file completely before inspecting, planning, or changing the repository.

## 2. Project Overview

PropertyHub is a locally deployed, multi-user property marketplace that allows registered users to
publish and manage property listings, upload property images, browse approved properties, and use
location-related external data. Administrators moderate access and monitor the platform through a
protected dashboard.

The application uses:

- .NET 8 ASP.NET Core Web API
- ASP.NET Core Identity with JWT authentication
- Entity Framework Core
- SQL Server
- React 19 with Vite SSR and browser hydration
- Docker and Docker Compose
- Open-Meteo as the real external API integration
- xUnit, Moq, FluentAssertions, and Coverlet

Everything must run locally. Do not introduce a cloud-hosting dependency.

## 3. Authoritative Requirements

Before implementation, read these files completely:

1. `docs/BUILD_SPRINT_1_REQUIREMENTS.md`
2. `docs/Businessrequirementdoc.md`
3. `CLAUDE.md`
4. Existing repository conventions and established working code

Use this priority when requirements conflict:

1. Mandatory Gate acceptance criteria in `docs/BUILD_SPRINT_1_REQUIREMENTS.md`
2. Approved business and technical requirements in `docs/Businessrequirementdoc.md`
3. Implementation and working rules in this file
4. Existing conventions in the repository
5. Optional improvements

Do not silently choose between conflicting requirements. Record the conflict in
`docs/SCOPE_CUTS.md`, select the smallest solution that satisfies the higher-priority source, and
continue unless the decision would materially change the core business workflow.

## 4. Required Gate MVP

The following features are mandatory and take priority over all enhancements.

### 4.1 Authentication and Authorization

- Register and log in using ASP.NET Core Identity.
- Issue JWT access tokens for authenticated API access.
- Support at least `User` and `Admin` roles.
- Protect frontend routes according to authentication and role.
- Protect API endpoints using authorization policies or role attributes.
- Return `401 Unauthorized` for missing or invalid authentication.
- Return `403 Forbidden` when an authenticated user lacks the required role.
- Prevent disabled accounts from logging in or using protected endpoints.
- Seed an administrator safely through configuration or environment variables.
- Never expose passwords, password hashes, JWT secrets, or sensitive tokens.

### 4.2 Property CRUD

Implement complete Create, Read, Update, and Delete operations for `Property`.

- Public users may view only approved and active properties.
- Authenticated users may create property listings.
- Owners may view, update, and delete only their own listings.
- New or materially edited properties enter `Pending` moderation status.
- Administrators may approve or reject submitted properties.
- Enforce ownership and authorization in the API, never only in React.

### 4.3 City CRUD

Implement complete Create, Read, Update, and Delete operations for `City`.

- Public or authenticated users may use active cities in property forms and filters.
- City creation, editing, and deletion are admin-only.
- Prevent deletion when it would violate referential integrity.
- Return a clear conflict response when a city is still used by a property.

`Property` and `City` are the two core domain entities required for full end-to-end CRUD.

### 4.4 Property Image Upload

- Upload property images from the React frontend.
- Store files in a local uploads directory or Docker volume.
- Persist file metadata or the relative file path in SQL Server.
- Serve images through a controlled API or configured static-file path.
- Display uploaded images in property cards, details, and owner management views.
- Accept only JPEG, PNG, and WebP files.
- Validate extension, declared content type, file signature, file size, and ownership.
- Generate server-side filenames; never trust the original filename as a storage path.
- Prevent path traversal, executable uploads, and filename collisions.
- Keep runtime uploads out of Git.

### 4.5 External API Integration

Use Open-Meteo meaningfully for property location data.

- Store or derive the latitude and longitude needed for a request.
- Display relevant weather information on the property details page.
- Use `HttpClientFactory`.
- Apply a finite timeout.
- Handle external errors, invalid responses, and unavailable data gracefully.
- Cache successful results for a reasonable duration.
- Do not allow an Open-Meteo failure to break property details.
- Unit-test success, timeout, failure, and cached-result behavior.

### 4.6 Admin Area

Provide a separate protected admin experience containing:

- Dashboard statistics read from SQL Server
- Total users
- Active and disabled users
- Total, pending, approved, and rejected properties
- Total cities
- User list
- User role changes
- Account enable/disable actions
- Property moderation
- City management

All admin API operations must enforce the `Admin` role.

### 4.7 Frontend

- Use React with Vite SSR for public pages and hydrate them in the browser.
- Server-render the public home, search/listing, and property-details pages.
- Use client-side React behavior for authenticated user and admin dashboards.
- Include loading, empty, success, validation-error, authorization-error, and server-error states.
- Protect private routes in the router for user experience, while relying on API authorization for
  actual security.
- Use accessible labels, buttons, form validation messages, and image alternative text.
- Keep API access in a dedicated client/service layer.
- Do not access SQL Server directly from React or the SSR process.

### 4.8 Tests and Coverage

- Use xUnit for backend tests.
- Use Moq for mocks and FluentAssertions for assertions.
- Mirror source folders in the test project where practical.
- Write unit tests for services and validation rules.
- Write integration tests for critical API workflows.
- Cover authentication, authorization, ownership, CRUD, file validation, external API resilience,
  account disabling, role management, and dashboard metrics.
- Achieve at least 80% backend code coverage.
- Exclude generated EF Core migrations and other generated code from coverage when appropriate.
- Generate and retain a Cobertura report and a readable HTML coverage report.
- Never weaken, delete, skip, or falsely mark a test merely to increase coverage.

### 4.9 Docker

Docker Compose must start:

- SQL Server with a persistent volume
- .NET Web API
- React/Vite SSR frontend

The complete application must start with:

```bash
docker compose up --build
```

Requirements:

- Add service health checks where practical.
- Make services depend on health, not only container start order.
- Persist SQL Server data and uploaded files.
- Use environment variables for secrets and deployment configuration.
- Supply safe examples in `.env.example`.
- Document the final local URL in `README.md`.
- Verify all mandatory features in the Docker environment, not only on host development servers.

## 5. Deferred Features

Do not implement these until every mandatory Gate criterion is complete and verified:

- Favourites
- Advanced multi-field search beyond essential city, purpose, and property-type filters
- Real-time chat
- Payments or escrow
- Cloud file storage
- MailHog or production email workflows
- Outbox processing
- ETags
- Idempotency keys
- Complex audit infrastructure
- Real-time notifications
- Map SDKs requiring paid accounts
- Production deployment or cloud hosting
- Additional SSR optimization beyond the required public pages

If an optional feature threatens mandatory work, record it as `DEFERRED` in `docs/SCOPE_CUTS.md`.

## 6. Architecture

Follow this dependency direction:

```text
React/Vite SSR frontend
        |
        v
ASP.NET Core controllers
        |
        v
Application services
        |
        v
Repositories / EF Core
        |
        v
SQL Server
```

Cross-cutting infrastructure includes authentication, global exception handling, file storage,
Open-Meteo access, caching, logging, configuration, and Docker.

Rules:

- Controllers handle HTTP concerns, validation results, and status-code mapping.
- Services contain use-case and business logic.
- Repositories contain persistence-specific queries when a repository abstraction adds clear value.
- EF Core `DbContext` is the unit of work.
- Domain rules must not depend on controllers or React.
- Do not add abstractions that have no current use.
- Prefer a small, clear implementation over ceremonial layers.
- Do not bypass the service layer by adding new controller-to-database logic.

## 7. Canonical Domain Model

Use the approved BRD as the complete schema source. At minimum, keep these canonical concepts:

- `ApplicationUser`: Identity account, display information, status, and roles
- `SellerProfile`: optional one-to-one selling/contact profile owned by an `ApplicationUser`
- `Property`: listing content, ownership, city, location coordinates, price, purpose, type, status,
  and timestamps
- `City`: normalized location record used by properties
- `PropertyImage`: property image metadata, relative path, order, and primary-image state

Important rules:

- A seller is not a separate authentication role or duplicate account.
- A registered user may browse, enquire, and sell through one account.
- Use explicit foreign keys and appropriate delete behaviors.
- Use UTC for persisted timestamps.
- Add indexes and unique constraints only for known query or integrity needs.
- Never add an entity or field without connecting it to a requirement, endpoint, and test.

## 8. Coding Conventions

### 8.1 Naming

- Use PascalCase for public members, types, records, enums, and methods.
- Use camelCase for parameters and local variables.
- Use `_camelCase` for private fields.
- Use clear domain names; avoid unexplained abbreviations.
- Use the `Async` suffix for asynchronous methods.

### 8.2 Asynchronous Code

- All I/O-bound operations must be asynchronous.
- Pass `CancellationToken` through controllers, services, repositories, EF Core, file operations,
  and HTTP calls where supported.
- Do not use `.Result`, `.Wait()`, or unnecessary `Task.Run`.

### 8.3 Controllers

- Use attribute routing.
- Return `IActionResult` or `ActionResult<T>`.
- Use RESTful resource URLs and HTTP methods.
- Return consistent status codes and Problem Details responses.
- Keep controllers thin.
- Do not expose EF Core entities directly when a request or response DTO is appropriate.

### 8.4 Dependency Injection

- Register services in `Program.cs` or focused registration extension methods called by `Program.cs`.
- Use constructor injection.
- Avoid the Service Locator pattern.
- Choose lifetimes intentionally.
- Use typed or named `HttpClient` instances for external integrations.

### 8.5 Entity Framework Core

- Use Fluent API configurations in separate `IEntityTypeConfiguration<T>` classes.
- Keep lazy loading disabled.
- Load related data explicitly or project only the required fields.
- Prefer `AsNoTracking()` for read-only queries.
- Avoid N+1 queries.
- Use pagination for potentially large lists.
- Manage migrations through `dotnet ef` CLI commands.
- Do not edit generated migrations without a documented reason.

### 8.6 Error Handling

- Domain-specific exceptions must inherit from `DomainException`.
- Use global exception middleware to map known exceptions to Problem Details.
- Do not leak stack traces, database details, secrets, or internal paths to clients.
- Log enough structured context to diagnose failures without logging sensitive values.
- Use validation responses for expected input errors rather than exceptions where appropriate.

### 8.7 Style Preferences

- Use primary constructors when a simple service remains readable.
- Avoid `#region`.
- Use partial classes only when generated code requires them or test hosting needs a recognized
  partial `Program`.
- Treat 120 characters as the suggested maximum line length.
- Prefer expression-bodied members for clear one-line implementations.
- Enable nullable reference types.
- Prefer records for immutable request and response DTOs when appropriate.
- Avoid magic strings and numbers; use constants, options, or enums.
- Add comments only when they explain intent or a non-obvious decision.

## 9. Security Rules

- Validate all input on the server.
- Enforce ownership and role checks before reading or mutating protected resources.
- Prevent IDOR by scoping owner operations to the authenticated user.
- Use parameterized EF Core queries; never concatenate user input into SQL.
- Configure JWT issuer, audience, lifetime, signing key, and clock skew deliberately.
- Do not store JWTs in `localStorage`.
- Prefer an in-memory access token strategy; if refresh tokens are added, store them in secure,
  HttpOnly, SameSite cookies and rotate them.
- Configure CORS with explicit allowed origins; never combine credentials with wildcard origins.
- Escape rendered content and avoid unsafe HTML injection.
- Apply upload limits at application and server levels.
- Rate-limit sensitive endpoints where practical, especially login and uploads.
- Protect administrative mutations and prevent accidental removal of the last usable administrator.
- Never commit secrets, passwords, tokens, connection strings with credentials, `.env` files,
  uploaded runtime content, `bin`, `obj`, `node_modules`, test results, or local database files.
- Provide placeholders in `.env.example` and document required variables.

## 10. Agent Working Rules

The agent must:

1. Read the requirement documents and this file completely.
2. Inspect the repository, Git status, branch, project files, and existing tests before editing.
3. Preserve all unrelated user changes in a dirty working tree.
4. Produce a concise gap report and implementation order before major work.
5. Work in small vertical slices that include backend, frontend, tests, and documentation.
6. Reuse working code and established conventions.
7. Prefer the simplest solution that fully meets the Gate.
8. Update tests and documentation in the same slice as the implementation.
9. Run focused verification after each slice and broader verification at checkpoints.
10. Record important AI prompts continuously in `docs/AI_PROMPT_LOG.md`.
11. Record deliberate scope reductions in `docs/SCOPE_CUTS.md`.
12. Maintain the UAT script and results in `docs/UAT_REPORT.md`.
13. Continue autonomously while requirements and permissions are clear.
14. Stop only for a genuine requirement conflict, missing credential, authentication blocker,
    permission failure, destructive ambiguity, or decision that materially changes the approved
    scope.

The agent must not:

- Rewrite working modules without a specific requirement or verified defect.
- Add unrelated features.
- Overengineer abstractions.
- Hide failing tests or warnings.
- claim tests, coverage, Docker, or UAT passed without executing them.
- Invent commit hashes, URLs, credentials, metrics, or test results.
- Delete user work.
- Use force-push, hard reset, or broad destructive commands.
- modify the approved BRD merely to make an incomplete implementation appear compliant.

## 11. Implementation Phases

Use the following dependency-based order. A phase is complete only after its relevant build and
tests pass.

### Phase 1: Repository and Solution Foundation

- Inspect existing code and documents.
- Confirm backend, frontend, test, and Docker structure.
- Configure formatting, nullable reference types, environment examples, and Git ignores.
- Establish database and migration workflow.
- Create or update the implementation checklist.

### Phase 2: Authentication and Authorization

- Add Identity models and database configuration.
- Implement registration and login.
- Issue and validate JWTs.
- Seed `User` and `Admin` roles and a configurable administrator.
- Enforce disabled-account behavior.
- Add protected frontend routes and API tests for `401` and `403`.

### Phase 3: City Vertical Slice

- Implement the `City` entity and configuration.
- Add admin-only City CRUD APIs.
- Add City management UI.
- Add validation, conflict handling, and tests.

### Phase 4: Property Vertical Slice

- Implement property entity, DTOs, service, persistence, and endpoints.
- Add owner authorization and moderation status.
- Add public listing/details and owner CRUD UI.
- Add admin moderation UI.
- Add unit and integration tests.

### Phase 5: Image Upload

- Add secure local image storage.
- Add upload, retrieval, deletion, primary-image behavior, and UI display.
- Configure a persistent Docker volume.
- Add validation and security tests.

### Phase 6: Open-Meteo Integration

- Add typed `HttpClient`, timeout, caching, fallback behavior, and configuration.
- Display weather data on property details.
- Test success, timeout, invalid response, failure, and caching.

### Phase 7: Admin Dashboard and User Management

- Add database-backed metrics.
- Add user listing, role changes, and enable/disable actions.
- Protect all admin operations.
- Add UI and integration tests.

### Phase 8: Coverage and Quality

- Run the full backend suite with coverage.
- Add meaningful tests for uncovered critical behavior.
- Reach at least 80% backend coverage.
- Generate Cobertura and HTML reports.
- Run the frontend production build and available frontend tests.

### Phase 9: Docker and End-to-End Verification

- Build and start the full stack through Docker Compose.
- Apply migrations and verify health.
- Execute mandatory user and admin journeys in the containerized application.
- Verify persistent database and upload volumes.

### Phase 10: Submission Evidence

- Complete README setup instructions.
- Complete UAT execution results.
- Complete the categorized top-20 prompt log.
- Complete scope-cuts and final Gate checklists.
- Confirm meaningful Git history.
- Prepare accurate technical and non-technical Loom walkthrough outlines.

## 12. Git Rules

- Inspect `git status`, the current branch, and recent history before editing.
- Use a dedicated feature branch for implementation.
- Preserve unrelated local changes.
- Commit after each verified, meaningful vertical slice.
- Target at least 15 meaningful commits across the complete project history.
- Keep commits focused; do not create a giant final dump.
- Use conventional commit messages.

Examples:

```text
chore: scaffold PropertyHub solution
feat: add JWT authentication and role policies
feat: implement admin city management
feat: implement owner property CRUD
feat: add property moderation
feat: secure property image uploads
feat: integrate Open-Meteo weather data
feat: add admin user management
feat: add database-backed dashboard metrics
test: cover authentication and authorization
test: cover property and city workflows
chore: add Docker Compose deployment
docs: add UAT results and prompt log
```

Before committing:

- Review the diff.
- Run the relevant tests.
- Confirm no secrets or runtime artifacts are staged.
- Stage only files belonging to that slice.

Push the confirmed feature branch when repository authentication and permissions are available.
Never force-push. Do not merge, delete branches, or rewrite public history unless explicitly
authorized.

## 13. Required Documentation

Maintain these files throughout implementation:

### `README.md`

- Project summary
- Architecture and technology stack
- Prerequisites
- Environment variables
- Docker Compose setup
- Application URL
- Seed admin setup
- Test and coverage commands
- Known limitations and scope cuts

### `docs/AI_PROMPT_LOG.md`

- Record at least the 20 most impactful prompts.
- Group prompts by code generation, architecture, debugging, testing, security, Docker, and
  documentation.
- For each prompt, state why it mattered and how the output was reviewed or changed.

### `docs/UAT_REPORT.md`

- Include at least five scenarios.
- Cover registration/login, authorization, both CRUD entities, image upload, external data, and
  admin functions.
- Record steps, expected result, actual result, evidence, and `PASS` or `FAIL`.
- Never mark an unexecuted scenario as passed.

### `docs/SCOPE_CUTS.md`

- Record deferred or simplified features.
- State the reason, Gate impact, and future recommendation.
- Do not list a mandatory incomplete feature as an acceptable scope cut.

### Coverage Evidence

- Store the Cobertura output and a readable HTML report at the documented repository location.
- Ensure generated evidence does not contain secrets or machine-specific absolute paths when
  avoidable.

## 14. Verification Commands

First discover the actual solution and project paths. Then use the repository-equivalent commands:

```bash
dotnet restore
dotnet build --no-restore
dotnet test --no-build
dotnet test --collect:"XPlat Code Coverage"
npm ci
npm run build
docker compose config
docker compose up --build
docker compose ps
```

Use the project-approved Coverlet command when generating final evidence, for example:

```bash
dotnet test \
  /p:CollectCoverage=true \
  /p:CoverletOutputFormat=cobertura \
  /p:CoverletOutput=../coverage/
```

Do not copy these commands blindly if the repository uses different valid paths. Discover and
document the exact commands that succeed.

## 15. Completion Checklist

Do not declare the project complete until every mandatory item has an evidence-backed status.

- [ ] Repository history contains at least 15 meaningful commits.
- [ ] Full stack starts successfully with one Docker Compose command.
- [ ] Application is accessible at the URL documented in `README.md`.
- [ ] Registration and JWT login work.
- [ ] Missing or invalid JWT returns `401`.
- [ ] Non-admin access to admin endpoints returns `403`.
- [ ] Disabled users cannot authenticate or use protected functions.
- [ ] Property CRUD works through the API and frontend.
- [ ] City CRUD works through the API and frontend.
- [ ] Ownership checks prevent access to another user's property mutations.
- [ ] Property moderation controls public visibility.
- [ ] Secure image upload, storage, retrieval, and frontend display work.
- [ ] Open-Meteo data is used meaningfully.
- [ ] External API timeout and failure are handled gracefully.
- [ ] Admin user list, role changes, and account disabling work.
- [ ] Admin dashboard metrics use live database data.
- [ ] Backend coverage is at least 80%.
- [ ] Coverage evidence is generated and retained.
- [ ] UAT includes executed results for at least five scenarios.
- [ ] Top-20 prompt log is complete and categorized.
- [ ] README contains complete local setup instructions.
- [ ] Scope cuts and known limitations are documented honestly.
- [ ] Technical walkthrough plan covers architecture, decisions, tests, coverage, and integration.
- [ ] Non-technical demo plan covers both user and admin journeys without showing code.

Use only `PASS`, `FAIL`, `BLOCKED`, or `DEFERRED` in the final Gate report. Mandatory criteria may
not be marked `DEFERRED`.

## 16. Progress Reporting

After each implementation phase, report:

- Phase and status
- Completed behavior
- Changed files
- Commands executed
- Test, build, coverage, or Docker results
- Commit hash, if committed
- Remaining risks or blockers
- Next phase

Keep progress reports concise and factual. Distinguish implementation from verification.

## 17. Definition of Done

A feature is done only when:

- Its requirement and acceptance behavior are clear.
- Backend behavior is implemented.
- Frontend behavior is implemented when required.
- Authentication, authorization, ownership, and validation are enforced.
- Appropriate unit or integration tests pass.
- Relevant documentation is updated.
- The change works in the intended Docker environment.
- The diff has been reviewed for secrets, unrelated changes, and generated artifacts.
- A focused, meaningful commit records the completed slice.

The project is done only when all mandatory Gate criteria are implemented, verified, documented,
and reported truthfully.
