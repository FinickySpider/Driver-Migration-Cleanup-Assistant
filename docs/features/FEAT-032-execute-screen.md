---
id: FEAT-032
type: feature
status: planned
priority: high
phase: PHASE-03
sprint: SPRINT-06
owner: ""
depends_on: [FEAT-026, FEAT-020]
---

# FEAT-032: Execute Screen with Dry-Run and Confirmation

## Description

Implement the execution screen showing the action queue, dry-run preview, and two-step confirmation for high-risk actions. Displays real-time progress during execution.

## Acceptance Criteria

- [ ] Action queue displayed as ordered list with action type, target, and mode
- [ ] Dry-run button previews all actions without executing
- [ ] Dry-run output shown in scrollable log view
- [ ] Execute button requires: (1) checkbox "I understand this will modify my system" (2) typed confirmation "EXECUTE"
- [ ] Real-time progress bar and per-action status during execution
- [ ] Execution can be cancelled between actions (not mid-action)
- [ ] Final summary shown on completion

## Files Touched

| File | Change |
|------|--------|
| `src/Dmca.App/Views/ExecuteScreen.xaml` | Execute UI |
| `src/Dmca.App/ViewModels/ExecuteScreenViewModel.cs` | Execute logic |

## Testing

- [ ] Dry-run shows preview without side effects
- [ ] Two-step confirmation enforced
- [ ] Progress updates in real-time

## Done When

- [ ] Acceptance criteria met
- [ ] Verified manually
