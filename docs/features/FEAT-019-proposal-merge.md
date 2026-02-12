---
id: FEAT-019
type: feature
status: complete
priority: high
phase: PHASE-02
sprint: SPRINT-04
owner: ""
depends_on: [FEAT-018, FEAT-014]
completed: 2026-02-12
---

# FEAT-019: Proposal Merge into Decision Plan

## Description

Implement the logic that merges an approved proposal's changes into the `DecisionPlan`. Score deltas are applied (with clamping), recommendations are updated, and the plan is re-persisted.

## Acceptance Criteria

- [x] Approved proposal changes are applied to the plan
- [x] `score_delta` changes update `aiScoreDelta` and recompute `finalScore`
- [x] AI delta clamped to ±25 (±40 with confirmed user fact)
- [x] `recommendation` changes update the plan item
- [x] `pin_protect` adds a `POLICY_PROTECTED` hard block
- [x] `note_add` appends to plan item notes
- [x] Hard-blocked items cannot have their recommendation changed
- [x] Plan is re-persisted after merge
- [x] Session status transitions to `PENDING_APPROVALS` when proposals exist

## Files Touched

| File | Change |
|------|--------|
| `src/Dmca.Core/Services/PlanMergeService.cs` | Merge logic |

## Testing

- [x] Score delta applied and clamped correctly
- [x] Hard-blocked items remain BLOCKED after merge
- [x] Multiple proposals merge sequentially

## Done When

- [x] Acceptance criteria met
- [x] Verified with eval fixtures

## Completion Notes

Implemented `PlanMergeService` that applies approved proposal changes to the `DecisionPlan`. Delta clamping enforced (±25 default, ±40 with user-fact). Hard-block protection prevents recommendation changes on blocked items. Plan re-persisted after merge. Multiple proposals merge sequentially. All tests passing.
