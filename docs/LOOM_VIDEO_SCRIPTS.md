# PropertyHub Loom Recording Guide and Scripts

The first recordings have been supplied, but both exceed the assessment duration limits and must
be trimmed or re-recorded before submission.

- [Current technical walkthrough](https://www.loom.com/share/44140a0d4f874900b57b5cedabaa499a):
  approximately 18 minutes; maximum permitted duration is 15 minutes.
- [Current non-technical demo](https://www.loom.com/share/686d9dcdd6f049288a95b11e90c1a9c2):
  approximately 11 minutes; maximum permitted duration is 10 minutes.

The scripts use only implemented behavior and retained evidence. Speak at a calm conversational
pace, follow the prepared screen sequence, and do not pause to troubleshoot during a recording.

## Shared preparation

Before either recording:

1. Start PropertyHub with `docker compose up --build` and wait for SQL Server, API, and web to be
   healthy.
2. Open `http://localhost:3000` and confirm one approved, Available listing has an image and
   weather details.
3. Close `.env`, password managers, database tools, Swagger, browser developer tools, email,
   notifications, and unrelated tabs.
4. Use a clean browser profile and a neutral desktop background. Hide bookmarks and account
   avatars that reveal private information.
5. Set browser zoom to 100%, terminal font to a readable size, and repository secrets to hidden.
6. Run one complete dry run and time it. Shorten pauses, not required content.

Never display passwords, the JWT signing key, access tokens, authorization headers, password
hashes, SQL credentials, Admin seed values, `.env`, or real contact information.

---

## Video 1 — Technical Walkthrough

**Target duration:** 11 minutes 40 seconds

**Assessment requirement:** 10–15 minutes
**Audience:** Technical reviewer

### Prepare these tabs and files

Open in this order before recording:

1. Root [`README.md`](../README.md), positioned at the architecture diagram.
2. Repository tree with `src`, `frontend`, `tests`, `adr`, and `docs` visible.
3. [`docs/ARCHITECTURE.md`](ARCHITECTURE.md).
4. [`adr/README.md`](../adr/README.md) and ADRs 0001, 0002, and 0003.
5. `frontend/propertyhub-web/src/pages/MyPropertiesPage.tsx` and
   `frontend/propertyhub-web/src/api/propertyApi.ts`.
6. `backend/PropertyHub.Api/Controllers/PropertiesController.cs`.
7. `backend/PropertyHub.Application/Services/PropertyService.cs` and
   `backend/PropertyHub.Application/Interfaces/Repositories/IPropertyRepository.cs`.
8. `backend/PropertyHub.Infrastructure/Data/Repositories/PropertyRepository.cs` and
   `backend/PropertyHub.Infrastructure/Data/ApplicationDbContext.cs`.
9. Domain entities/enums and the migrations folder.
10. Authentication, active-user authorization, image validation/storage, SSR, and weather files.
11. Representative backend and frontend test files.
12. [`docs/coverage/index.html`](coverage/index.html), [`uat-report.md`](../uat-report.md), and
    [`ai-collaboration-log.md`](../ai-collaboration-log.md).
13. A terminal showing only `docker compose ps`; clear command history first.

Do not open raw Cobertura XML during the recording. The HTML coverage report is safer and easier to
read.

### Timestamped word-for-word script

#### 0:00–0:50 — Product and business problem

**Show:** README title, status, and value proposition.

**Say:**

“PropertyHub is a locally deployed, multi-user property marketplace built for the capstone Gate.
It addresses a trust problem in informal property advertising: visitors need useful listings,
owners need a straightforward way to manage them, and administrators need to control both content
and platform access. Registered users create and maintain sale or rental listings, upload images,
and control whether a listing is Available, Sold, or Rented. Administrators moderate listings,
manage Cities and accounts, and see live database metrics. Public visitors see only approved,
active results. The completed solution uses .NET 8, React 19 with Vite server-side rendering,
SQL Server, Open-Meteo, and Docker Compose.”

#### 0:50–1:40 — Repository structure

**Show:** Repository tree; expand `src`, `frontend`, `tests`, `adr`, and `docs`.

**Say:**

“The repository is organized around clear responsibilities. Under `src`, the API project owns HTTP
controllers, middleware, authentication configuration, startup, and health endpoints. Application
contains request and response records, validation, use-case services, and repository contracts.
Domain contains entities, enums, role names, and domain exceptions. Infrastructure contains the
Identity and Entity Framework implementation, entity configurations, migrations, repositories,
local image storage, and the Open-Meteo client.

“The React SSR application and Vitest tests are under `frontend`. Backend unit and integration
projects are under `tests`. The root ADR folder records important decisions. The docs folder holds
the canonical Gate, detailed UAT, scope decisions, coverage evidence, and these recording scripts.
That separation makes the implementation and its proof easy to navigate.”

#### 1:40–2:35 — Architecture and decisions

**Show:** Mermaid overview in `docs/ARCHITECTURE.md`, then the ADR index and ADR 0001.

**Say:**

“This Mermaid diagram shows the deployed and logical architecture. The browser receives HTML from
the React and Vite SSR container and hydrates the same React application. Both SSR and browser API
clients call the ASP.NET Core container; neither frontend process accesses SQL Server. The API
coordinates SQL Server, protected image storage, and Open-Meteo. Docker provides a private network,
health-gated startup, and separate persistent volumes for database data and uploaded files.

“Architecture decisions are recorded rather than left implicit. ADR 0001 chooses the required
Controller, Service, domain-specific Repository, Entity Framework flow. ADR 0002 relates that
layering to JWT authorization and immediate token invalidation. ADR 0003 explains why images are
outside the public web root. ADRs also cover public-route SSR and resilient weather integration.
Each uses the same Title, Status, Context, Decision, and Consequences structure, including the
trade-offs we accepted.”

#### 2:35–4:05 — One complete React-to-database flow

**Show:** In sequence: `MyPropertiesPage.tsx`, frontend properties API module,
`PropertiesController.cs`, `PropertyService.cs`, `IPropertyRepository.cs`,
`PropertyRepository.cs`, and `ApplicationDbContext.cs`.

**Say:**

“Here is one complete vertical flow: an owner edits a Property. My Properties renders the existing
data, collects the unchanged contract fields, shows validation and submission states, and calls
the dedicated frontend Property API client. That client sends the authenticated HTTP request to
the Property endpoint; React never knows anything about database connections.

“PropertiesController is the HTTP boundary. It applies authorization, reads the authenticated user
identifier, accepts the DTO and cancellation token, and maps the service result to the correct
HTTP response. It contains no Entity Framework query.

“PropertyService is the use-case boundary. It validates the requested City and domain values,
loads the owner-scoped listing, enforces ownership, applies material changes, returns an edited
approved listing to Pending moderation, and maps the entity to a response DTO. It depends on the
domain-specific `IPropertyRepository`, not on the DbContext.

“PropertyRepository implements only the persistence operations this domain needs. It scopes owner
queries, projects public results, loads required relationships, uses no-tracking reads where
appropriate, and saves through ApplicationDbContext. Entity Framework then generates parameterized
SQL for SQL Server. The DbContext is the unit of work. This exact dependency direction also applies
to City, images, authentication, and Admin management, while keeping each repository focused
instead of introducing a ceremonial generic repository.”

#### 4:05–5:05 — Data model, relationships, migrations, and states

**Show:** Architecture ER diagram, entity files, status enums, then the five migration filenames.

**Say:**

“ApplicationUser extends Identity with display data, account status, and a token version. A user
may have one SellerProfile, and that profile owns Properties. Each Property references one
normalized City and has ordered PropertyImages. UserStatusChange records Admin account actions.
Explicit foreign keys and restrictive delete behavior protect referenced Cities, users,
Properties, images, and audit history.

“Moderation and availability are deliberately separate. Moderation is Pending, Approved, or
Rejected. Availability is Available, Sold, or Rented. New and materially edited listings become
Pending. Public queries require Approved plus Available, not soft-deleted, and an active owner.
Owners can mark sale listings Sold or rentals Rented without rewriting the moderation decision.

“The migrations show the evolution from the initial schema through canonical City seed data,
Property workflow fields, image dimensions, and Admin user management. Persisted timestamps use
UTC, and configuration lives in separate Entity Framework configuration classes.”

#### 5:05–6:15 — Identity, JWT authorization, and Admin protections

**Show:** Authentication registration, JWT service/options, active-user handler, and Admin service.

**Say:**

“Registration and login use ASP.NET Core Identity, so passwords are hashed and public registration
always creates a User rather than accepting a client-selected role. User and Admin roles and one
configured Admin are seeded at startup from environment configuration. Secrets are represented
only by placeholders in the repository.

“JWT validation checks issuer, audience, signing key, lifetime, and zero clock skew. The browser
keeps its short-lived access token only in React memory, so it is not written to local storage or
server-rendered HTML.

“A valid signature alone is not enough. The ActiveUser policy checks current account status and
token version in SQL Server on every protected request. Disabling an account or changing its role
increments that version, so an already-issued token loses access immediately. Tests verify 401
for missing or invalid authentication and 403 for an authenticated User calling Admin endpoints.
Admin rules block self-disable and self-demotion, prevent unsafe Admin disabling, and protect the
last usable administrator.”

#### 6:15–7:05 — Protected image storage

**Show:** Image controller/service, validator, local storage, and Docker upload volume.

**Say:**

“Property creation and image upload are separate operations. An owner creates a listing, then uses
protected image endpoints to upload between one and five files. The server accepts only JPEG, PNG,
and WebP and validates extension, declared MIME type, file signature, decoded image, file size,
dimensions, and pixel count. It generates collision-resistant storage names and never trusts the
original filename as a path.

“Files are outside the web root. LocalImageStorage resolves every path beneath its configured root,
blocking traversal, while controlled API endpoints enforce ownership and public visibility and add
safe response headers. Metadata, order, and primary-image state are stored in SQL Server; bytes are
stored in the named upload volume. This favors control and privacy over the simplicity of public
static-file serving.”

#### 7:05–7:55 — React SSR and privacy boundary

**Show:** React route configuration, `entry-server.tsx`, SSR data loader/serializer, and SSR tests.

**Say:**

“The public home, listing, and Property details routes are rendered in the Node Vite server and
then hydrated in the browser. The server fetches only public response DTOs, generates meaningful
HTML, and serializes the initial payload through a strict JSON boundary. Authenticated owner and
Admin pages behave as client applications and fetch protected data only after an in-memory session
exists.

“A Docker UAT found that absent data for direct private-route requests was represented as
JavaScript `undefined`, which JSON serialization could not convert into a string before escaping.
The smallest correct fix represents that intentional absence as JSON `null`. Four direct-render
regressions cover login, register, My Properties, and Admin Properties. Direct requests now return
200, hydration is clean, and SSR HTML contains neither access tokens nor protected contact data.”

#### 7:55–8:45 — Open-Meteo resilience

**Show:** Property weather service, typed Open-Meteo client, registration/options, and weather UI.

**Say:**

“Weather uses actual Property location data. The service first loads a publicly visible Property,
then takes latitude and longitude from its City. A typed HttpClient created by HttpClientFactory
calls Open-Meteo with a finite configurable timeout, safely bounded and five seconds by default.
Successful observations are cached by City for thirty minutes.

“Non-success status codes, timeouts, network failures, malformed JSON, and invalid measurements
produce a provider-neutral unavailable response. Failures are not allowed to break or replace the
Property page. The React details page independently shows condition, temperature, humidity, wind,
and UTC observation time when available.”

**Safe fallback if live weather is unavailable:**

“Open-Meteo is unavailable at this moment, and this is the designed fallback: the complete
Property remains available, the page shows a safe unavailable message, and a transient failure is
not cached as successful data.”

#### 8:45–9:55 — Tests and exact coverage

**Show:** A Property service unit test, authentication or image integration test, weather client
test, SSR/frontend test, then `docs/coverage/index.html`.

**Say:**

“The backend has 38 unit tests and 41 integration tests, 79 in total. Unit tests focus on business
rules such as Property validation and transitions, City conflicts, weather fallback and caching,
file validation, and unsafe Admin actions. Integration tests exercise real endpoint workflows:
registration and JWTs, 401 and 403 boundaries, cross-owner protection, City and Property CRUD,
moderation, image authorization, immediate disabled-account rejection, user management, and
dashboard metrics. Open-Meteo tests cover success, timeout, provider failure, invalid response,
and cached results.

“The current frontend suite contains 31 Vitest tests, including direct SSR regressions and owner,
Admin, public listing, form, image, weather, and navigation behavior. Both client and SSR production
builds pass.

“This committed HTML report was generated by Coverlet and ReportGenerator. Verified backend line
coverage is exactly 91.2 percent: 2,382 covered lines out of 2,610 coverable lines. API,
Application, Domain, and Infrastructure are measured. Only generated Entity Framework migrations
are excluded; application code was not hidden to inflate the number.”

#### 9:55–10:45 — AI collaboration evidence

**Show:** Root `ai-collaboration-log.md`; briefly highlight prompts 2, 16, and 14.

**Say:**

“AI assistance is documented as reviewed collaboration rather than unquestioned generation.
Prompt two enforced Controllers to Services to domain-specific Repositories before implementation.
Prompt sixteen required reproducing the direct-route SSR failure, capturing the exact undefined
value, and fixing the transfer boundary instead of adding blind optional chaining. Prompt fourteen
required honest coverage above eighty percent without weakening tests or excluding application
code.

“Each of the exactly twenty entries records its phase, exact supported prompt, purpose, and whether
the result was accepted or modified. Outputs were checked against the canonical Gate, tests,
Docker behavior, diffs, and retained evidence. Deferrals and uncertainty are recorded instead of
being presented as completed work.”

#### 10:45–11:40 — Docker, UAT, trade-offs, and lessons learned

**Show:** Docker deployment diagram, terminal `docker compose ps`, root `uat-report.md`, then README
Loom placeholders.

**Say:**

“Docker Compose starts SQL Server 2022, the .NET API, and the React Vite SSR server. SQL health
gates API startup, and API readiness gates the web service. The private network keeps database
traffic internal. Named volumes preserve SQL data and protected uploads. A full restart UAT
confirmed healthy recovery and preserved users, Cities, Properties, image content, weather access,
and both volume identities.

“The capstone UAT summarizes more than ten major acceptance tests and links to detailed executed
steps. The mandatory application Gate is supported by passing tests, 91.2 percent backend
coverage, live browser and API checks, Docker health, and persistence evidence. The report keeps
the unrecorded narrow-viewport visual pass as not run, and the Loom recordings remain blocked until
their real links are supplied.

“The central trade-offs were deliberate: memory-only JWTs improve browser storage safety but
require login after refresh; controlled image streaming improves authorization at extra API cost;
public-only SSR protects private data while authenticated dashboards remain client-driven; and
focused Gate features took priority over optional enquiries, favourites, advanced filters, and
email infrastructure. The main lesson was that architecture, security, and evidence must evolve
together. Small vertical slices and live Docker UAT found issues that isolated builds could not.
That completes the technical walkthrough.”

### Technical recording warnings

- Keep the terminal limited to `docker compose ps`; do not scroll into command history.
- Do not show `.env`, `appsettings` secrets, JWTs, network headers, SQL tools, Admin seed
  credentials, or browser storage.
- Do not claim that optional deferred features exist.
- If a file takes too long to open, continue with the prepared architecture or UAT tab rather than
  searching live.
- If weather is unavailable, use the exact fallback wording above; this demonstrates implemented
  resilience rather than a failed demo.

---

## Video 2 — Non-Technical Demo

**Target duration:** 7 minutes 45 seconds

**Assessment requirement:** 5–10 minutes
**Audience:** Product owner or non-technical reviewer

### Prepare accounts, data, image, and browser windows

Use two browser profiles or separate windows:

- **User window:** signed out at `http://localhost:3000/properties`.
- **Admin window:** already authenticated at `http://localhost:3000/admin`. Do not expose how it
  was authenticated.

Prepare:

- A unique unused demo email and a strong password stored in a password manager.
- An existing approved, Available, image-backed public listing for the opening.
- A separate disposable active User named `Loom Role Target` for role/status actions. Do not use
  the seeded Admin or new demo owner.
- A safe JPEG, PNG, or WebP image under 5 MB with no faces, documents, addresses, watermark, or
  sensitive metadata.
- Clipboard snippets for this listing:

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
| Contact number | A fictional number accepted by the form |

### Non-technical dry-run checklist

- Confirm all three services are healthy before opening the recording software.
- Confirm the prepared public listing and weather section load.
- Confirm the disposable Admin target is Active with the User role.
- Confirm the new demo email is unused and the prepared image is accepted.
- At mobile width, check the navigation button, catalogue, details, form, owner cards, and Admin
  dashboard for usability and horizontal overflow. Record any defect instead of hiding it.
- Return the browser to desktop width for the opening.
- Close code, terminals, Swagger, database tools, developer tools, password managers, and private
  tabs.
- Rehearse the entire flow without refreshing authenticated pages because the session is
  intentionally memory-only.

### Timestamped word-for-word script

#### 0:00–0:40 — Public marketplace

**Show:** `/properties`; apply one essential filter only if a result remains visible.

**Say:**

“This is PropertyHub, a moderated local marketplace for property sales and rentals. Anyone can
browse without creating an account. The catalogue shows a clear image, price, City, property type,
purpose, and area, and I can narrow it by City, purpose, or type. Only approved listings that are
still Available appear here, so Pending, Rejected, Sold, Rented, deleted, or disabled-owner
properties do not appear. Private owner contact details are not exposed.”

#### 0:40–1:15 — Details and meaningful weather

**Show:** Open the prepared listing and scroll to Current weather.

**Say:**

“The detail page brings together the gallery, description, price, dimensions, and seller display
name. It also uses this listing’s City location to show current weather, including condition,
temperature, humidity, wind, and observation time. That is useful context when planning a visit,
and it loads separately so the listing remains available if the weather provider has a problem.”

**Safe fallback if live weather is unavailable:**

“Open-Meteo is temporarily unavailable during this recording. PropertyHub handles that safely:
the complete listing still works and the page shows this friendly unavailable message instead of
an application error.”

#### 1:15–1:55 — Register and sign in

**Show:** Register using prepared data, then sign in with the masked password.

**Say:**

“I’ll create a normal user account. Registration asks for a name, email, and strong password; it
does not allow someone to make themselves an administrator. The account is ready, so I’ll sign in
with the same details. PropertyHub now opens the owner workspace. The session is deliberately
temporary for browser safety, so refreshing would require signing in again.”

#### 1:55–3:10 — Create, edit, upload, and show status

**Show:** `/my/properties`; create the prepared listing, edit one sentence, save, and upload the
prepared image.

**Say:**

“My Properties contains the complete owner workflow. I’ll add this prepared House for Sale in
Lahore with its location, price, area, rooms, and fictional contact number. The contact number
belongs to the protected owner record and is not shown publicly.

“The Property is created first and immediately displays Pending moderation and Available status.
It is not public yet. I can edit all listing details, so I’ll add this small description change and
save. A new or materially edited listing returns to Pending so an administrator can review what
the public will see.

“Images are added separately after creation. I’ll upload this safe image, and it now appears on the
owner card. The card keeps moderation and availability distinct and retains Edit, image management,
delete, and Sold or Rented actions.”

#### 3:10–3:45 — Responsive experience

**Show:** Narrow the User browser to a mobile viewport; open the mobile navigation; briefly show
the owner card and Add Property form, then return to desktop width.

**Say:**

“PropertyHub adapts to a smaller screen without changing the workflow. The navigation becomes an
accessible menu, controls remain keyboard-friendly, the Property card stacks its content, and the
form remains readable without horizontal scrolling. The same actions and status labels stay
available on mobile, tablet, and desktop.”

If the dry run revealed a responsive defect, do not use the sentence above. State the limitation
honestly and show only the working viewport.

#### 3:45–4:30 — Admin dashboard and user search

**Show:** Switch to the prepared Admin window at `/admin`; refresh metrics and search for
`Loom Role Target`.

**Say:**

“I’m switching to a separate prepared Admin session without displaying credentials. This protected
dashboard uses live platform data. It shows total, active, and disabled users; total, Pending,
Approved, and Rejected Properties; and the City count.

“Below the metrics is a searchable user list. I’ll find the disposable Loom Role Target account so
the access demonstration does not affect the owner or the signed-in administrator.”

#### 4:30–5:20 — One Admin access action

**Show:** Promote target to Admin, return it to User, disable with a safe reason, then reactivate.

**Say:**

“An administrator can promote a User and return that account to the User role. Each role change
ends the account’s previous access immediately. PropertyHub prevents the signed-in Admin from
demoting or disabling themselves and protects the last usable administrator.

“I’ll enter ‘Loom access-control demonstration’ and disable this disposable User. Its status and
the live metrics change, and both an existing session and a new login are rejected. I’ll now
reactivate it with ‘Loom demonstration complete’. These actions are audited, and the target is
restored to its original safe state.”

#### 5:20–6:00 — Moderate the new listing

**Show:** `/admin/properties`; locate Garden View Family House and approve it.

**Say:**

“The new Garden View Family House is waiting in Property moderation with its submitted details and
image. An Admin can approve it or reject it with a required explanation. I’ll approve this valid
demo listing. Moderation is now Approved while availability remains Available, making it eligible
for the public catalogue.”

#### 6:00–6:40 — Confirm public outcome

**Show:** Return to User window without refreshing, refresh listings, sign out, then enter
`/properties` directly and open the new listing.

**Say:**

“Back in the original owner session, Refresh listings shows the Approved decision. I’ll sign out
and return to the public catalogue. The listing now appears with its image. Its detail page shows
the public information and Lahore weather while keeping the fictional contact number private.”

#### 6:40–7:30 — Mark Sold, delete, and confirm removal

**Show:** Sign in again with the masked password, open My Properties, choose Mark sold, sign out,
observe the Sold badge, then delete the disposable listing, sign out, and load `/properties`
directly.

**Say:**

“I’ll sign in once more to demonstrate availability. Because this is a sale listing, the owner can
mark it Sold. Rental listings provide the matching Mark rented action. Sold and Rented are final
availability states, separate from the Admin’s moderation decision. The Sold badge confirms that
transition. I have now demonstrated create, public read, edit, and availability management. To
complete the core CRUD journey and clean up the disposable data, I’ll delete this owner listing.
After signing out and returning to the public catalogue, the deleted Property no longer appears.”

#### 7:30–7:45 — User-value summary

**Show:** Public catalogue with the sold listing absent.

**Say:**

“PropertyHub gives visitors a trusted catalogue, owners a complete listing and image workflow, and
administrators live control over content and access. It combines clear status, secure account
rules, resilient local information, and persistent local deployment in one practical marketplace.”

### Non-technical recording warnings

- Do not show code, terminals, Swagger, SQL tools, browser developer tools, secrets, tokens, or
  credentials.
- Keep passwords masked and avoid recording password-manager pop-ups.
- Use fictional contact data and a non-sensitive image.
- Do not disable or demote the signed-in Admin. Restore the disposable target to Active/User.
- Do not refresh an authenticated window unless signing in again is part of the script.
- If weather is unavailable, use the prepared fallback; do not open developer tools.
- If a live mutation fails, state that the demonstrated step did not complete and stop. Do not
  claim a result that is not visible.

## After recording

1. Trim or re-record the technical walkthrough and confirm it is between 10 and 15 minutes.
2. Trim or re-record the non-technical demo and confirm it is between 5 and 10 minutes.
3. Review both videos for accidental secrets, tokens, contact data, notifications, or unrelated
   tabs before sharing.
4. Set the sharing permissions required by the assessment and test both links in a signed-out
   browser.
5. If Loom creates replacement share URLs, update this file, `README.md`, and
   `docs/GATE_REPORT.md`.
6. Re-run the Markdown link check and `git diff --check`, then commit the corrected recording
   evidence.
