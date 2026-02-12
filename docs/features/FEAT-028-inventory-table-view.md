---
id: FEAT-028
type: feature
status: planned
priority: high
phase: PHASE-03
sprint: SPRINT-06
owner: ""
depends_on: [FEAT-026, FEAT-014]
---

# FEAT-028: Inventory Table View

## Description

Implement the main inventory table showing all scanned items with baseline score, AI delta, final score, recommendation, and hard-block badges. Supports sorting, filtering, and row selection.

## Acceptance Criteria

- [ ] Table columns: Item Name, Type, Vendor, Score (baseline/AI delta/final), Recommendation, Hard Blocks
- [ ] Hard-block items show badge/icon
- [ ] Sort by any column
- [ ] Filter by: type (DRIVER, SERVICE, DRIVER_PACKAGE, APP), recommendation, vendor
- [ ] Row selection opens detail pane
- [ ] Score cells color-coded by recommendation band
- [ ] Responsive to window resizing

## Files Touched

| File | Change |
|------|--------|
| `src/Dmca.App/Views/InventoryTableView.xaml` | Table layout |
| `src/Dmca.App/ViewModels/InventoryTableViewModel.cs` | Table logic |

## Testing

- [ ] Table displays all items from snapshot
- [ ] Sort and filter work correctly
- [ ] Color coding matches recommendation bands

## Done When

- [ ] Acceptance criteria met
- [ ] Verified manually
