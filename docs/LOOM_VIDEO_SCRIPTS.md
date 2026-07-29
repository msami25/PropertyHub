# PropertyHub Loom Video Scripts

These scripts reflect the completed implementation on `feature/core-implementation` and the
evidence generated on 2026-07-29. They target seven to eight minutes each at a natural speaking
pace. Rehearse once and keep transitions between prepared tabs short.

## Video 1: Technical Walkthrough

**Target duration:** 7 minutes 55 seconds
**Gate limit:** 15 minutes

### Prepare before recording

Open these tabs or editor files in this order:

1. `README.md` at the project summary and architecture sections.
2. The repository tree with `backend`, `frontend`, `tests`, and `docs` expanded.
3. `PropertiesController.cs`, `PropertyService.cs`, `IPropertyRepository.cs`, and
   `PropertyRepository.cs`.
4. `ApplicationDbContext.cs`, the domain entity folder, the two status enums, the configuration
   folder, and the migrations folder.
5. `AuthenticationExtensions.cs`, `ActiveUserAuthorizationHandler.cs`, and
   `AdminService.cs`.
6. `PropertyImageValidator.cs` and `LocalImageStorage.cs`.
7. `App.tsx`, `entry-server.tsx`, `ssrData.ts`, and the frontend `api` folder.
8. `OpenMeteoWeatherClient.cs` and `PropertyWeatherService.cs`.
9. `PropertyServiceTests.cs`, `AuthenticationEndpointTests.cs`,
   `PropertyImageEndpointTests.cs`, `OpenMeteoWeatherClientTests.cs`, and
   `AdminEndpointTests.cs`.
10. `docs/coverage/index.html`, `docs/UAT_REPORT.md`, `docs/GATE_REPORT.md`, and
    `docker-compose.yml`.
11. A terminal containing only a recent successful `docker compose ps` result.

Collapse side panels that could display environment-variable values. Increase the editor and
browser zoom enough for file names and selected code to be readable.

### Pre-recording checklist

- Start the stack with `docker compose up --build` and confirm SQL Server, API, and web are healthy.
- Confirm `http://localhost:3000` and the committed HTML coverage report both open.
- Keep the terminal working directory at the repository root.
- Close `.env`, `appsettings.Development.json`, browser developer storage, request headers, and any
  terminal history containing credentials.
- Do not open generated JWTs, Identity password hashes, connection strings, seed Admin credentials,
  or uploaded files by their physical host path.
- Do not imply that deferred contact reveal, enquiries, favourites, MailHog, or cloud deployment
  are implemented.

### Timestamped word-for-word script

#### 0:00-0:40 — Product and business problem

**Show:** `README.md`, title and current-status sections.

**Say:**

“PropertyHub is a locally deployed, multi-user property marketplace. It solves a simple trust and
workflow problem: owners need to publish and manage sale or rental listings, visitors need to
discover only valid and currently available properties, and administrators need to control
quality and account access. The completed Gate implementation uses a .NET 8 Web API, React 19 with
Vite server-side rendering and hydration, Entity Framework Core, SQL Server, JWT authentication,
Open-Meteo, and Docker Compose. The core Gate journeys are implemented and verified; broader
features such as enquiries and favourites are deliberately documented as deferred.”

#### 0:40-1:25 — Repository and layered solution

**Show:** Repository tree, then the architecture diagram in `README.md`.

**Say:**

“The solution is divided by responsibility. The API project owns HTTP endpoints, authentication
configuration, middleware, and health checks. The Application project contains DTOs, use-case
services, validation, and repository interfaces. Domain contains the entities, enums, roles, and
domain exceptions. Infrastructure contains Identity, Entity Framework configurations and
repositories, local file storage, and the Open-Meteo client. The React application and its Vitest
suite live under `frontend`, while xUnit unit and integration projects live under `tests`.
Submission evidence, including UAT and coverage, is committed under `docs`.”

#### 1:25-2:10 — Controllers, services, repositories, and EF Core

**Show:** `PropertiesController.cs`, then `PropertyService.cs`, `IPropertyRepository.cs`, and
`PropertyRepository.cs`.

**Say:**

“Every application request follows Controllers to Services to domain-specific Repositories to
Entity Framework Core. For example, PropertiesController handles routes, claims, request DTOs, and
HTTP results. PropertyService owns validation, ownership, moderation, availability, and mapping.
IPropertyRepository exposes only Property persistence operations that the use cases need, and
PropertyRepository implements those queries with Entity Framework. City, images, authentication,
and Admin user management use their own focused repositories. Controllers never access the
DbContext directly, and the DbContext remains the unit of work.”

#### 2:10-3:15 — Database model, migrations, and statuses

**Show:** `ApplicationDbContext.cs`, entity files, configurations, enums, then migration filenames.

**Say:**

“ApplicationDbContext extends the Identity DbContext and includes Cities, SellerProfiles,
Properties, PropertyImages, and UserStatusChanges. An ApplicationUser has Identity credentials,
an account status, and a token version. A user can have one SellerProfile, a SellerProfile owns
many Properties, each Property belongs to one City, and a Property has ordered images. Restrict
delete behavior protects referenced users, cities, properties, and audit history.

“Moderation and availability are intentionally separate. ModerationStatus is Pending, Approved,
or Rejected. AvailabilityStatus is Available, Sold, or Rented. Public queries require an approved,
available, non-deleted listing whose owner is active. Properties also retain moderator, moderation
time, rejection reason, and UTC timestamps. User enable and disable operations create immutable
UserStatusChange audit records with the target, Admin, previous and new status, reason, correlation
identifier, and UTC time. The migrations show the initial schema, canonical City seed data,
Property workflow fields, image dimensions, and Admin user management.”

#### 3:15-4:30 — Authentication, authorization, Admin safety, and images

**Show:** `AuthenticationExtensions.cs`, `ActiveUserAuthorizationHandler.cs`, `AdminService.cs`,
then `PropertyImageValidator.cs` and `LocalImageStorage.cs`.

**Say:**

“Registration and login use ASP.NET Core Identity. Public registration always creates a User;
User and Admin roles and the configured Admin are seeded at startup. JWT validation deliberately
checks issuer, audience, signature, lifetime, and zero clock skew. The browser holds its
30-minute access token only in React memory.

“Protected policies do more than validate a token. ActiveUserAuthorizationHandler checks the
account status and token version against the database on every protected request. Disabling an
account or changing its role increments that version, immediately invalidating older tokens.
Admin operations block self-disable and self-demotion, prevent disabling an Admin through user
management, and protect the last active Admin.

“Images are stored outside the web root and streamed through controlled endpoints. Uploads are
owner-scoped and limited to one through five JPEG, PNG, or WebP files of at most five megabytes.
The server validates extension, MIME type, file signature, decoding, dimensions, and pixel count,
then generates collision-resistant storage names. Path resolution blocks traversal, and Docker
persists the files in a named volume.”

#### 4:30-5:15 — React routes and SSR

**Show:** `App.tsx`, `entry-server.tsx`, `ssrData.ts`, then the frontend `api` folder.

**Say:**

“The frontend has dedicated API client modules and never accesses SQL Server. Public routes are
slash, slash properties, and slash properties slash property ID. The Node Vite server fetches
public DTOs, renders meaningful HTML, serializes only public initial data, and React hydrates that
markup in the browser. Login, registration, My Properties, the Admin dashboard, moderation, and
City management use client behavior. Protected data is loaded only after an in-memory session
exists, so owner contact data, Admin data, and access tokens are not emitted into SSR HTML. Both
direct requests and hydrated navigation are covered by regression tests.”

#### 5:15-6:00 — Open-Meteo integration

**Show:** `PropertyWeatherService.cs`, then `OpenMeteoWeatherClient.cs`.

**Say:**

“Weather is meaningful property-location data rather than a disconnected API demo. The weather
service first loads a publicly visible Property and uses its City’s stored latitude and longitude.
A typed HttpClient created by HttpClientFactory calls Open-Meteo with a configurable timeout,
clamped to a safe range and set to five seconds by default. Successful observations are cached by
City for 30 minutes. Non-success responses, timeouts, network errors, malformed JSON, and invalid
measurements return a provider-neutral unavailable result. The Property page remains usable even
when weather is unavailable.”

**Fallback if live weather is unavailable:**

“The provider is unavailable at this moment, and this is the designed fallback: Property details
remain available, the endpoint returns a safe unavailable result, and failures are not cached.”

#### 6:00-6:55 — Representative tests and coverage

**Show:** The prepared unit and integration test files, then `docs/coverage/index.html`.

**Say:**

“The backend suite contains 38 unit tests and 41 integration tests, for 79 backend tests in total.
The current frontend suite contains 31 Vitest tests. Representative service tests cover Property
validation, Pending transitions, moderation, terminal Sold and Rented behavior, weather fallback,
and unsafe Admin actions. Integration tests exercise registration and JWTs, verified 401 and 403
responses, cross-owner protection, City and Property CRUD, secure images, immediate account-token
invalidation, live dashboard metrics, and Open-Meteo success, timeout, failure, invalid response,
and caching.

“This is the committed ReportGenerator HTML summary. The verified backend line coverage is exactly
91.2 percent: 2,382 covered lines out of 2,610 coverable lines. It combines API, Application,
Domain, and Infrastructure coverage. Only generated Entity Framework migration code is excluded
through the committed coverage run settings.”

#### 6:55-7:55 — Docker, UAT, and tradeoffs

**Show:** `docker-compose.yml`, terminal `docker compose ps`, `docs/UAT_REPORT.md`, then
`docs/GATE_REPORT.md`.

**Say:**

“Docker Compose starts SQL Server 2022, the .NET API, and the Vite SSR web server. SQL Server must
be healthy before the API starts, and the API readiness check must pass before the web service
starts. Named volumes preserve SQL data and property uploads. The documented application URL is
localhost port 3000, with the API on port 8081.

“Eight UAT scenarios are recorded as PASS. They cover authentication, authorization, both core
CRUD entities, image security and persistence, weather resilience, Admin management, and a full
restart that preserved database records and image hashes. The main tradeoffs were deliberate:
short-lived in-memory JWTs avoid insecure browser persistence; controlled image streaming favors
security over direct static serving; public-only SSR prevents private-data leakage; and focused
Gate features took priority over optional enquiries, favourites, refresh tokens, and cloud
services. That completes the technical walkthrough of the verified PropertyHub Gate
implementation.”

### Sensitive-screen warnings

- Never show `.env`, user secrets, JWT payloads, Authorization headers, passwords, password hashes,
  SQL connection strings, or the Admin seed configuration.
- Do not expand browser local/session storage or network request headers.
- Coverage HTML is safe to show; raw Cobertura XML can contain machine-oriented paths and is less
  readable.
- If a terminal command fails during recording, do not troubleshoot secrets on screen. Refer to
  the committed UAT evidence and continue.

## Video 2: Non-Technical Demo

**Target duration:** 7 minutes 45 seconds
**Gate limit:** 10 minutes

### Prepare before recording

Use two browser profiles or separate browser windows:

- **User window:** signed out at `http://localhost:3000/properties`.
- **Admin window:** already signed in at `http://localhost:3000/admin`.

Prepare:

- A unique demo email address that has not been registered.
- A password that satisfies the form rules, stored in a password manager and never shown in plain
  text.
- One existing approved, available, image-backed public listing for the opening browse/weather
  sequence.
- One separate disposable active User account named clearly, such as “Loom Role Target”, for Admin
  role and account-status actions. Do not use the seeded Admin or the newly registered demo owner
  as the role-management target.
- A safe JPEG, PNG, or WebP interior image under 5 MB, with no faces, personal documents, addresses,
  metadata, or copyrighted watermark.
- These property values ready to paste:

| Field | Prepared value |
|---|---|
| Title | Garden View Family House |
| Description | Bright family house with a practical layout, secure parking, and nearby daily amenities. |
| Purpose | Sale |
| Type | House |
| City | Lahore |
| Address | Garden View Block, Lahore |
| Price | 32500000 |
| Area | 10 |
| Area unit | Marla |
| Bedrooms | 4 |
| Bathrooms | 4 |
| Contact number | A non-sensitive fictional demo number accepted by the form |

Use clipboard snippets to keep data entry under one minute. The Admin window must already be
authenticated because refreshing it clears the in-memory session.

### Pre-recording checklist

- Confirm all three Docker services are healthy without showing the terminal during this video.
- Confirm the opening public listing still displays an image and opens successfully.
- Confirm the disposable role target is Active and has the User role.
- Confirm the Pending moderation filter is selected in the Admin moderation window.
- Close source code, terminals, Swagger, database tools, developer tools, password managers, and
  environment files.
- Turn off browser notifications and hide bookmarks or tabs containing private names.
- Rehearse switching between the User and Admin windows without displaying credentials.

### Timestamped word-for-word script

#### 0:00-0:45 — Public marketplace

**Show:** `http://localhost:3000/properties`, then use one essential filter if it retains a visible
listing.

**Say:**

“This is PropertyHub, a local marketplace for moderated sale and rental listings. A visitor can
browse without an account, and the public catalogue shows only listings that are approved,
available, not deleted, and owned by an active account. I can narrow the catalogue by City,
purpose, and property type. Each card gives a clear image, location, area, and price without
revealing the owner’s private contact number.”

#### 0:45-1:20 — Property details and weather

**Show:** Open the prepared public Property detail page and scroll to Current weather.

**Say:**

“The detail page presents the gallery, description, price, dimensions, and seller display name.
Below that, PropertyHub uses the selected City’s saved coordinates to show current local weather.
Here I can see the condition, temperature, humidity, wind, and the UTC observation time. This is
useful context for a location visit, and it loads independently so it cannot break the listing.”

**Fallback if live weather is unavailable:**

“Open-Meteo is temporarily unavailable during this recording. PropertyHub handles that safely:
the full listing remains usable and the page shows this friendly unavailable message instead of
an error.”

#### 1:20-2:05 — Register and sign in

**Show:** Register, submit the prepared new account, then Sign in.

**Say:**

“I’ll now create a new account. Registration asks only for a full name, email, and a strong
password; it does not let the visitor choose an Admin role. The account is ready, so I’ll sign in
using the same details. After authentication, PropertyHub opens the owner workspace. The session
is intentionally held only for this browser session, so a reload requires signing in again.”

#### 2:05-3:20 — Create, edit, and upload

**Show:** `/my/properties`. Complete the prepared form, create the Property, scroll to its card,
select Edit, make a small visible description change, save, then upload the prepared image.

**Say:**

“My Properties combines the complete owner workflow. I’ll create a House for Sale in Lahore using
the prepared title, description, location, price, area, rooms, and demo contact number. The contact
number is stored with the owner’s listing but is not exposed on public pages.

“The new Property is created first and immediately enters Pending moderation. It is not public
yet. I can edit every listing field, so I’ll add a short description detail and save the change.
The listing remains Pending. For an already approved listing, a material edit also returns it to
Pending for another review.

“Images are managed separately after creation. I’ll upload this safe image. It now appears on the
owner card, and the card clearly shows both independent states: moderation is Pending and
availability is Available.”

#### 3:20-4:20 — Admin dashboard and user search

**Show:** Switch directly to the prepared Admin browser window at `/admin`. Refresh dashboard, then
search for “Loom Role Target”.

**Say:**

“I’m switching to a separately prepared Admin session, without displaying its credentials. This
dashboard is protected and populated from the live SQL Server database. It shows total,
registered, active, and disabled accounts; total, Pending, Approved, and Rejected properties; and
the total City count. The timestamp confirms when these metrics were read.

“Below the metrics is the searchable user list. I’ll search for the disposable role-management
account so the change is controlled and easy to verify.”

#### 4:20-5:20 — Role and account management

**Show:** Promote “Loom Role Target” to Admin, then change it back to User. Enter a safe reason,
disable it, observe the status/metrics, then enter another reason and enable it.

**Say:**

“An Admin can promote a User to Admin and return the account to User. Each role change invalidates
that account’s previous access immediately. PropertyHub also prevents an Admin from demoting
their own signed-in account or removing the last usable Admin.

“For account access, I’ll enter the reason ‘Loom access-control demonstration’ and disable this
User. The status and live metrics update, and both an existing session and a new login would now
be rejected. I’ll reactivate the account with the reason ‘Loom demonstration complete’. These
status changes create audit records. Admin accounts cannot be disabled through this control, so
the demonstration cannot accidentally lock out the platform.”

#### 5:20-6:05 — Property moderation

**Show:** Open `/admin/properties`, locate Garden View Family House, show its image and details, and
click Approve.

**Say:**

“Next I’ll open Property moderation. The new Garden View Family House appears in Pending with the
owner’s submitted details and image. An Admin can approve it, or reject it with a required reason.
I’ll approve this valid demo listing. Approval changes the moderation status to Approved and makes
the listing eligible for public discovery while its availability remains Available.”

#### 6:05-6:45 — Confirm the owner and public result

**Show:** Switch to the User window without refreshing, select Refresh listings, then Sign out.
Load `/properties` directly in the address bar so the public SSR response is fresh. Locate and open
the new listing.

**Say:**

“Back in the original User session, Refresh listings now shows the Approved status. After signing
out, the property appears in the public catalogue with its uploaded image. Opening the detail page
confirms the public result, while the private contact number remains hidden. Weather uses Lahore’s
stored coordinates and continues to fail gracefully if the provider is unavailable.”

#### 6:45-7:25 — Availability action

**Show:** Sign in again without exposing the password, return to `/my/properties`, click Mark sold,
then Sign out and load `/properties` directly in the address bar for a fresh public result.

**Say:**

“I’ll sign back in and demonstrate listing availability. Because this is a sale listing, the owner
can mark it Sold. Rental listings provide the equivalent Mark rented action. Sold and Rented are
terminal availability states and are separate from Admin moderation. After marking this Property
Sold and returning to the public catalogue, it is no longer publicly discoverable even though its
moderation decision remains Approved.”

#### 7:25-7:45 — User-value summary

**Show:** Public Properties page with the Property absent.

**Say:**

“PropertyHub gives visitors a clean, trusted catalogue, owners a complete listing and image
workflow, and administrators live oversight of content and access. The result is a practical local
marketplace with secure authorization, resilient location data, and clear moderation and
availability states.”

### Sensitive-screen warnings

- Never show a password in plain text, the Admin seed email/password source, JWTs, browser storage,
  network headers, Swagger, SQL tools, terminals, source code, or `.env`.
- Use password-manager autofill or a pre-authenticated second browser profile. Keep password fields
  masked and avoid opening the password-manager popup on screen.
- Use fictional property contact data and a non-sensitive image. Do not upload personal documents,
  family photographs, real phone numbers, or files with visible addresses.
- Do not disable or demote the signed-in Admin. Use only the prepared disposable target and restore
  it to Active/User during the recording.
- Do not refresh authenticated User or Admin pages unnecessarily because JWTs are intentionally
  stored only in React memory.
- If a live action fails, state the expected behavior briefly and move to the already visible
  evidence. Do not open developer tools or troubleshoot credentials on screen.
