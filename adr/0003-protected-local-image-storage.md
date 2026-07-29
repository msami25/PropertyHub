# ADR-0003: Store property images locally behind controlled API endpoints

## Status

Accepted

## Context

The Gate requires local file upload, persistence, retrieval, and frontend display. Property images
are untrusted user input. Serving an upload directory directly would bypass ownership, moderation,
availability, and disabled-owner checks. Trusting original filenames would allow collisions and
path traversal.

Cloud storage is outside the local-only deployment constraint.

## Decision

Create a Property first, then upload one to five images through owner-protected image endpoints.
Accept JPEG, PNG, and WebP files up to 5 MB each. Validate extension, declared content type, magic
bytes, successful ImageSharp decode, dimensions, pixel count, batch count, ownership, and listing
state.

Generate server-controlled storage names. Store bytes outside the web root through
`LocalImageStorage`; persist relative path, content type, dimensions, size, order, and primary state
in SQL Server. Resolve paths under a canonical root and reject rooted or escaping values.

Stream retrieval through the API after checking whether the caller is a public visitor, owner, or
Admin. Return `X-Content-Type-Options: nosniff`. Persist the upload root in the
`propertyhub-uploads` Docker volume.

## Consequences

- Images cannot bypass listing visibility or owner authorization.
- Runtime uploads stay outside Git and survive API recreation.
- Validation and generated names reduce executable upload, traversal, and collision risks.
- API streaming adds application work compared with static-file hosting.
- Property creation and image upload are separate requests, so a new Property can temporarily have
  no image and remains ineligible for approval.
- Timed cleanup of files belonging to deleted listings is deferred.
