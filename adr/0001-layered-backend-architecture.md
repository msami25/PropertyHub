# ADR-0001: Use a layered backend with domain-specific repositories

## Status

Accepted

## Context

PropertyHub needs HTTP endpoints, business rules, authentication, persistence, external services,
and file storage without coupling every concern to ASP.NET Core or Entity Framework Core. The
permanent project guide requires Controllers → Services → Repositories → EF Core. Property and City
workflows also contain ownership, moderation, validation, and public-visibility rules that must be
testable without an HTTP server or SQL Server.

A generic repository would hide useful EF Core capabilities while adding abstractions with no
domain meaning. Direct controller-to-DbContext access would make authorization and business rules
easy to duplicate or bypass.

## Decision

Use four backend projects:

- `PropertyHub.Api` for controllers, authentication configuration, middleware, and health.
- `PropertyHub.Application` for request/response contracts, use-case services, validation, and
  repository interfaces.
- `PropertyHub.Domain` for entities, enums, roles, and domain exceptions.
- `PropertyHub.Infrastructure` for EF Core, Identity, domain-specific repository implementations,
  local image storage, and Open-Meteo.

Controllers call services only. Services use focused repository interfaces such as
`IPropertyRepository`, `ICityRepository`, and `IAdminUserRepository`. EF Core stays in
Infrastructure, and `ApplicationDbContext` remains the unit of work.

## Consequences

- Business rules can be unit-tested with Moq and without HTTP or SQL Server.
- API integration tests can exercise controllers and authorization through the real service graph.
- Persistence-specific projections and public-visibility queries remain centralized.
- More files and mappings are required than in a single-project application.
- Repository interfaces must stay use-case-driven to avoid ceremonial abstractions.
- This decision supports [ADR-0002](0002-jwt-authorization-and-token-invalidation.md), where the
  ActiveUser policy depends on an account repository rather than querying EF Core from the API.
