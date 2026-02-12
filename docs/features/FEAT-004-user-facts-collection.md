---
id: FEAT-004
type: feature
status: complete
priority: medium
phase: PHASE-01
sprint: SPRINT-01
owner: ""
depends_on: [FEAT-003]
completed: 2025-07-15
---

# FEAT-004: User Facts Collection

## Description

Implement the `UserFacts` model and persistence. User facts capture context about the migration (old platform vendor, new platform vendor, aggressiveness preference) and are used by the scoring engine and AI Advisor.

## Acceptance Criteria

- [x] `UserFacts` model matches `userFacts.json` schema (sessionId, facts array with key/value/source/createdAt)
- [x] Facts can be added to a session (append-only)
- [x] Facts are persisted and retrievable by session ID
- [x] Source field distinguishes `USER` vs `AI` origin
- [x] Console harness allows manual fact entry for testing

## Files Touched

| File | Change |
|------|--------|
| `src/Dmca.Core/Models/UserFacts.cs` | Domain model |
| `src/Dmca.Core/Models/UserFact.cs` | Individual fact model |
| `src/Dmca.Data/Repositories/UserFactsRepository.cs` | Persistence |

## Testing

- [x] Facts persist and retrieve correctly
- [x] Append-only behavior verified (no updates/deletes)

## Done When

- [x] Acceptance criteria met
- [x] Verified manually
