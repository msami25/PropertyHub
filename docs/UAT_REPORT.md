# PropertyHub UAT Report

The complete Gate UAT remains `BLOCKED` until all mandatory phases are implemented and exercised.
Phase 2 authentication and Phase 3 City CRUD were manually exercised against the Docker
environment on 2026-07-28. Phase 4 automated workflows pass and live Docker execution completed on
2026-07-29. The direct-route SSR defect found during the first execution was fixed, and the affected
Docker and browser checks passed on rerun.

| ID | Scenario | Expected result | Actual result | Evidence | Status |
|---|---|---|---|---|---|
| UAT-01 | Register and log in | Active user receives a JWT and can enter protected routes | Registration returned 201, login returned 200, and `/api/auth/me` returned 200 | Docker HTTP probe; `AuthenticationEndpointTests` | PASS |
| UAT-02 | Enforce authorization | Missing/invalid tokens return 401, User-to-Admin access returns 403, and disabled accounts cannot log in or reuse a token | Missing and invalid tokens returned 401; User-to-Admin, disabled login, and disabled existing token returned 403; seeded Admin access returned 200 | Docker HTTP/SQL probe; `AuthenticationEndpointTests` | PASS |
| UAT-03 | Complete City CRUD | Admin lists, creates, edits, and deletes an unused City; referenced deletion returns 409 | Seeded Admin completed create/read/update/delete through Docker API and create/edit/delete through the browser UI; invalid input returned 400, duplicates and referenced deletion returned 409 | Docker HTTP/SQL and browser probes; `CityEndpointTests`; `CityServiceTests`; `city-management.test.tsx` | PASS |
| UAT-04 | Complete Property CRUD | Owner creates, views, edits, marks availability, and deletes a listing; Admin moderation controls public visibility | SQL-backed Property workflow, authorization, moderation, public visibility, direct SSR routes, owner/Admin login, hydration, and public-data privacy passed after the SSR transfer fix | Docker Compose/API/SQL/browser probes; `PropertyEndpointTests`; `PropertyServiceTests`; 22 frontend tests | PASS |
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

### UAT-04 live Docker execution

1. Ran `docker compose --env-file .env.example up -d --build` without deleting or recreating named
   volumes. Both images built and SQL Server, API, and web became healthy.
2. Confirmed API `/health/live`, API `/health/ready`, and web `/health` returned HTTP 200.
3. Confirmed migration `20260728203146_AddPropertyWorkflowFields` was present in the preserved SQL
   database.
4. Registered two disposable Users and logged in as both Users and the seeded Admin.
5. Confirmed missing authentication returned `401`, User-to-Admin access returned `403`, and a
   cross-owner property read returned `404`.
6. Created a sale property and confirmed it was `Pending` and publicly hidden. Admin approval made
   it visible through public detail and City/purpose/type-filtered list endpoints.
7. Confirmed the public SSR detail response contained the approved title but contained neither the
   contact number nor an access-token marker.
8. Edited the approved property and confirmed it returned to `Pending` and disappeared publicly.
   Reapproval restored visibility; marking it `Sold` removed it publicly.
9. Created a rental property and confirmed rejection without a reason returned `400`. Rejection
   with a reason succeeded and remained hidden. Owner correction returned it to `Pending`; Admin
   approval followed by marking it `Rented` removed it publicly.
10. Soft-deleted both listings and confirmed each delete returned `204` and the owner read returned
    `404`. SQL Server retained two soft-deleted rows.
11. Entered through the public SSR page, navigated client-side to login, and confirmed the owner
    management and Admin moderation UIs loaded from the live API with no browser console warnings
    or errors.
12. Directly requested `/login`, `/register`, `/my/properties`, and `/admin/properties`; every route
    returned HTTP 500. The web container logged
    `TypeError: Cannot read properties of undefined (reading 'replaceAll')` from the SSR serializer,
    producing the initial UAT failure.
13. Traced the failure to `loadPublicPageData` returning `undefined` for non-public routes,
    `JSON.stringify(undefined)` returning a non-string `undefined`, and the server serializer then
    calling `replaceAll` on that value.
14. Changed the SSR transfer boundary to represent absent public data as JSON `null`. Added direct
    SSR regression coverage for all four affected routes; the full frontend suite passed 22 tests,
    and both production client and SSR builds passed.
15. Rebuilt and recreated only the `web` service. API, SQL Server, and named volumes were not
    recreated or removed; all three services remained healthy.
16. Confirmed `/login`, `/register`, `/my/properties`, and `/admin/properties` each returned HTTP
    200 with the expected server-rendered heading, an initial-data payload of `null`, and no contact
    number or access-token marker.
17. Loaded all four routes directly in the browser and confirmed hydration produced no console
    warnings or errors. Direct owner and Admin login reached their protected routes.
18. Created and approved a disposable SQL-backed Property, confirmed its direct public SSR response
    and hydrated page contained the title but no contact number or token marker, then soft-deleted
    it and confirmed the public API returned `404`. UAT-04 is `PASS`.

Remaining scenarios will receive the same evidence and a truthful `PASS` or `FAIL` only when
implemented and manually executed.
