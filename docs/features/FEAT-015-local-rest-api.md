---
id: FEAT-015
type: feature
status: planned
priority: high
phase: PHASE-02
sprint: SPRINT-04
owner: ""
depends_on: [FEAT-014]
---

# FEAT-015: Local REST API Server

## Description

Implement the local REST API server on `http://127.0.0.1:17831` serving all endpoints defined in the OpenAPI spec. The server uses ASP.NET Core minimal APIs and binds only to loopback.

## Acceptance Criteria

- [ ] Server starts on `http://127.0.0.1:17831` (loopback only)
- [ ] All GET endpoints return correct data: session, inventory/latest, inventory/item/{id}, plan/current, policy/hardblocks/{id}, proposals, proposals/{id}
- [ ] POST /v1/proposals creates a pending proposal
- [ ] POST /v1/proposals/{id}/approve is UI-only (not exposed to AI tools)
- [ ] POST /v1/actions/queue/execute is UI-only
- [ ] POST /v1/rescan is UI-only
- [ ] Proper HTTP status codes (200, 201, 404, 400)
- [ ] JSON responses match schemas

## Files Touched

| File | Change |
|------|--------|
| `src/Dmca.App/Api/` | API endpoint handlers |
| `src/Dmca.App/Program.cs` | Server startup configuration |

## Testing

- [ ] All endpoints return expected status codes
- [ ] JSON responses validate against schemas
- [ ] Server only accessible on loopback

## Done When

- [ ] Acceptance criteria met
- [ ] Verified with HTTP client
