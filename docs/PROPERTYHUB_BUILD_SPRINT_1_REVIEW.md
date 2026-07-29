# PropertyHub — Capstone Build Sprint 1 Review

**Sprint duration:** 360 minutes  
**Review verdict:** Feasible only as a tightly controlled gate MVP  
**Primary journey:** Register/Login → Create property → Admin approval → Public browse → View external data → Contact owner

## 1. Overall Assessment

The requirements are complete and suitable for PropertyHub, but they are intentionally too large for six hours if the full Version 2.0 BRD is implemented. The sprint should target the smallest end-to-end implementation that satisfies every assessment checkbox. Production-grade enhancements must not delay a working Docker demo.

The most important success condition is not the number of screens or advanced features. It is a reliable demonstration in Docker of authentication, two-entity CRUD, secure image upload, a real external API, property moderation, user administration, tests, and the complete buyer/seller journey.

## 2. Important Compliance Gaps

- **MailHog is not a real third-party API.** It is a local SMTP testing service. Keep it only as an optional enquiry notification if time permits. To satisfy this sprint, integrate a real public API such as **Open-Meteo** and show current weather for the property's city. Apply an HTTP timeout, graceful fallback, and 30-minute cache.
- **A second full CRUD entity must be explicit.** `Property` is the first entity. Use `City` as the second because PropertyHub already needs a city dropdown. Admins can list, create, edit, and delete cities; deletion returns `409 Conflict` when properties reference the city.
- **The existing BRD is a three-day design, not a six-hour implementation scope.** Features such as favourites, advanced filters, transactional outbox processing, ETags, idempotency keys, immutable audit records, image cleanup jobs, and detailed SSR performance targets are valuable but not required for this gate.
- **At least 15 meaningful commits are mandatory.** Commit after each verified feature rather than creating a final bulk commit.
- **Coverage must be measured early.** Waiting until the last hour to generate tests creates a high risk of missing the 80% target. Keep backend code small and test each service/controller as it is completed.
- **React Vite SSR adds implementation risk.** Retain the approved decision, but server-render only public Home, Property List, and Property Details routes. Hydrate them in the browser; implement authenticated dashboards as normal client-side React routes.

## 3. Frozen Gate MVP

### Must Implement

- .NET 8 Web API, React with Vite SSR, EF Core, SQL Server, and Docker Compose.
- Registration and JWT login.
- Exactly two stored roles for authorization: `User` and `Admin`.
- Protected frontend routes and protected API endpoints.
- Active/disabled user enforcement on protected requests.
- Full owner-scoped CRUD for `Property`.
- Full admin CRUD for `City`.
- Property types stored as a simple enum: House, Apartment, Plot, Shop, and Office.
- Sale/Rent purpose, price, address, area, bedrooms, bathrooms, and contact number.
- Upload and display 1–5 JPEG/PNG/WebP images with size, extension, MIME, signature, randomized filename, and ownership checks.
- Admin listing approval/rejection.
- Public browsing of approved listings.
- Basic filters: city, Sale/Rent, and property type.
- Authenticated phone-number reveal and a simple stored enquiry form.
- Open-Meteo weather data displayed on the property details page, with timeout, error handling, and caching.
- Admin dashboard with live counts.
- Admin user list, role change, disable, and enable actions.
- Unit and integration tests with backend line coverage of at least 80%.
- Dockerfiles and one `docker-compose.yml` that starts the full stack and SQL Server.
- README, executed UAT report, coverage report, and categorized top-20 prompt log.
- Two Loom videos within the specified duration limits.

### Defer Unless the Mandatory Flow Is Already Green

- Favourites.
- Advanced price, area, bedroom, and bathroom filters.
- Rejected-listing correction/resubmission workflow.
- MailHog and transactional email outbox.
- Seller profile as a separate entity.
- Refresh tokens.
- ETags and optimistic concurrency.
- Idempotency keys.
- Audit-history screens.
- Background image cleanup.
- Pagination refinements and performance benchmarking.

These are documented scope cuts, not failures, because they do not replace any mandatory gate requirement.

## 4. Recommended Minimal Data Model

| Entity | Purpose |
|---|---|
| `ApplicationUser` | Identity account, role, display name, phone, active/disabled status |
| `City` | Second CRUD entity and validated listing location |
| `Property` | Owner listing, property details, approval state, timestamps |
| `PropertyImage` | Local uploaded-file metadata linked to a property |
| `Enquiry` | Buyer message linked to property, sender, and owner |

Use Identity roles rather than separate Buyer and Seller accounts. A normal `User` can both publish properties and contact other owners. This preserves the agreed flexible-account model and keeps authorization manageable.

## 5. External Integration Decision

Use **Open-Meteo** for property-city weather:

1. Resolve the selected seeded city's stored latitude and longitude.
2. Call the Open-Meteo forecast endpoint from the .NET API.
3. Apply an HTTP timeout of approximately 3–5 seconds.
4. Cache the successful result by city for 30 minutes.
5. Return a friendly `weatherUnavailable` response when the external service fails.
6. Never block the property details page when weather is unavailable.

This is meaningful because buyers can see current conditions for the property's location, and it directly meets the real external API, resilience, and caching requirements.

## 6. Six-Hour Execution Plan

| Time | Work package | Required evidence |
|---|---|---|
| 00:00–00:20 | Start timer; freeze scope; inspect repository; create prompt log; confirm branches and commits | Clean starting state and first planning commit |
| 00:20–01:00 | Scaffold API, React SSR, EF Core, SQL Server, Dockerfiles, Compose, health endpoint | Containers build; API health responds |
| 01:00–01:40 | Identity, registration, JWT login, User/Admin policies, seed Admin | 200 login, 401 missing token, 403 non-admin |
| 01:40–02:35 | `City` and `Property` models, migrations, services, API CRUD, ownership checks | API CRUD works for both entities |
| 02:35–03:15 | React public pages and protected property create/edit/delete UI; basic filters | Core user CRUD works through UI |
| 03:15–03:40 | Secure multi-image upload and retrieval/display | Valid image works; unsafe/oversized file rejected |
| 03:40–04:05 | Admin moderation, user management, role/status changes, live summary cards | Admin flow works through UI |
| 04:05–04:25 | Enquiry/contact flow and Open-Meteo integration with cache/fallback | Enquiry stored; weather success/fallback shown |
| 04:25–05:05 | Complete unit/integration tests; measure and raise coverage to ≥80% | Cobertura and HTML coverage reports |
| 05:05–05:30 | Fresh Docker rebuild; execute UAT; fix only blocking defects | All services healthy; UAT pass/fail recorded |
| 05:30–05:50 | Finalize README, top-20 prompt log, scope-cuts section, repository links | Submission artifacts complete |
| 05:50–06:00 | Final commit/push and evidence capture | Clean status and remote commit verified |

Record Loom videos immediately after the timed build if the assessor treats recording as a submission activity; otherwise reserve approximately 25–35 minutes by shortening UI polish and optional documentation work. Clarify this timing rule before starting the timer.

## 7. Suggested Commit Sequence

Use at least these 15 meaningful checkpoints:

1. `chore: scaffold PropertyHub solution`
2. `chore: add Docker development stack`
3. `feat: configure database and identity`
4. `feat: implement registration and JWT login`
5. `feat: enforce role and account status policies`
6. `feat: add city management API`
7. `feat: implement property CRUD API`
8. `feat: add React SSR public property pages`
9. `feat: add protected property management UI`
10. `feat: secure property image uploads`
11. `feat: add property moderation workflow`
12. `feat: add admin user management and metrics`
13. `feat: add property enquiries`
14. `feat: integrate cached city weather`
15. `test: cover critical backend workflows`
16. `docs: add UAT coverage and prompt evidence`
17. `fix: resolve final Docker UAT defects`

Each commit should build or represent a coherent verified documentation milestone. Do not make empty commits merely to reach the count.

## 8. Testing Strategy for 80% Coverage

- Test service/business logic heavily because it is fast and deterministic.
- Use `WebApplicationFactory` integration tests for register/login, authorization, Property CRUD, City CRUD, moderation, user disablement, upload rejection, and weather fallback.
- Mock the external weather HTTP handler; do not depend on the live API during automated tests.
- Exclude only generated EF Core migrations when calculating the required coverage.
- Generate the first coverage result as soon as the core API exists, then rerun after every major feature.
- Keep controllers thin and move testable rules into small services.
- Commit both Cobertura output and a readable HTML report if repository size remains reasonable.

## 9. Highest-Risk Areas

- Docker networking and SQL Server readiness.
- JWT role/status enforcement producing the correct `401` versus `403`.
- File paths and uploaded-image persistence inside containers.
- Vite SSR hydration and API base URLs.
- Reaching 80% coverage without excluding legitimate application code.
- External API downtime during the demo.
- Role change or account disablement not taking effect until token expiry.
- Spending too long polishing CSS while mandatory admin/test flows are incomplete.

## 10. Gate Definition of Done

The build is ready only when:

- A fresh `docker compose up --build` starts the complete stack.
- The reviewer can register, log in, create/edit/delete a property, upload images, and see it after Admin approval.
- Admin can manage cities, users, roles, account status, listing approval, and dashboard metrics.
- Public pages show only approved listings.
- Missing/invalid authentication returns `401`; an authenticated non-admin receives `403` on admin endpoints.
- Property details show weather or a graceful fallback without breaking.
- Backend coverage is at least 80% and the report is included.
- At least five UAT scenarios are executed and recorded.
- Git history contains at least 15 meaningful commits.
- README, prompt log, coverage report, UAT report, and both Loom links are ready.

## 11. Final Recommendation

Proceed with the sprint, but use this gate MVP instead of attempting every feature in the larger BRD. The first priority is a thin, secure vertical slice that runs in Docker. Once the full user/admin flow and coverage threshold are green, use remaining time for advanced filters, favourites, MailHog, and UI polish—in that order.
