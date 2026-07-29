# PropertyHub AI Collaboration Log

This assessment-facing log contains exactly 20 substantial prompts supported by the repository's
implementation history and [`docs/AI_PROMPT_LOG.md`](docs/AI_PROMPT_LOG.md). Prompt wording is
preserved; outcomes explain how the generated work was reviewed rather than implying automatic
acceptance.

## Prompt 01

- **Phase:** Planning and requirements
- **Prompt:** Read `AGENTS.md` and all relevant documentation, inspect PropertyHub, compare it with the business requirements and Build Sprint Gate, and propose a structured implementation plan.
- **Purpose:** Establish a requirements-first implementation order and identify gaps before editing.
- **Outcome:** **Modified.** The initial plan was paused when the canonical Gate text was unavailable, then corrected after the document was supplied and read completely.

## Prompt 02

- **Phase:** Architecture
- **Prompt:** Follow the required controller-to-service-to-repository architecture; controllers must not use EF Core directly and services must use repository abstractions.
- **Purpose:** Enforce the approved dependency direction without a generic repository.
- **Outcome:** **Accepted.** Controllers remained thin, services own use-case rules, and domain-specific repositories contain EF Core persistence queries.

## Prompt 03

- **Phase:** Scope management
- **Prompt:** Read `docs/BUILD_SPRINT_1_REQUIREMENTS.md` completely and treat it as higher priority than optional BRD features or the review document.
- **Purpose:** Protect mandatory Gate work from optional BRD expansion.
- **Outcome:** **Accepted.** The Gate became the acceptance baseline and optional enquiries, favourites, advanced filters, and email workflows were documented as deferred.

## Prompt 04

- **Phase:** Domain design
- **Prompt:** Model `ModerationStatus` as Pending/Approved/Rejected and `AvailabilityStatus` as Available/Sold/Rented; hide unapproved, unavailable, deleted, and disabled-owner listings.
- **Purpose:** Separate Admin moderation from the owner-controlled listing lifecycle.
- **Outcome:** **Accepted.** Separate enums, persistence fields, service transitions, public queries, tests, and UI badges enforce the distinction.

## Prompt 05

- **Phase:** Foundation
- **Prompt:** Create the .NET solution, layered backend, React/Vite SSR foundation, test projects, database model/migration, exception handling, health checks, Docker baseline, and documentation placeholders without implementing later slices.
- **Purpose:** Produce a complete, buildable foundation without an oversized first change.
- **Outcome:** **Accepted.** Restore, build, tests, EF migration validation, Compose validation, and focused commits were completed before feature slices.

## Prompt 06

- **Phase:** Authentication and security
- **Prompt:** Add registration/login, User/Admin roles, configurable Admin seeding, disabled-account enforcement, protected frontend/API routes, and verified 401/403 behavior.
- **Purpose:** Establish the authentication and authorization boundary used by later features.
- **Outcome:** **Accepted.** Identity registration/login, JWT issuance, role policies, database-backed active-user checks, protected routes, and 401/403 tests were implemented.

## Prompt 07

- **Phase:** City vertical slice
- **Prompt:** Complete Admin-only City CRUD through controller/service/repository, validation, management UI, authorization tests, referential-integrity conflict handling, and Docker UAT.
- **Purpose:** Deliver the Gate's second required full CRUD entity end to end.
- **Outcome:** **Accepted.** Public reference data, Admin CRUD, validation, 409 conflict behavior, React management, automated tests, and live Docker UAT passed.

## Prompt 08

- **Phase:** Property vertical slice
- **Prompt:** Implement Phase 4 according to the Gate and permanent agent guide.
- **Purpose:** Deliver owner-scoped Property CRUD, public discovery, and Admin moderation.
- **Outcome:** **Modified.** The implementation incorporated the later-approved separation of moderation and availability and kept image upload as a separate endpoint and phase.

## Prompt 09

- **Phase:** Secure uploads
- **Prompt:** Add separate protected image endpoints, local persistence, validation, ownership, primary/deletion behavior, frontend display, tests, and Docker verification.
- **Purpose:** Complete image upload without coupling images to Property creation.
- **Outcome:** **Accepted.** Extension, MIME, signature, decoding, size, dimension, ownership, count, randomized-name, traversal, display, and persistence paths were verified.

## Prompt 10

- **Phase:** External integration
- **Prompt:** Use a typed Open-Meteo `HttpClient`, finite timeout, caching, graceful fallback, City coordinates, property-detail UI, resilience tests, and Docker verification.
- **Purpose:** Make weather useful while preventing provider failure from breaking Property details.
- **Outcome:** **Accepted.** Coordinate-based requests, a typed client, bounded timeout, successful-result cache, provider-neutral fallback, UI, tests, and Docker probes passed.

## Prompt 11

- **Phase:** Admin vertical slice
- **Prompt:** Add live SQL metrics, user listing, role changes, enable/disable behavior, protected UI/API, validation, authorization tests, immediate access removal, and unsafe self-action guards.
- **Purpose:** Complete protected database-backed administration safely.
- **Outcome:** **Accepted.** Metrics, search/listing, role/status mutations, immediate JWT invalidation, audit records, self-action guards, last-Admin protection, tests, and Docker UAT passed.

## Prompt 12

- **Phase:** Security testing
- **Prompt:** Verify registration/login, invalid and missing JWTs, User-to-Admin access, disabled login, and rejection of a disabled account's already-issued token.
- **Purpose:** Prove status-code and token-invalidation behavior rather than relying only on endpoint attributes.
- **Outcome:** **Accepted.** Automated and live probes confirmed 201/200 success, 401 for missing/invalid authentication, and 403 for insufficient role or disabled account.

## Prompt 13

- **Phase:** Domain and authorization testing
- **Prompt:** Test owner isolation, Property CRUD, Pending/Approved/Rejected visibility, Sold/Rented terminal states, validation, and public-data privacy.
- **Purpose:** Cover high-risk IDOR, workflow-transition, and disclosure paths.
- **Outcome:** **Accepted.** Service, API, React, SSR, and Docker evidence covers cross-owner denial, visibility rules, terminal availability, validation, and protected owner data.

## Prompt 14

- **Phase:** Coverage
- **Prompt:** Achieve and verify at least 80% backend coverage, retain Cobertura and readable HTML evidence, and do not weaken tests or exclude application code to inflate the result.
- **Purpose:** Produce honest, reproducible coverage evidence for the Gate.
- **Outcome:** **Accepted.** Coverlet measured 91.2% line coverage, 2,382 of 2,610 lines; only generated EF migrations are excluded.

## Prompt 15

- **Phase:** Frontend quality
- **Prompt:** Run a clean npm install, all frontend tests, and production client/SSR builds; fix failures before reporting.
- **Purpose:** Verify browser and server bundles from a clean dependency installation.
- **Outcome:** **Accepted, then extended.** The checkpoint passed with 30 tests; the later UI work added another regression, bringing the current verified frontend total to 31.

## Prompt 16

- **Phase:** SSR debugging
- **Prompt:** Reproduce the Docker 500 responses, capture the full stack, trace the exact undefined value, implement the smallest correct SSR-safe fix, and add four direct-render regressions.
- **Purpose:** Fix direct navigation without masking the root cause or weakening route protection.
- **Outcome:** **Modified.** Investigation traced absent route data across the strict JSON transfer boundary; representing only that intentional absence as `null` fixed all four routes and preserved hydration/privacy behavior.

## Prompt 17

- **Phase:** UI and accessibility
- **Prompt:** Redesign the shared navbar, My Properties page, and Add Property form as a responsive, restrained black-and-gold experience while preserving every API call, field, validation rule, protected route, image action, moderation state, CRUD behavior, SSR boundary, and hydration flow.
- **Purpose:** Improve the owner experience without changing contracts or adding a large styling dependency.
- **Outcome:** **Modified.** Shared tokens, semantic form groups, visible focus, active/mobile navigation, cards, badges, and responsive rules were accepted; a complete manual narrow-viewport visual pass remains explicitly unverified.

## Prompt 18

- **Phase:** Docker recovery
- **Prompt:** After Docker recovered, run only the blocked build/UAT, preserve named volumes, update evidence, and do not begin the next phase.
- **Purpose:** Resume verification safely without losing SQL or upload evidence.
- **Outcome:** **Accepted.** Compose health, SQL-backed Property workflows, SSR, hydration, authorization, and unchanged named volumes were captured; the discovered SSR failure was reported before being fixed.

## Prompt 19

- **Phase:** Final UAT
- **Prompt:** Run a full-stack restart and persistence UAT, execute the Gate checklist, and fix all in-scope failures.
- **Purpose:** Prove restart recovery and persistence rather than only successful first startup.
- **Outcome:** **Accepted.** SQL Server, API, and web recovered healthy; counts, approved Property data, image hash, weather, SSR response, and both named volumes persisted.

## Prompt 20

- **Phase:** Documentation and delivery
- **Prompt:** Complete README, UAT, scope cuts, top-20 prompt log, committed coverage reports, small local commits, and report any acceptance criteria that remain unverified without pushing. Then prepare practical, timed, word-for-word technical and non-technical Loom scripts from the actual implementation and evidence without recording videos or exposing secrets.
- **Purpose:** Align submission evidence and presentation guidance with the verified implementation.
- **Outcome:** **Modified.** Evidence remained honest and Loom stayed blocked; the scripts were subsequently expanded to the assessment's 10–15 minute technical and 5–10 minute non-technical duration requirements.
