# ADR-0002: Use JWT authorization with database-backed token invalidation

## Status

Accepted

## Context

The Gate requires registration, JWT login, User/Admin authorization, verified 401/403 behavior, and
immediate enforcement when an account is disabled. A signed token alone remains valid until expiry,
so disabling a user or changing a role would otherwise leave an existing token usable.

Storing tokens in browser storage would survive reloads but would increase exposure to
cross-site-scripting attacks. Refresh tokens are outside the approved Gate.

## Decision

Use ASP.NET Core Identity for password hashing, users, roles, lockout, and normalized email.
Successful login issues a 30-minute JWT containing the user identifier, role, and token version.
JWT validation enforces issuer, audience, signing key, expiry, and zero clock skew.

Store the access token only in React memory. Apply an `ActiveUser` authorization requirement to
protected operations. Its handler checks the user ID and token version through
`IUserAccountRepository`. Role or account-status changes increment the stored version, so an older
token immediately fails protected authorization.

Use an Admin-only policy for Admin APIs. Prevent self-demotion, self-disable, Admin disable through
user management, and demotion of the last active Admin.

## Consequences

- Disabled users and accounts with changed roles lose protected access immediately.
- Public registration cannot assign privileged roles.
- Tokens do not appear in local storage, session storage, URLs, or SSR HTML.
- A browser reload clears the session and requires login again.
- Every protected request performs a small database-backed active/version check.
- Refresh-token convenience is deferred.
- The repository dependency follows [ADR-0001](0001-layered-backend-architecture.md) and keeps EF
  Core out of the authorization handler.
