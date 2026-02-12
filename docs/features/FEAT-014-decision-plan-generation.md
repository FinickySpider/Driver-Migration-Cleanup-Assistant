---
id: FEAT-014
type: feature
status: planned
priority: high
phase: PHASE-02
sprint: SPRINT-03
owner: ""
depends_on: [FEAT-012, FEAT-013]
---

# FEAT-014: Decision Plan Generation and Persistence

## Description

Implement the `DecisionPlan` generator that combines baseline scores and hard blocks into a complete plan with `PlanItem`s. The plan is persisted to SQLite and the session transitions to `PLANNED`.

## Acceptance Criteria

- [ ] `DecisionPlan` model matches `plan.json` schema
- [ ] `PlanItem` includes: itemId, baselineScore, aiScoreDelta (initially 0), finalScore, recommendation, hardBlocks, engineRationale
- [ ] Final score = clamp(baselineScore + aiScoreDelta, 0, 100)
- [ ] Recommendation assigned from band or BLOCKED if any hard block
- [ ] Engine rationale includes human-readable reasons for score components
- [ ] Plan is persisted to SQLite
- [ ] Session status transitions to `PLANNED`
- [ ] `GET /v1/plan/current` returns the plan (stubbed endpoint)

## Files Touched

| File | Change |
|------|--------|
| `src/Dmca.Core/Models/DecisionPlan.cs` | Domain model |
| `src/Dmca.Core/Models/PlanItem.cs` | Domain model |
| `src/Dmca.Core/Services/PlanService.cs` | Plan generator |
| `src/Dmca.Data/Repositories/PlanRepository.cs` | Persistence |

## Testing

- [ ] Generated plan matches expected scores for eval fixtures
- [ ] Hard-blocked items have recommendation = BLOCKED
- [ ] Plan round-trips through SQLite

## Done When

- [ ] Acceptance criteria met
- [ ] Verified with eval fixtures
