# ADR-0004: Server-render public routes and hydrate authenticated behavior

## Status

Accepted

## Context

The approved frontend is React 19 with Vite SSR and hydration. Public Property list and detail
pages need meaningful HTML before JavaScript runs, while authenticated owner and Admin data must
not leak into HTML or serialized state.

Rendering every protected dashboard on the server would require forwarding sensitive credentials
through the Node process and would complicate the intentionally memory-only JWT lifecycle.

## Decision

Use one shared React route tree for server rendering and browser hydration. The Express/Vite server
loads public DTOs only for `/`, `/properties`, and `/properties/{id}`. It renders HTML and transfers
escaped public initial data. Non-public routes transfer `null`.

Hydrate the same route tree in the browser. Registration, login, owner, and Admin screens use
client-side behavior and dedicated API clients. Load protected data only after React has an
in-memory session. Keep SQL Server inaccessible to both React and the SSR process.

## Consequences

- Public routes return useful HTML and hydrate without private data.
- Direct public and private route requests are supported by the production web container.
- Protected dashboards do not expose tokens, contact numbers, roles, or metrics in SSR state.
- A strict JSON transfer boundary is required; absent route data is represented as `null`, not
  JavaScript `undefined`.
- This approach complements the memory-only token decision in
  [ADR-0002](0002-jwt-authorization-and-token-invalidation.md).
- Advanced SSR optimization and protected server rendering remain outside scope.
