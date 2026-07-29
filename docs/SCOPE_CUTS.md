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
| Background image cleanup | DEFERRED | The Gate requires secure local persistence but not timed retention processing; adding a worker before the remaining Gate slices would expand scope | None | Add the BRD's configurable 30-day cleanup after mandatory Gate verification |
| Cloud hosting/storage | DEFERRED | Project must run locally | None | Consider only outside this capstone |
| Rejected-listing correction workflow | DEFERRED | Owners can edit a rejected listing, which returns it to Pending; a separate correction state is not required by the Gate | None | Add review comments/history after mandatory workflows pass |
| Authenticated contact reveal | DEFERRED | Required by the broader BRD/review but not by the higher-priority canonical Gate checklist or the permanent Gate MVP; Phase 8 is limited to coverage, quality, UAT, and evidence | None for canonical Gate | Implement next with owner/self-contact privacy tests |
| Enquiries and owner inbox | DEFERRED | Required by the broader BRD/review but not by the higher-priority canonical Gate checklist; implementing it would introduce another domain workflow after the approved Phase 8 boundary | None for canonical Gate | Add stored enquiries before optional email delivery |

The review document's Frozen Gate calls contact reveal and enquiries mandatory, while the canonical
Gate checklist and `AGENTS.md` mandatory MVP do not. Per the approved priority order and explicit
phase boundary, they remain deferred after mandatory Gate stability rather than being silently
treated as implemented. Property moderation is implemented and is not a scope cut.
