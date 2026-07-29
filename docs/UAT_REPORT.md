# PropertyHub UAT Report

The complete Gate UAT remains `BLOCKED` until all mandatory phases are implemented and exercised.
Phase 2 authentication and Phase 3 City CRUD were manually exercised against the Docker
environment on 2026-07-28. Phase 4 automated workflows pass, but live Docker execution is
`BLOCKED` by Docker Desktop failing to initialize its engine on 2026-07-29.

| ID | Scenario | Expected result | Actual result | Evidence | Status |
|---|---|---|---|---|---|
| UAT-01 | Register and log in | Active user receives a JWT and can enter protected routes | Registration returned 201, login returned 200, and `/api/auth/me` returned 200 | Docker HTTP probe; `AuthenticationEndpointTests` | PASS |
| UAT-02 | Enforce authorization | Missing/invalid tokens return 401, User-to-Admin access returns 403, and disabled accounts cannot log in or reuse a token | Missing and invalid tokens returned 401; User-to-Admin, disabled login, and disabled existing token returned 403; seeded Admin access returned 200 | Docker HTTP/SQL probe; `AuthenticationEndpointTests` | PASS |
| UAT-03 | Complete City CRUD | Admin lists, creates, edits, and deletes an unused City; referenced deletion returns 409 | Seeded Admin completed create/read/update/delete through Docker API and create/edit/delete through the browser UI; invalid input returned 400, duplicates and referenced deletion returned 409 | Docker HTTP/SQL and browser probes; `CityEndpointTests`; `CityServiceTests`; `city-management.test.tsx` | PASS |
| UAT-04 | Complete Property CRUD | Owner creates, views, edits, marks availability, and deletes a listing; Admin moderation controls public visibility | API and UI are implemented; 6 API integration, 6 service unit, and 7 focused frontend tests pass; live Docker journey was not executed because the Docker engine did not become responsive | `PropertyEndpointTests`; `PropertyServiceTests`; property frontend and SSR tests; Docker Desktop logs show control API timeouts | BLOCKED |
| UAT-05 | Upload property images | Valid images persist and display; unsafe input is rejected | Not implemented | Pending | BLOCKED |
| UAT-06 | Display Open-Meteo weather | Details show weather or a graceful unavailable state | Not implemented | Pending | BLOCKED |
| UAT-07 | Manage users and metrics | Admin changes roles/status and sees live database counts | Not implemented | Pending | BLOCKED |
| UAT-08 | Restart Docker services | SQL data and uploaded images survive a normal restart | Not implemented | Pending | BLOCKED |

## Phase 2 execution steps

### UAT-01

1. Started SQL Server, API, and SSR frontend with Docker Compose.
2. Confirmed API readiness and frontend health returned 200.
3. Registered a new account without a client-controlled role.
4. Logged in and called the protected current-user endpoint with the returned access token.

### UAT-02

1. Called the current-user endpoint without a token and with a malformed token.
2. Called the Admin session endpoint with a User token and with the seeded Admin token.
3. Disabled the disposable User directly in the verification database and incremented its token
   version.
4. Retried the protected endpoint with the existing token and retried login.

### UAT-03

1. Started the complete Docker stack and confirmed SQL Server, API, and SSR frontend were healthy.
2. Confirmed the public endpoint returned eight seeded active cities.
3. Confirmed missing authentication returned 401 and a User token returned 403 for Admin City
   access.
4. Signed in as the seeded Admin and created, read, edited, deactivated, listed, and deleted a City.
5. Confirmed the inactive City disappeared from public reference data, invalid input returned 400,
   a duplicate normalized name returned 409, and a deleted City returned 404.
6. Added temporary SQL-backed Property reference data, confirmed deletion returned 409, removed the
   reference, and confirmed deletion then returned 204.
7. Repeated create, edit, and delete through the browser management UI and confirmed visible
   success states with no browser console warnings or errors.

### UAT-04 automated evidence and Docker blocker

1. Exercised owner create, read, edit, availability, and soft-delete workflows through the
   integration host.
2. Confirmed another owner receives `404`, missing authentication receives `401`, and a
   non-Admin receives `403` on moderation operations.
3. Confirmed pending and rejected properties are not public; approval makes an available property
   public; sold properties and properties owned by disabled accounts disappear from public data.
4. Confirmed public SSR includes property content but not owner contact data.
5. Validated the Compose model successfully with `.env.example`.
6. Attempted Docker engine recovery through Docker Desktop restart and a Docker-only WSL VM
   restart. New sessions remained in `starting`; engine `_ping` and init control API calls timed
   out. No live Phase 4 Docker request was possible, so UAT-04 remains `BLOCKED`.

Remaining scenarios will receive the same evidence and a truthful `PASS` or `FAIL` only when
implemented and manually executed.
