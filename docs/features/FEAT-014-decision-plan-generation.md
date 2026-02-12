---
id: FEAT-014
type: feature
status: complete
priority: high
phase: PHASE-02
sprint: SPRINT-03
owner: ""
depends_on: [FEAT-012, FEAT-013]
completed: 2026-02-12
---

# FEAT-014: Decision Plan Generation and Persistence

## Description

Implement the `DecisionPlan` generator that combines baseline scores and hard blocks into a complete plan with `PlanItem`s. The plan is persisted to SQLite and the session transitions to `PLANNED`.

## Acceptance Criteria

- [x] `DecisionPlan` model matches `plan.json` schema
- [x] `PlanItem` includes: itemId, baselineScore, aiScoreDelta (initially 0), finalScore, recommendation, hardBlocks, engineRationale
- [x] Final score = clamp(baselineScore + aiScoreDelta, 0, 100)
- [x] Recommendation assigned from band or BLOCKED if any hard block
- [x] Engine rationale includes human-readable reasons for score components
- [x] Plan is persisted to SQLite
- [x] Session status transitions to `PLANNED`
- [x] `GET /v1/plan/current` returns the plan (stubbed endpoint)

## Files Touched

| File | Change |
|------|--------|
| `src/Dmca.Core/Models/DecisionPlan.cs` | Domain model |
| `src/Dmca.Core/Models/PlanItem.cs` | Domain model |
| `src/Dmca.Core/Services/PlanService.cs` | Plan generator |
| `src/Dmca.Data/Repositories/PlanRepository.cs` | Persistence |

## Testing

- [x] Generated plan matches expected scores for eval fixtures
- [x] Hard-blocked items have recommendation = BLOCKED
- [x] Plan round-trips through SQLite

## Done When

- [x] Acceptance criteria met
- [x] Verified with eval fixtures

## Completion Notes

Implemented `PlanService` to assemble `DecisionPlan` with scored `PlanItem`s combining baseline scores, hard blocks, and recommendation bands. `PlanRepository` persists plans to SQLite with full round-trip fidelity. Session transitions to `PLANNED` after plan generation. All tests passing.
