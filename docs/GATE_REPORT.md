# PropertyHub Final Gate Report

Execution date: 2026-07-29

Branch: `feature/core-implementation`

Status values are limited to `PASS`, `FAIL`, `BLOCKED`, and `DEFERRED`. Mandatory canonical Gate
criteria are never marked `DEFERRED`.

## Canonical Build Sprint Gate

| Acceptance criterion | Status | Evidence |
|---|---|---|
| At least 15 meaningful commits without a giant final dump | PASS | 46 meaningful commits existed before the focused Phase 8 evidence commits |
| One Compose command starts SQL Server, API, and frontend | PASS | `docker compose --env-file .env.example up -d --build`; all three services healthy |
| Application is accessible at the documented URL | PASS | Web `http://localhost:3000`; API `http://localhost:8081`; health responses 200 |
| Registration and JWT login | PASS | `AuthenticationEndpointTests`; Docker UAT returned 201/200 |
| Missing/invalid JWT returns 401 | PASS | Integration tests and Docker probes |
| Non-Admin access to Admin endpoints returns 403 | PASS | Integration tests; Phase 7 Docker UAT returned 403 |
| Full Property CRUD in API and frontend | PASS | Property service/API/React tests and Phase 4 live Docker UAT |
| Full City CRUD in API and frontend | PASS | City service/API/React tests and Phase 3 live Docker UAT |
| Local file upload, controlled retrieval, and frontend display | PASS | Phase 5 Docker rerun; invalid signature 400, valid upload/display 200 |
| Admin user list and role changes | PASS | Admin API/React tests and Phase 7 Docker promotion/demotion UAT |
| Live database dashboard metrics | PASS | Metrics matched SQL state; final live counts retained across restart |
| Meaningful external API with resilience | PASS | Open-Meteo City-coordinate weather, timeout/failure fallback, success-only cache tests and Docker UAT |
| At least 80% backend coverage and included report | PASS | 91.2%, 2,382/2,610 lines; Cobertura, HTML, and text reports in `docs/coverage/` |
| Committed UAT script with at least five executed scenarios | PASS | Eight PASS scenarios in `docs/UAT_REPORT.md`; Phase 5, 7, and 8 scripts |
| Categorized top-20 prompt log | PASS | 20 reviewed entries in `docs/AI_PROMPT_LOG.md` |
| Loom technical video at most 15 minutes with required content | BLOCKED | Walkthrough outline exists; no recording or externally accessible Loom link was supplied |
| Loom non-technical video at most 10 minutes with required content | BLOCKED | Demo outline exists; no recording or externally accessible Loom link was supplied |

## Extended mandatory application checks

| Acceptance criterion | Status | Evidence |
|---|---|---|
| Disabled accounts cannot log in or reuse existing JWTs | PASS | Unit/integration tests and Phase 7 Docker UAT |
| Ownership blocks cross-user Property mutations | PASS | Cross-owner API tests and Phase 4 Docker UAT |
| Moderation and availability independently control public visibility | PASS | Property service/API/React tests and live moderation UAT |
| Image validation covers extension, MIME, signature, decode, size, dimensions, and ownership | PASS | Validator, storage, API tests, and Phase 5 Docker rerun |
| Provider failure does not break Property details | PASS | Weather timeout/failure tests and forced-failure Docker UAT |
| Unsafe Admin self-demotion/self-disable is blocked | PASS | Service/API tests and Phase 7 Docker UAT returned 409 |
| Role/status changes invalidate access immediately | PASS | Integration tests and Phase 7 stale-token checks returned 403 |
| SQL and upload data survive a full Compose restart | PASS | Phase 8 preserved counts, Property, image hash, and named volumes |
| Public SSR contains useful content without JWT/contact data | PASS | Fresh direct browser render and SSR tests; no token/contact marker |
| Hydration/browser console remains clean | PASS | 30 frontend tests and fresh production browser probe with zero console entries |
| README setup, environment, test, coverage, UAT, and limitations | PASS | Root `README.md` |
| Scope conflicts and optional deferrals are documented | PASS | `docs/SCOPE_CUTS.md` |
| Technical and non-technical walkthrough plans exist | PASS | Root `README.md` |

## Deferred non-canonical scope

| Feature | Status | Reason |
|---|---|---|
| Favourites and advanced filters | DEFERRED | Outside the canonical Gate |
| Contact reveal and enquiries | DEFERRED | Broader BRD/review scope; outside the canonical checklist and approved Phase 8 boundary |
| MailHog/outbox and background cleanup | DEFERRED | Optional post-Gate workflows |

## Final verdict

| Area | Status |
|---|---|
| Mandatory application implementation | PASS |
| Automated tests and coverage | PASS |
| Docker rebuild, restart, and persistence | PASS |
| Repository documentation evidence | PASS |
| External Loom submission evidence | BLOCKED |
| Overall submission Gate | BLOCKED |
