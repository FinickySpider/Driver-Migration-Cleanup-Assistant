---
id: FEAT-012
type: feature
status: complete
priority: high
phase: PHASE-02
sprint: SPRINT-03
owner: ""
depends_on: [FEAT-011, FEAT-010]
completed: 2026-02-12
---

# FEAT-012: Signal Evaluation and Baseline Scoring

## Description

Implement the signal evaluation logic that computes a baseline score (0–100) for each inventory item. Signals from `rules.yml` are evaluated against item properties, weights are summed, and the result is clamped to the valid range.

## Acceptance Criteria

- [x] Each signal's `when` conditions are evaluated against inventory item fields
- [x] `all` conditions require all to match; `any` conditions require at least one
- [x] Supported operators: `eq`, `contains_i`, `matches_keywords`, `missing_or_empty`
- [x] `requires_user_fact` conditions check against session user facts
- [x] Weights are summed to produce baseline score
- [x] Score is clamped to 0–100 post-computation
- [x] Recommendation band is assigned based on final score
- [x] Scores match expected values for eval fixtures S1–S5

## Files Touched

| File | Change |
|------|--------|
| `src/Dmca.Core/Scoring/SignalEvaluator.cs` | Condition matcher |
| `src/Dmca.Core/Scoring/BaselineScorer.cs` | Score computation |

## Testing

- [x] Unit tests with fixture data (S1 Intel MEI, S2 Microsoft inbox, etc.)
- [x] Clamping verified at boundaries
- [x] Keyword matching is case-insensitive

## Done When

- [x] Acceptance criteria met
- [x] Verified with eval fixtures

## Completion Notes

Implemented `SignalEvaluator` with 5 operators (`eq`, `contains_i`, `matches_keywords`, `missing_or_empty`) plus `all`/`any` condition groups. `BaselineScorer` computes clamped 0–100 scores and assigns recommendation bands. All eval fixture scores match expected values. All tests passing.
