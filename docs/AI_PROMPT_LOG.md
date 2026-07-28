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

## Documentation

Entries will be added as final UAT, coverage, and walkthrough evidence is produced.
