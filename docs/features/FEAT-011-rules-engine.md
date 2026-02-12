---
id: FEAT-011
type: feature
status: complete
priority: high
phase: PHASE-02
sprint: SPRINT-03
owner: ""
depends_on: [FEAT-010]
completed: 2026-02-12
---

# FEAT-011: Rules Engine and rules.yml Loader

## Description

Implement a rules engine that loads scoring configuration from `rules.yml`. This includes limits (score min/max, AI delta bounds), recommendation bands, hard-block definitions, keyword sets, and scoring signals.

## Acceptance Criteria

- [x] `rules.yml` is parsed and deserialized into strongly-typed configuration objects
- [x] Limits loaded: score_min=0, score_max=100, ai_delta_min=-25, ai_delta_max=25, ai_delta_max_with_user_fact=40
- [x] Recommendation bands loaded: REMOVE_STAGE_1 (80–100), REMOVE_STAGE_2 (55–79), REVIEW (30–54), KEEP (0–29)
- [x] Hard-block definitions loaded with conditions
- [x] Keyword sets loaded (intel_platform, asus_oem, amd_platform, gigabyte_oem)
- [x] Signal definitions loaded with weights and conditions
- [x] Configuration is validated on load (missing fields = startup error)
- [x] Configuration is read-only after load

## Files Touched

| File | Change |
|------|--------|
| `src/Dmca.Core/Scoring/RulesConfig.cs` | Configuration models |
| `src/Dmca.Core/Scoring/RulesLoader.cs` | YAML parser and validator |

## Testing

- [x] Loader parses the production `rules.yml` without errors
- [x] Invalid YAML produces clear error messages
- [x] All configuration values match expected defaults

## Done When

- [x] Acceptance criteria met
- [x] Verified manually

## Completion Notes

Implemented `RulesConfig` models and `RulesLoader` using YamlDotNet. All limits, bands, hard-block definitions, keyword sets, and signal definitions are loaded and validated. Configuration is immutable after load. All tests passing.
