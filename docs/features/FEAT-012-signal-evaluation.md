---
id: FEAT-012
type: feature
status: planned
priority: high
phase: PHASE-02
sprint: SPRINT-03
owner: ""
depends_on: [FEAT-011, FEAT-010]
---

# FEAT-012: Signal Evaluation and Baseline Scoring

## Description

Implement the signal evaluation logic that computes a baseline score (0–100) for each inventory item. Signals from `rules.yml` are evaluated against item properties, weights are summed, and the result is clamped to the valid range.

## Acceptance Criteria

- [ ] Each signal's `when` conditions are evaluated against inventory item fields
- [ ] `all` conditions require all to match; `any` conditions require at least one
- [ ] Supported operators: `eq`, `contains_i`, `matches_keywords`, `missing_or_empty`
- [ ] `requires_user_fact` conditions check against session user facts
- [ ] Weights are summed to produce baseline score
- [ ] Score is clamped to 0–100 post-computation
- [ ] Recommendation band is assigned based on final score
- [ ] Scores match expected values for eval fixtures S1–S5

## Files Touched

| File | Change |
|------|--------|
| `src/Dmca.Core/Scoring/SignalEvaluator.cs` | Condition matcher |
| `src/Dmca.Core/Scoring/BaselineScorer.cs` | Score computation |

## Testing

- [ ] Unit tests with fixture data (S1 Intel MEI, S2 Microsoft inbox, etc.)
- [ ] Clamping verified at boundaries
- [ ] Keyword matching is case-insensitive

## Done When

- [ ] Acceptance criteria met
- [ ] Verified with eval fixtures
