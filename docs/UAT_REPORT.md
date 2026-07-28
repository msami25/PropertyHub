# PropertyHub UAT Report

Phase 1 establishes the UAT script structure. Feature scenarios remain `BLOCKED` until their
implementation exists and the complete Docker environment is running.

| ID | Scenario | Expected result | Actual result | Evidence | Status |
|---|---|---|---|---|---|
| UAT-01 | Register and log in | Active user receives a JWT and can enter protected routes | Not implemented | Pending | BLOCKED |
| UAT-02 | Enforce authorization | Missing token returns 401 and non-admin Admin access returns 403 | Not implemented | Pending | BLOCKED |
| UAT-03 | Complete City CRUD | Admin lists, creates, edits, and deletes an unused City | Not implemented | Pending | BLOCKED |
| UAT-04 | Complete Property CRUD | Owner creates, views, edits, marks availability, and deletes a listing | Not implemented | Pending | BLOCKED |
| UAT-05 | Upload property images | Valid images persist and display; unsafe input is rejected | Not implemented | Pending | BLOCKED |
| UAT-06 | Display Open-Meteo weather | Details show weather or a graceful unavailable state | Not implemented | Pending | BLOCKED |
| UAT-07 | Manage users and metrics | Admin changes roles/status and sees live database counts | Not implemented | Pending | BLOCKED |
| UAT-08 | Restart Docker services | SQL data and uploaded images survive a normal restart | Not implemented | Pending | BLOCKED |

Each scenario will gain numbered steps, expected and actual results, evidence references, and a
truthful `PASS` or `FAIL` only when manually executed.
