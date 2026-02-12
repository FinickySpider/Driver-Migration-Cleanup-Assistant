---
id: FEAT-027
type: feature
status: planned
priority: medium
phase: PHASE-03
sprint: SPRINT-06
owner: ""
depends_on: [FEAT-026, FEAT-004]
---

# FEAT-027: Interview Wizard Screen

## Description

Implement the optional interview wizard that collects user facts before scanning. Guides users through old platform, new platform, symptoms, and aggressiveness preference.

## Acceptance Criteria

- [ ] Multi-step wizard with: old platform vendor, new platform vendor, symptoms (optional), aggressiveness (conservative/balanced/aggressive)
- [ ] Each step validates input before proceeding
- [ ] Wizard can be skipped (all facts optional)
- [ ] Collected facts stored as `UserFacts`
- [ ] Wizard triggers initial scan on completion

## Files Touched

| File | Change |
|------|--------|
| `src/Dmca.App/Views/InterviewWizard.xaml` | Wizard UI |
| `src/Dmca.App/ViewModels/InterviewWizardViewModel.cs` | Wizard logic |

## Testing

- [ ] Facts persist after wizard completion
- [ ] Skip produces empty facts (no errors)

## Done When

- [ ] Acceptance criteria met
- [ ] Verified manually
