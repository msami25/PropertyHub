# Coverage Evidence

Final evidence generated on 2026-07-29:

- `coverage.cobertura.xml`: merged machine-readable Coverlet evidence;
- `index.html`: readable single-file HTML summary;
- `Summary.txt`: readable text summary.

The merged result is 91.2% backend line coverage: 2,382 covered of 2,610 coverable lines. It
includes PropertyHub.Api, PropertyHub.Application, PropertyHub.Domain, and
PropertyHub.Infrastructure. Only generated EF Core migration files are excluded through
`tests/coverage.runsettings`.

The exact restore, collection, and merge commands are documented in the root `README.md`.
