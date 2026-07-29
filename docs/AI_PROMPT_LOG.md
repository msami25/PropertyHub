# PropertyHub AI Prompt Log

This log records only prompts that materially influence implementation. It will contain at least 20
reviewed entries before Gate submission.

## Architecture

### 1. Repository audit and Gate implementation plan

- **Prompt:** Inspect PropertyHub against the BRD and Build Sprint Gate, then prepare an
  implementation plan.
- **Why it mattered:** Established the documentation-only baseline and exposed the missing
  canonical Gate document.
- **Review:** The initial plan was revised to require domain-specific repositories and a smaller
  scaffold phase.

### 2. Correct the architecture and establish the canonical Gate

- **Prompt:** Use controllers to services to repositories, require the canonical Gate file, separate
  image uploads from Property creation, and limit the first phase to foundation work.
- **Why it mattered:** Removed direct service-to-EF access and prevented a large unverified feature
  dump.
- **Review:** The final foundation plan uses separate projects and postpones feature abstractions
  until their vertical slices.

### 3. Separate moderation and availability state

- **Prompt:** Model moderation independently from Available, Sold, and Rented, and keep the scaffold
  practical.
- **Why it mattered:** Prevents moderation decisions from overwriting the owner-controlled listing
  lifecycle.
- **Review:** The initial model uses distinct `ModerationStatus` and `AvailabilityStatus` enums.

## Code generation

### 4. Implement the approved Phase 1 scaffold

- **Prompt:** Create and verify the layered backend, SSR frontend, tests, migration, Docker baseline,
  environment template, and documentation without starting later feature slices.
- **Why it mattered:** Authorizes the first repository implementation while preserving Gate-first
  sequencing.
- **Review:** Changes are being verified through restore, build, tests, migration scripting, Compose
  validation, and focused commits.

### 5. Implement JWT authentication as an isolated vertical slice

- **Prompt:** Complete registration/login, User/Admin roles, configured Admin seeding,
  disabled-account enforcement, protected routes, `401`/`403` tests, frontend integration,
  documentation, and verification without beginning City CRUD.
- **Why it mattered:** Defined a security-complete Phase 2 boundary and prevented unrelated domain
  work from entering the authentication commits.
- **Review:** The backend follows controller to service to identity repository, JWTs stay in browser
  memory, and HTTP integration tests exercise active, disabled, User, and Admin behavior.

## Testing

The Phase 2 implementation prompt produced service tests with Moq and HTTP integration coverage for
registration, login, role enforcement, disabled accounts, and invalid tokens.

### 7. Implement City as the second Gate CRUD entity

- **Prompt:** Complete the Admin-only City API, controller-service-repository flow, validation,
  management UI, authorization/CRUD tests, referential conflict handling, documentation, and
  Docker verification without beginning Property CRUD.
- **Why it mattered:** Delivered the second mandatory Gate CRUD entity as a complete vertical slice
  rather than a database-only model.
- **Review:** The implementation keeps active reference data public, protects all mutations with
  the Admin policy, returns 409 for duplicate/referenced cities, seeds canonical coordinates, and
  exercises the same flow through unit, integration, React, Docker API, SQL Server, and browser UI
  checks.

### 8. Implement Property CRUD as the next Gate vertical slice

- **Prompt:** Implement Phase 4 according to the canonical Gate and permanent agent guide.
- **Why it mattered:** Authorized the owner-scoped Property workflow, separate moderation and
  availability lifecycles, public SSR pages, Admin moderation, and end-to-end automated evidence
  without starting image uploads.
- **Review:** Controllers call the Property service, the service enforces business transitions and
  uses a domain-specific repository, and EF Core alone handles persistence. Tests verify owner
  isolation, 401/403 behavior, inactive cities, duplicate prevention, moderation visibility,
  disabled-owner filtering, terminal Sold/Rented states, public-data privacy, and React journeys.
  Live Docker evidence later confirmed those workflows while exposing a direct-route SSR failure.

## Security

The Phase 2 implementation prompt established short-lived signed JWTs, strict issuer/audience/key
validation, per-request account-status checks, rate limiting, and safe Admin seeding.

## Docker and debugging

### 6. Verify authentication in the complete Docker environment

- **Prompt:** Run restore, builds, tests, migration validation, Compose validation, and live
  container checks; fix Phase 2 failures before reporting.
- **Why it mattered:** Required evidence that authentication works against SQL Server and the
  production-shaped API/SSR containers, not only the in-memory integration host.
- **Review:** The live probe covered health, registration, login, seeded Admin access, 401/403
  behavior, and immediate rejection of a disabled account's existing JWT.

### 9. Resume the blocked Phase 4 Docker UAT

- **Prompt:** With Docker Desktop healthy again, run only the blocked Compose build and live Phase 4
  UAT, preserve named volumes, update evidence, and commit documentation only.
- **Why it mattered:** Converted an environmental blocker into actual container evidence without
  expanding into Phase 5 or rewriting completed Phase 4 code.
- **Review:** Images built and all services became healthy. SQL-backed Property CRUD, authorization,
  moderation, public visibility, public SSR privacy, and hydrated owner/Admin UI routes passed.
  Direct private-route requests returned HTTP 500 because the SSR serializer receives undefined
  public-page data. The UAT was therefore recorded as `FAIL`, and application code was left
  unchanged as requested.

## Documentation

Entries will be added as final UAT, coverage, and walkthrough evidence is produced.
