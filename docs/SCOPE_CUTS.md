# PropertyHub Scope Cuts

Gate requirements are never treated as acceptable scope cuts. This file records only deliberate
deferrals that protect mandatory delivery.

| Feature | Decision | Reason | Gate impact | Future recommendation |
|---|---|---|---|---|
| Favourites | DEFERRED | Not required by the canonical Gate | None | Add after all mandatory flows pass |
| Advanced filters | DEFERRED | Essential filters are City, purpose, and property type | None | Add price/area/room filters later |
| MailHog and outbox | DEFERRED | Open-Meteo is the required real integration | None | Add after enquiry flow is stable |
| Refresh tokens | DEFERRED | Short-lived in-memory JWT satisfies the Gate | None | Reassess for a production release |
| ETags and idempotency keys | DEFERRED | Additional concurrency complexity | None | Add with broader concurrency needs |
| Complex immutable audit infrastructure | DEFERRED | Not needed for initial Gate evidence | None | Add focused audit records later |
| Background image cleanup | DEFERRED | Secure storage and access control come first | None | Add retention processing later |
| Cloud hosting/storage | DEFERRED | Project must run locally | None | Consider only outside this capstone |

Moderation, contact reveal, and enquiries are sequenced after generic Gate stability; they are not
declared permanent scope cuts.
