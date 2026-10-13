# Operations Guide

## Safe File Replacement Procedure
1. Prepare new `.pmtiles` file in staging location.
2. Validate file integrity and expected size.
3. If needed, rename the incoming artifact to your stable operational filename (for example `main.pmtiles`).
4. Stop service or route traffic away (recommended for atomic swap).
5. Replace existing file at configured `Protomaps__FilePath`.
6. Ensure file permissions remain read-only for service account.
7. Restart service (if required by hosting model).
8. Run smoke checks:
   - `GET /healthz`
   - full file request
   - range request

## Permission Checklist
- Service identity has read permission to:
  - target `.pmtiles` file
  - parent directory
- Service identity has no write permission unless explicitly required by ops model.
- Service identity has no access to unrelated sensitive directories.

## Rollback Procedure
1. Restore previous known-good `.pmtiles` file backup.
2. Re-run smoke checks.
3. Confirm frontend map rendering recovery.
4. Record rollback incident and corrective actions.

## Monitoring Signals
- HTTP status distribution for `.pmtiles` endpoint (`200`, `206`, `4xx`, `5xx`)
- Log events for file access failures and configuration errors
- Request latency and response size trends for range requests
