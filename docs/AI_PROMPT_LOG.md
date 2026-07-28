# PropertyHub AI Prompt Log

This log records only prompts that materially influence implementation. It will contain at least 20
reviewed entries before Gate submission.

## Architecture

### 1. Repository audit and Gate implementation plan

- **Prompt:** Inspect PropertyHub against the BRD and Build Sprint Gate, then prepare an
  implementation plan.
- **Why it mattered:** Established the documentation-only baseline and exposed the missing
  canonical Gate document.
- **Review:** The initial plan was revised to require domain-specific repositories and a smaller
  scaffold phase.

### 2. Correct the architecture and establish the canonical Gate

- **Prompt:** Use controllers to services to repositories, require the canonical Gate file, separate
  image uploads from Property creation, and limit the first phase to foundation work.
- **Why it mattered:** Removed direct service-to-EF access and prevented a large unverified feature
  dump.
- **Review:** The final foundation plan uses separate projects and postpones feature abstractions
  until their vertical slices.

### 3. Separate moderation and availability state

- **Prompt:** Model moderation independently from Available, Sold, and Rented, and keep the scaffold
  practical.
- **Why it mattered:** Prevents moderation decisions from overwriting the owner-controlled listing
  lifecycle.
- **Review:** The initial model uses distinct `ModerationStatus` and `AvailabilityStatus` enums.

## Code generation

### 4. Implement the approved Phase 1 scaffold

- **Prompt:** Create and verify the layered backend, SSR frontend, tests, migration, Docker baseline,
  environment template, and documentation without starting later feature slices.
- **Why it mattered:** Authorizes the first repository implementation while preserving Gate-first
  sequencing.
- **Review:** Changes are being verified through restore, build, tests, migration scripting, Compose
  validation, and focused commits.

## Testing

Entries will be added as test-generation prompts materially affect later slices.

## Security

Entries will be added as authentication and upload-security prompts materially affect later slices.

## Docker and debugging

Entries will be added when container verification or diagnosed defects materially affect the build.

## Documentation

Entries will be added as final UAT, coverage, and walkthrough evidence is produced.
