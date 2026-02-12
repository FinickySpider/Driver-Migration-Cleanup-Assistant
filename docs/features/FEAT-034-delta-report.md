---
id: FEAT-034
type: feature
status: planned
priority: high
phase: PHASE-04
sprint: SPRINT-07
owner: ""
depends_on: [FEAT-033]
---

# FEAT-034: Delta Report Generation

## Description

Implement delta report generation that compares two snapshots and produces a summary of changes: items removed, items changed (e.g., service disabled), items remaining, and next-steps guidance.

## Acceptance Criteria

- [ ] Delta computed between two `InventorySnapshot`s (pre vs post execution)
- [ ] Report categorizes items as: removed, changed, remaining (unchanged)
- [ ] Changed items show what changed (e.g., "start type: Auto → Disabled")
- [ ] Summary statistics: count of removed/changed/remaining per type
- [ ] Next-steps guidance generated (e.g., "Recommend rebooting to complete driver removal")
- [ ] Report displayed in UI
- [ ] Report exportable as text/markdown

## Files Touched

| File | Change |
|------|--------|
| `src/Dmca.Core/Reports/DeltaReportGenerator.cs` | Comparison logic |
| `src/Dmca.Core/Models/DeltaReport.cs` | Report model |
| `src/Dmca.App/Views/DeltaReportView.xaml` | Report UI |

## Testing

- [ ] Removed items correctly identified
- [ ] Changed items show correct diffs
- [ ] Remaining items unchanged

## Done When

- [ ] Acceptance criteria met
- [ ] Verified manually
