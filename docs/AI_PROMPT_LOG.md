# PropertyHub AI Prompt Log

These are the 20 prompts that most materially affected the implementation. Each entry records why
the prompt mattered and how its output was reviewed rather than treating AI output as accepted by
default.

## Architecture

### 1. Audit the repository against the approved requirements

- **Prompt:** Read `AGENTS.md` and all relevant documentation, inspect PropertyHub, compare it with
  the business requirements and Build Sprint Gate, and propose a structured implementation plan.
- **Impact:** Established a requirements-first baseline before any files changed.
- **Review:** The repository, branch, projects, tests, Docker files, and Markdown requirements were
  inspected. The plan was corrected when the canonical Gate text was initially unavailable.

### 2. Enforce controllers to services to repositories

- **Prompt:** Follow the required controller-to-service-to-repository architecture; controllers
  must not use EF Core directly and services must use repository abstractions.
- **Impact:** Fixed the dependency direction before implementation began.
- **Review:** Every feature uses thin controllers, application services, domain-specific
  repositories, and EF Core only in Infrastructure. No generic repository was introduced.

### 3. Make the canonical Gate the implementation priority

- **Prompt:** Read `docs/BUILD_SPRINT_1_REQUIREMENTS.md` completely and treat it as higher priority
  than optional BRD features or the review document.
- **Impact:** Prevented favourites, advanced filters, MailHog, and other BRD expansion from
  displacing mandatory Gate work.
- **Review:** Conflicts and deferrals are recorded in `docs/SCOPE_CUTS.md`; mandatory Gate criteria
  remain the acceptance baseline.

### 4. Separate moderation from availability

- **Prompt:** Model `ModerationStatus` as Pending/Approved/Rejected and `AvailabilityStatus` as
  Available/Sold/Rented; hide unapproved, unavailable, deleted, and disabled-owner listings.
- **Impact:** Prevented Admin moderation from overwriting the owner-controlled listing lifecycle.
- **Review:** Separate enums, persistence fields, service transitions, public queries, API tests,
  and frontend behavior enforce the distinction.

## Code generation

### 5. Scaffold only the complete Phase 1 foundation

- **Prompt:** Create the .NET solution, layered backend, React/Vite SSR foundation, test projects,
  database model/migration, exception handling, health checks, Docker baseline, and documentation
  placeholders without implementing later slices.
- **Impact:** Produced a buildable foundation without a giant unverified feature dump.
- **Review:** Restore, build, tests, EF migration validation, Compose validation, diff review, and
  focused commits were completed before authentication began.

### 6. Implement JWT authentication as one vertical slice

- **Prompt:** Add registration/login, User/Admin roles, configurable Admin seeding, disabled-account
  enforcement, protected frontend/API routes, and verified 401/403 behavior.
- **Impact:** Established the security boundary used by every later feature.
- **Review:** Identity handles password hashing; JWTs remain in browser memory; the ActiveUser
  policy checks database status and token version on every protected request.

### 7. Implement City as the second CRUD entity

- **Prompt:** Complete Admin-only City CRUD through controller/service/repository, validation,
  management UI, authorization tests, referential-integrity conflict handling, and Docker UAT.
- **Impact:** Satisfied the Gate's explicit second full CRUD entity.
- **Review:** Public active-city reference data and Admin CRUD were tested through services, API,
  React, SQL Server, and live Docker; referenced deletion returns 409.

### 8. Implement owner-scoped Property CRUD and moderation

- **Prompt:** Implement Phase 4 according to the Gate and permanent agent guide.
- **Impact:** Delivered the marketplace's primary owner/Admin/public workflow.
- **Review:** API enforcement covers ownership, validation, duplicate prevention, moderation,
  availability, soft deletion, disabled-owner filtering, public privacy, and essential filters.

### 9. Implement secure local property images

- **Prompt:** Add separate protected image endpoints, local persistence, validation, ownership,
  primary/deletion behavior, frontend display, tests, and Docker verification.
- **Impact:** Completed the mandatory upload and display journey without coupling images to
  Property creation.
- **Review:** Extension, MIME, magic bytes, decode, dimensions, pixel count, file size, count,
  randomized storage names, `nosniff`, authorization, and named-volume persistence were verified.

### 10. Integrate resilient property weather

- **Prompt:** Use a typed Open-Meteo `HttpClient`, finite timeout, caching, graceful fallback, City
  coordinates, property-detail UI, resilience tests, and Docker verification.
- **Impact:** Satisfied the real external API requirement without making Property details depend on
  provider availability.
- **Review:** Success, timeout, failure, invalid/unavailable response, cache behavior, live weather,
  forced failure fallback, and restored provider behavior all passed.

### 11. Implement the protected Admin dashboard

- **Prompt:** Add live SQL metrics, user listing, role changes, enable/disable behavior, protected
  UI/API, validation, authorization tests, immediate access removal, and unsafe self-action guards.
- **Impact:** Completed the Gate's Admin management workflow.
- **Review:** Role/status changes invalidate JWTs, self-demotion/self-disable are blocked, Admin
  disable and last-Admin demotion are prevented, and status changes retain focused audit evidence.

## Testing and quality

### 12. Cover authentication and authorization outcomes

- **Prompt:** Verify registration/login, invalid and missing JWTs, User-to-Admin access, disabled
  login, and rejection of a disabled account's already-issued token.
- **Impact:** Made the 401/403 boundary evidence-based instead of relying on attributes alone.
- **Review:** Unit/integration tests and live Docker probes confirmed 201/200/401/403 outcomes and
  immediate token invalidation.

### 13. Cover ownership and moderation transitions

- **Prompt:** Test owner isolation, Property CRUD, Pending/Approved/Rejected visibility,
  Sold/Rented terminal states, validation, and public-data privacy.
- **Impact:** Targeted the highest-risk domain and IDOR paths.
- **Review:** Service and API tests assert cross-owner 404 behavior and public filtering; React and
  Docker journeys verify the corresponding user experience.

### 14. Measure final backend coverage honestly

- **Prompt:** Achieve and verify at least 80% backend coverage, retain Cobertura and readable HTML
  evidence, and do not weaken tests or exclude application code to inflate the result.
- **Impact:** Converted test volume into a measurable Gate result.
- **Review:** Coverlet measured 91.2% line coverage (2,382/2,610). Only generated EF migrations are
  excluded; API, Application, Domain, and Infrastructure are included.

### 15. Run the complete frontend quality checkpoint

- **Prompt:** Run a clean npm install, all frontend tests, and production client/SSR builds; fix
  failures before reporting.
- **Impact:** Verified hydrated and server-rendered behavior from the same final source state.
- **Review:** npm reported zero vulnerabilities, 30 Vitest tests passed, and both Vite builds
  succeeded.

## Security and debugging

### 16. Diagnose the direct-route SSR failure

- **Prompt:** Reproduce the Docker 500 responses, capture the full stack, trace the exact undefined
  value, implement the smallest correct SSR-safe fix, and add four direct-render regressions.
- **Impact:** Fixed refresh/direct navigation without weakening route authorization.
- **Review:** The root cause was absent public route data crossing a strict JSON transfer boundary;
  only that intentional absence is now represented as `null`. Hydration and privacy checks passed.

### 17. Revamp the owner frontend without changing behavior

- **Prompt:** Redesign the shared navbar, My Properties page, and Add Property form as a responsive,
  restrained black-and-gold experience while preserving every API call, field, validation rule,
  protected route, image action, moderation state, CRUD behavior, SSR boundary, and hydration flow.
- **Impact:** Improved the most important owner workflow without introducing Tailwind, a component
  library, another large dependency, or frontend/backend contract drift.
- **Review:** Shared CSS tokens match the approved palette; the navbar has active and mobile states;
  semantic fieldsets group the unchanged form; listing cards retain every action and expose
  accessible status hierarchy. Focused tests, both production builds, direct SSR privacy checks,
  hydrated browser interaction, keyboard focus, console output, and overflow were reviewed.

## Docker and UAT

### 18. Resume blocked Property Docker UAT without volume loss

- **Prompt:** After Docker recovered, run only the blocked build/UAT, preserve named volumes, update
  evidence, and do not begin the next phase.
- **Impact:** Turned an environment blocker into real container evidence while preserving state.
- **Review:** Compose health, SQL-backed Property workflows, SSR, hydration, authorization, and
  volume names were captured; the SSR defect was reported as FAIL before it was fixed.

### 19. Rebuild and restart the final full stack

- **Prompt:** Run a full-stack restart and persistence UAT, execute the Gate checklist, and fix all
  in-scope failures.
- **Impact:** Verified recovery and persistence rather than only first startup.
- **Review:** SQL Server, API, and web recovered healthy; user/property/City counts, an approved
  Property, its image hash, weather endpoint, SSR response, and both named volumes were preserved.

## Documentation and delivery

### 20. Finalize honest submission evidence

- **Prompt:** Complete README, UAT, scope cuts, top-20 prompt log, committed coverage reports, small
  local commits, and report any acceptance criteria that remain unverified without pushing.
- **Impact:** Aligned the repository evidence with the actual final implementation and command
  results.
- **Review:** The final Gate report uses only PASS, FAIL, BLOCKED, or DEFERRED. Missing Loom videos
  remain BLOCKED rather than being described as complete, and all commits remain local.
