---
id: FEAT-035
type: feature
status: complete
priority: medium
phase: PHASE-04
sprint: SPRINT-07
owner: ""
depends_on: [FEAT-017]
completed: 2025-07-19
---

# FEAT-035: Eval Harness CI Integration

## Description

Integrate the existing eval harness (from `Design-And-Data/evals/`) into the CI pipeline so all 10 scenarios (S1–S10) are validated on every build. Includes both offline and online eval modes.

## Acceptance Criteria

- [x] Eval harness runs as part of CI pipeline
- [x] All 10 scenarios (S1–S10) execute in offline mode
- [x] Online mode (with OpenAI key) available as optional CI step
- [x] Test results reported in CI summary
- [x] Failures block merge/release
- [x] Eval metrics tracked over time (pass rate, tool discipline, safety)

## Files Touched

| File | Change |
|------|--------|
| `.github/workflows/ci.yml` | CI workflow with 3 jobs: build-and-test, eval-offline, eval-live |

## Testing

- [x] CI pipeline configured with build, offline eval, and live eval jobs
- [x] Offline eval runs S1–S10 in every PR
- [x] Live eval runs on main branch only (requires OPENAI_API_KEY secret)

## Done When

- [x] Acceptance criteria met
- [x] CI workflow created and validated
