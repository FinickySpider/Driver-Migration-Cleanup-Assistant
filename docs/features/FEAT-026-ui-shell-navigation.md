---
id: FEAT-026
type: feature
status: complete
priority: high
phase: PHASE-03
sprint: SPRINT-06
owner: ""
depends_on: [FEAT-015]
---

# FEAT-026: UI Shell and Navigation

## Description

Implement the main desktop application shell with navigation between all major screens. Uses the chosen UI framework (WPF or WinUI 3 per ADR-0001) with MVVM pattern.

## Acceptance Criteria

- [ ] Application launches with a main window shell
- [ ] Navigation sidebar or tabs for: Home, Inventory, AI Advisor, Proposals, Execute
- [ ] Navigation state preserved (no data loss on screen switch)
- [ ] Window title shows app name and session ID
- [ ] Minimum window size enforced
- [ ] MVVM architecture with ViewModels for each screen

## Files Touched

| File | Change |
|------|--------|
| `src/Dmca.App/Views/MainWindow.xaml` | Shell layout |
| `src/Dmca.App/ViewModels/MainViewModel.cs` | Shell ViewModel |
| `src/Dmca.App/Navigation/` | Navigation service |

## Testing

- [ ] All screens reachable via navigation
- [ ] No crashes on rapid screen switching

## Done When

- [ ] Acceptance criteria met
- [ ] Verified manually
