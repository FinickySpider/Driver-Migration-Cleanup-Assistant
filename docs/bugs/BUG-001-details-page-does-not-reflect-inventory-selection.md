---
id: BUG-001
type: bug
status: in_progress  # planned | in_progress | complete | deprecated
severity: major  # critical | major | minor
phase: PHASE-04
sprint: SPRINT-09
owner: ""
---

# BUG-001: Details page does not reflect Inventory selection

## Problem
The Details tab never shows item details even when an inventory item is selected.

## Reproduction Steps
1. Complete Interview.
2. Go to Inventory and run a scan.
3. Click any row in the inventory grid.
4. Navigate to Details.

## Expected
Details view shows the selected inventory item fields.

## Actual
Details view always displays the "Select an item" message.

## Fix Strategy
- Wire Inventory selection changes to update `ItemDetailViewModel.Item`.

## Verification
- [ ] Not reproducible
- [ ] No regressions
