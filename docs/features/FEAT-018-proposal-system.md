---
id: FEAT-018
type: feature
status: complete
priority: high
phase: PHASE-02
sprint: SPRINT-04
owner: ""
depends_on: [FEAT-015]
completed: 2026-02-12
---

# FEAT-018: Proposal CRUD and Lifecycle

## Description

Implement the full proposal system: create, list, get, and status transitions (PENDING → APPROVED | REJECTED). Proposals contain typed changes (score_delta, recommendation, pin_protect, action_add, action_remove, note_add, fact_request).

## Acceptance Criteria

- [x] `Proposal` model matches `proposal.json` schema
- [x] `ProposalChange` supports all 7 change types
- [x] Proposals created as PENDING
- [x] Proposals can be approved or rejected (UI-only endpoints)
- [x] Proposal list and get endpoints return correct data
- [x] Evidence array stored and returned with proposals
- [x] Risk summary (LOW/MEDIUM/HIGH) computed per proposal
- [x] Proposals persisted to SQLite

## Files Touched

| File | Change |
|------|--------|
| `src/Dmca.Core/Models/Proposal.cs` | Domain model |
| `src/Dmca.Core/Models/ProposalChange.cs` | Change model |
| `src/Dmca.Core/Services/ProposalService.cs` | CRUD logic |
| `src/Dmca.Data/Repositories/ProposalRepository.cs` | Persistence |

## Testing

- [x] Create, list, get proposals work correctly
- [x] Status transitions enforce lifecycle
- [x] All 7 change types accepted

## Done When

- [x] Acceptance criteria met
- [x] Verified with eval fixtures

## Completion Notes

Implemented `ProposalService` with full CRUD operations and `ProposalRepository` for SQLite persistence. Full lifecycle enforced: proposals created as PENDING, transitioned to APPROVED or REJECTED via UI-only endpoints. All 7 change types supported (score_delta, recommendation, pin_protect, action_add, action_remove, note_add, fact_request). Evidence array and risk summary (LOW/MEDIUM/HIGH) stored and returned. All tests passing.
