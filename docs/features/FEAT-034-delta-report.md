---
id: FEAT-034
type: feature
status: complete
priority: high
phase: PHASE-04
sprint: SPRINT-07
owner: ""
depends_on: [FEAT-033]
completed: 2025-07-19
---

# FEAT-034: Delta Report Generation

## Description

Implement delta report generation that compares two snapshots and produces a summary of changes: items removed, items changed (e.g., service disabled), items remaining, and next-steps guidance.

## Acceptance Criteria

- [x] Delta computed between two `InventorySnapshot`s (pre vs post execution)
- [x] Report categorizes items as: removed, changed, remaining (unchanged), added
- [x] Changed items show what changed (e.g., "start type: Auto → Disabled")
- [x] Summary statistics: count of removed/changed/remaining/added per type
- [x] Next-steps guidance generated (e.g., "Recommend rebooting to complete driver removal")
- [x] Report displayed in UI
- [x] Report exportable as text/markdown

## Files Touched

| File | Change |
|------|--------|
| `src/Dmca.Core/Reports/DeltaReportGenerator.cs` | Comparison logic |
| `src/Dmca.Core/Models/DeltaReport.cs` | Report model (DeltaStatus, PropertyChange, DeltaReportItem, etc.) |
| `src/Dmca.App/Views/DeltaReportView.xaml` | Report UI (summary cards, DataGrid, next steps) |
| `src/Dmca.App/Views/DeltaReportView.xaml.cs` | Code-behind |
| `src/Dmca.App/ViewModels/DeltaReportViewModel.cs` | ViewModel with commands |
| `src/Dmca.App/Views/MainWindow.xaml` | DataTemplate for DeltaReportViewModel |
| `src/Dmca.App/Views/MainWindow.xaml.cs` | Navigation item |

## Testing

- [x] Removed items correctly identified
- [x] Changed items show correct diffs (version, vendor, start type, running, etc.)
- [x] Remaining items unchanged
- [x] Added items detected
- [x] Mixed scenario summary correct
- [x] Markdown export contains all sections
- [x] Next-steps guidance covers reboot, service, review, and system check

## Done When

- [x] Acceptance criteria met
- [x] Verified with 20 unit tests (DeltaReportGeneratorTests)
