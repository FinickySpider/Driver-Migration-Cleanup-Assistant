---
id: FEAT-035
type: feature
status: planned
priority: medium
phase: PHASE-04
sprint: SPRINT-07
owner: ""
depends_on: [FEAT-017]
---

# FEAT-035: Eval Harness CI Integration

## Description

Integrate the existing eval harness (from `Design-And-Data/evals/`) into the CI pipeline so all 10 scenarios (S1–S10) are validated on every build. Includes both offline and online eval modes.

## Acceptance Criteria

- [ ] Eval harness runs as part of CI pipeline
- [ ] All 10 scenarios (S1–S10) execute in offline mode
- [ ] Online mode (with OpenAI key) available as optional CI step
- [ ] Test results reported in CI summary
- [ ] Failures block merge/release
- [ ] Eval metrics tracked over time (pass rate, tool discipline, safety)

## Files Touched

| File | Change |
|------|--------|
| `.github/workflows/eval.yml` | CI workflow |
| `Design-And-Data/evals/` | Harness updates if needed |

## Testing

- [ ] CI pipeline runs successfully
- [ ] All S1–S10 pass in offline mode
- [ ] Failure detection works

## Done When

- [ ] Acceptance criteria met
- [ ] Verified in CI
