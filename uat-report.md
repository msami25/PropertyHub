# PropertyHub Capstone UAT Report

This report summarizes the executed acceptance evidence retained in
[`docs/UAT_REPORT.md`](docs/UAT_REPORT.md). It does not turn an automated assertion, a planned
recording step, or an unexecuted visual check into a manual pass.

## Verified environment

| Item | Verified value |
|---|---|
| Execution dates | 28–29 July 2026 |
| Source branch | `feature/core-implementation` |
| Application | Docker Compose: SQL Server, ASP.NET Core API, React/Vite SSR web |
| Public web URL | `http://localhost:3000` |
| API URL | `http://localhost:8081` |
| Database | SQL Server 2022 Express in a persistent named volume |
| Upload storage | Protected API storage in a persistent named volume |
| Backend tests | 79 passing: 38 unit and 41 integration |
| Frontend tests | 31 passing Vitest tests |
| Backend line coverage | 91.2%: 2,382 of 2,610 coverable lines |
| Detailed evidence | [`docs/UAT_REPORT.md`](docs/UAT_REPORT.md), [`docs/GATE_REPORT.md`](docs/GATE_REPORT.md), and [`docs/coverage/index.html`](docs/coverage/index.html) |

## Major acceptance tests

`PASS` means the recorded result was executed against the stated environment. `NOT RUN` is used
where the available evidence does not support a pass or fail decision.

| ID | Acceptance test | Expected result | Actual result | Result | Evidence / comments |
|---|---|---|---|---|---|
| CAP-UAT-01 | Registration and login | A visitor can create a User account, sign in, and call a protected endpoint | Registration returned 201, login returned 200, and `/api/auth/me` returned 200 | PASS | Docker HTTP probe and `AuthenticationEndpointTests` |
| CAP-UAT-02 | Authentication and role authorization | Missing or invalid authentication returns 401; a User calling an Admin endpoint receives 403 | Missing and malformed tokens returned 401; User-to-Admin access returned 403 | PASS | Docker probes and authentication integration tests |
| CAP-UAT-03 | Disabled-account enforcement | A disabled account cannot log in or continue using a previously issued token | Disabled login and reuse of the prior access token returned 403 immediately | PASS | Docker HTTP/SQL probe and token-version integration coverage |
| CAP-UAT-04 | City CRUD and validation | Admin can create, read, update, deactivate, and delete a City; invalid or duplicate data is rejected | Full CRUD succeeded; invalid input returned 400 and duplicate normalized names returned 409 | PASS | Phase 3 API/browser UAT, `CityEndpointTests`, `CityServiceTests`, and frontend City tests |
| CAP-UAT-05 | City referential integrity | A City used by a Property cannot be deleted | Referenced deletion returned 409; deletion succeeded after the reference was removed | PASS | Docker SQL/API probe and City integration tests |
| CAP-UAT-06 | Owner Property CRUD and ownership | An owner can manage their listing; another owner cannot read or mutate it | Create, read, edit, availability change, and soft-delete succeeded; cross-owner read returned 404 | PASS | Phase 4 Docker journey, `PropertyEndpointTests`, and `PropertyServiceTests` |
| CAP-UAT-07 | Property validation and moderation | New or materially edited listings are Pending; only Admin can approve or reject; rejection needs a reason | Create and material edit produced Pending; approval and reasoned rejection worked; missing rejection reason returned 400 | PASS | Docker API/browser UAT and Property service/integration tests |
| CAP-UAT-08 | Public visibility rules | Only approved, Available, non-deleted listings owned by active users are public | Pending, Rejected, Sold, Rented, soft-deleted, and disabled-owner listings were hidden | PASS | Public API/SSR probes and Property workflow tests |
| CAP-UAT-09 | Availability workflow | Owners can mark sale listings Sold and rental listings Rented without changing moderation history | Sold and Rented transitions succeeded and removed the listings from public results | PASS | Phase 4 Docker journey and status-transition tests |
| CAP-UAT-10 | Secure image upload and display | Valid JPEG/PNG/WebP images persist and display; unsafe content is rejected; ownership and count rules apply | Decoded PNG uploads, primary selection, display, and deletion rules passed; invalid signature returned 400 and last-image deletion returned 409 | PASS | Phase 5 Docker/API/SSR/browser UAT and image validator/storage/endpoint tests |
| CAP-UAT-11 | Protected image access and persistence | Hidden listing images follow authorization; public images remain controlled; files survive service recreation | Hidden/public access followed moderation and the image remained available with the same content after API recreation | PASS | Controlled image endpoints, browser probe, and upload-volume evidence |
| CAP-UAT-12 | Open-Meteo success and cache | Property details use the Property City coordinates and successful weather observations are cached | Live coordinate-based weather rendered and repeated calls used the successful cache path | PASS | Phase 6 Docker/browser probes and Open-Meteo client/service tests |
| CAP-UAT-13 | Open-Meteo timeout or failure | Provider failure must not break Property details and must show a safe unavailable state | Forced provider failure produced a friendly unavailable result while Property API and SSR remained 200 | PASS | Failure-mode Docker probe and success/timeout/failure/invalid/cache tests |
| CAP-UAT-14 | Admin dashboard and user management | Admin sees live metrics, searches users, changes roles, disables/reactivates users, and cannot take unsafe self-actions | Metrics matched SQL state; role/status changes invalidated tokens; self-disable/self-demotion returned 409; audit records were written | PASS | Phase 7 Docker/API/SQL/browser UAT and Admin tests |
| CAP-UAT-15 | SSR, hydration, and public privacy | Direct public and application route requests return 200; hydration has no warnings; protected data is absent from SSR HTML | Direct `/login`, `/register`, `/my/properties`, `/admin/properties`, and public routes returned 200 after the transfer-boundary fix; browser hydration had no console errors; tokens/contact numbers were absent | PASS | Four SSR regression tests and live browser/HTTP rerun |
| CAP-UAT-16 | Docker health, restart, and persistence | All services become healthy and SQL/upload data survive a normal full-stack restart | SQL Server, API, and web recovered healthy; users, Cities, Properties, image hash, and both named volumes persisted | PASS | `Invoke-Phase8PersistenceUat.ps1`, Compose health checks, SQL/API/SSR probes |
| CAP-UAT-17 | Responsive visual behavior at mobile viewport | Owner and public pages remain usable without overflow at mobile width | Desktop behavior was already verified, and the project owner confirmed the mobile view worked correctly during recording | PASS | Owner confirmation supplied with the non-technical recording on 29 July 2026; a separate tablet-width result is not claimed |

## Known limitations and release impact

| Limitation | Severity | Impact | Workaround / next action |
|---|---|---|---|
| Both recorded Loom videos exceed their maximum duration | Blocker for assessment submission | Technical is approximately 18 minutes against a 15-minute maximum; non-technical is approximately 11 minutes against a 10-minute maximum | Trim or re-record both videos using [`docs/LOOM_VIDEO_SCRIPTS.md`](docs/LOOM_VIDEO_SCRIPTS.md), then verify the final durations |
| Separate tablet-width visual UAT is not retained | Low | Desktop and mobile are confirmed, but a dedicated tablet-width result is not documented | Check one representative tablet width before final submission |
| Enquiries/contact reveal, favourites, advanced filters, and email workflows are deferred | Low for the mandatory Gate | These optional BRD workflows are unavailable | Use essential City/purpose/type filters; see [`docs/SCOPE_CUTS.md`](docs/SCOPE_CUTS.md) |
| JWT access tokens are intentionally memory-only and there are no refresh tokens | Low | A browser refresh requires signing in again | Sign in again; add secure rotating refresh tokens only in a later security slice |
| Orphan-upload cleanup is not a background process | Low | Failed cleanup is logged and may require maintenance | Inspect the protected upload volume during maintenance; add reconciliation only after Gate scope |

## Release sign-off

The mandatory application Gate is supported by passing automated tests, 91.2% backend line
coverage, Docker health checks, and executed end-to-end UAT. Capstone submission sign-off remains
**BLOCKED** until the technical recording is no longer than 15 minutes and the non-technical
recording is no longer than 10 minutes. Mobile behavior is now confirmed. No mandatory application
criterion is being treated as a scope cut.
