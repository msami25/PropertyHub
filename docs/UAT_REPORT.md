# PropertyHub UAT Report

The complete Gate UAT remains `BLOCKED` until all mandatory phases are implemented and exercised.
Phase 2 authentication and Phase 3 City CRUD were manually exercised against the Docker
environment on 2026-07-28. Phases 4 through 6 automated workflows pass and live Docker execution
completed on 2026-07-29. The direct-route SSR defect found during Phase 4 was fixed, and the
affected Docker and browser checks passed on rerun.

| ID | Scenario | Expected result | Actual result | Evidence | Status |
|---|---|---|---|---|---|
| UAT-01 | Register and log in | Active user receives a JWT and can enter protected routes | Registration returned 201, login returned 200, and `/api/auth/me` returned 200 | Docker HTTP probe; `AuthenticationEndpointTests` | PASS |
| UAT-02 | Enforce authorization | Missing/invalid tokens return 401, User-to-Admin access returns 403, and disabled accounts cannot log in or reuse a token | Missing and invalid tokens returned 401; User-to-Admin, disabled login, and disabled existing token returned 403; seeded Admin access returned 200 | Docker HTTP/SQL probe; `AuthenticationEndpointTests` | PASS |
| UAT-03 | Complete City CRUD | Admin lists, creates, edits, and deletes an unused City; referenced deletion returns 409 | Seeded Admin completed create/read/update/delete through Docker API and create/edit/delete through the browser UI; invalid input returned 400, duplicates and referenced deletion returned 409 | Docker HTTP/SQL and browser probes; `CityEndpointTests`; `CityServiceTests`; `city-management.test.tsx` | PASS |
| UAT-04 | Complete Property CRUD | Owner creates, views, edits, marks availability, and deletes a listing; Admin moderation controls public visibility | SQL-backed Property workflow, authorization, moderation, public visibility, direct SSR routes, owner/Admin login, hydration, and public-data privacy passed after the SSR transfer fix | Docker Compose/API/SQL/browser probes; `PropertyEndpointTests`; `PropertyServiceTests`; 22 frontend tests | PASS |
| UAT-05 | Upload property images | Valid images persist and display; unsafe input is rejected | Owner uploaded two decoded PNGs, changed the primary image, deleted a non-last image, and received 409 for last-image deletion; invalid signature returned 400; hidden/public access followed moderation; SSR and hydrated pages displayed the image; the image remained available after API recreation | Docker API/SSR/browser probe; `PropertyImageEndpointTests`; `PropertyImageValidatorTests`; `LocalImageStorageTests`; `property-images.test.tsx` | PASS |
| UAT-06 | Display Open-Meteo weather | Details show weather or a graceful unavailable state | Live City-coordinate weather rendered; an unreachable provider returned a friendly unavailable result while Property API and SSR remained 200 | Docker API/browser probes; `OpenMeteoWeatherClientTests`; `PropertyWeatherServiceTests`; `property-public.test.tsx` | PASS |
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

### UAT-05 live Docker execution

1. Restored and built the .NET solution with zero warnings/errors. All 27 unit and 28 integration
   tests passed. All 24 frontend tests passed, followed by successful production client and SSR
   builds.
2. Confirmed EF reported no pending model changes after generating
   `20260729063300_AddPropertyImageDimensions`.
3. Validated Docker Compose, rebuilt API and web, and recreated only those services. SQL Server and
   both named volumes were preserved; all three services returned healthy.
4. Ran `tests/Invoke-Phase5DockerUat.ps1`. Registration and login returned 201/200, a Property was
   created with 201, and its public detail returned 404 before approval.
5. Uploaded an executable-signature payload declared as JPEG and received 400. Uploaded two
   decodable PNG files and received 200 with exactly one primary image.
6. Confirmed the owner could read the hidden image with 200 and a `nosniff` header while anonymous
   access returned 404.
7. Approved the Property as Admin. Public detail and controlled image retrieval returned 200, and
   direct SSR HTML contained both the listing title and image URL.
8. Changed the primary image and deleted the other image, which returned the listing to Pending.
   Reapproval succeeded; deletion of the last image returned 409.
9. Recreated only the API service. The named volume remained
   `propertyhub_propertyhub-uploads`, and the retained image still returned 200. Evidence Property:
   `b26cefd4-5632-4066-92cc-fec75e29fb2e`.
10. Loaded the public list and detail directly in the browser. The image completed with natural
    width 1, client navigation and direct SSR hydration both worked, and the browser console
    contained no warnings or errors. UAT-05 is `PASS`.

### UAT-06 live Docker execution

1. Restored and built the .NET solution with zero warnings/errors. All 30 unit and 36 integration
   tests passed. All 26 frontend tests passed, followed by successful production client and SSR
   builds.
2. Confirmed EF reported no pending model changes and Docker Compose configuration was valid.
   Rebuilt and recreated only API and web; SQL Server and the named SQL/upload volumes were
   preserved, and all three services became healthy.
3. Requested weather for approved Property
   `b26cefd4-5632-4066-92cc-fec75e29fb2e`. Its stored Faisalabad City coordinates produced an
   available current observation and a friendly condition. The first request completed in 659 ms;
   an immediate second request completed in 8 ms with the identical payload, confirming the
   successful-result cache.
4. Recreated only API with a deliberately unreachable local provider address and a one-second
   timeout. The weather endpoint returned HTTP 200 with `isAvailable: false`; the public Property
   API and direct SSR detail page both continued to return HTTP 200.
5. Restored the real provider configuration, recreated only API, and confirmed weather became
   available again. No named volume was deleted or recreated; the uploads volume remained
   `propertyhub_propertyhub-uploads`.
6. Loaded the Property detail URL directly in the browser. The hydrated page displayed “Current
   weather in Faisalabad” with condition, temperature, humidity, wind, and a UTC observation time.
   A second direct navigation also rendered weather with no browser console or hydration warnings
   or errors, and no provider-specific details appeared in the page. UAT-06 is `PASS`.

Remaining scenarios will receive the same evidence and a truthful `PASS` or `FAIL` only when
implemented and manually executed.
