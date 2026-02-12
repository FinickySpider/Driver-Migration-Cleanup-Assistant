---
id: FEAT-011
type: feature
status: planned
priority: high
phase: PHASE-02
sprint: SPRINT-03
owner: ""
depends_on: [FEAT-010]
---

# FEAT-011: Rules Engine and rules.yml Loader

## Description

Implement a rules engine that loads scoring configuration from `rules.yml`. This includes limits (score min/max, AI delta bounds), recommendation bands, hard-block definitions, keyword sets, and scoring signals.

## Acceptance Criteria

- [ ] `rules.yml` is parsed and deserialized into strongly-typed configuration objects
- [ ] Limits loaded: score_min=0, score_max=100, ai_delta_min=-25, ai_delta_max=25, ai_delta_max_with_user_fact=40
- [ ] Recommendation bands loaded: REMOVE_STAGE_1 (80–100), REMOVE_STAGE_2 (55–79), REVIEW (30–54), KEEP (0–29)
- [ ] Hard-block definitions loaded with conditions
- [ ] Keyword sets loaded (intel_platform, asus_oem, amd_platform, gigabyte_oem)
- [ ] Signal definitions loaded with weights and conditions
- [ ] Configuration is validated on load (missing fields = startup error)
- [ ] Configuration is read-only after load

## Files Touched

| File | Change |
|------|--------|
| `src/Dmca.Core/Scoring/RulesConfig.cs` | Configuration models |
| `src/Dmca.Core/Scoring/RulesLoader.cs` | YAML parser and validator |

## Testing

- [ ] Loader parses the production `rules.yml` without errors
- [ ] Invalid YAML produces clear error messages
- [ ] All configuration values match expected defaults

## Done When

- [ ] Acceptance criteria met
- [ ] Verified manually
