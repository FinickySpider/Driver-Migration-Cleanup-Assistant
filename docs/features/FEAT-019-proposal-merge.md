---
id: FEAT-019
type: feature
status: planned
priority: high
phase: PHASE-02
sprint: SPRINT-04
owner: ""
depends_on: [FEAT-018, FEAT-014]
---

# FEAT-019: Proposal Merge into Decision Plan

## Description

Implement the logic that merges an approved proposal's changes into the `DecisionPlan`. Score deltas are applied (with clamping), recommendations are updated, and the plan is re-persisted.

## Acceptance Criteria

- [ ] Approved proposal changes are applied to the plan
- [ ] `score_delta` changes update `aiScoreDelta` and recompute `finalScore`
- [ ] AI delta clamped to ±25 (±40 with confirmed user fact)
- [ ] `recommendation` changes update the plan item
- [ ] `pin_protect` adds a `POLICY_PROTECTED` hard block
- [ ] `note_add` appends to plan item notes
- [ ] Hard-blocked items cannot have their recommendation changed
- [ ] Plan is re-persisted after merge
- [ ] Session status transitions to `PENDING_APPROVALS` when proposals exist

## Files Touched

| File | Change |
|------|--------|
| `src/Dmca.Core/Services/PlanMergeService.cs` | Merge logic |

## Testing

- [ ] Score delta applied and clamped correctly
- [ ] Hard-blocked items remain BLOCKED after merge
- [ ] Multiple proposals merge sequentially

## Done When

- [ ] Acceptance criteria met
- [ ] Verified with eval fixtures
