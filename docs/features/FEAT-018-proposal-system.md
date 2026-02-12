---
id: FEAT-018
type: feature
status: planned
priority: high
phase: PHASE-02
sprint: SPRINT-04
owner: ""
depends_on: [FEAT-015]
---

# FEAT-018: Proposal CRUD and Lifecycle

## Description

Implement the full proposal system: create, list, get, and status transitions (PENDING → APPROVED | REJECTED). Proposals contain typed changes (score_delta, recommendation, pin_protect, action_add, action_remove, note_add, fact_request).

## Acceptance Criteria

- [ ] `Proposal` model matches `proposal.json` schema
- [ ] `ProposalChange` supports all 7 change types
- [ ] Proposals created as PENDING
- [ ] Proposals can be approved or rejected (UI-only endpoints)
- [ ] Proposal list and get endpoints return correct data
- [ ] Evidence array stored and returned with proposals
- [ ] Risk summary (LOW/MEDIUM/HIGH) computed per proposal
- [ ] Proposals persisted to SQLite

## Files Touched

| File | Change |
|------|--------|
| `src/Dmca.Core/Models/Proposal.cs` | Domain model |
| `src/Dmca.Core/Models/ProposalChange.cs` | Change model |
| `src/Dmca.Core/Services/ProposalService.cs` | CRUD logic |
| `src/Dmca.Data/Repositories/ProposalRepository.cs` | Persistence |

## Testing

- [ ] Create, list, get proposals work correctly
- [ ] Status transitions enforce lifecycle
- [ ] All 7 change types accepted

## Done When

- [ ] Acceptance criteria met
- [ ] Verified with eval fixtures
