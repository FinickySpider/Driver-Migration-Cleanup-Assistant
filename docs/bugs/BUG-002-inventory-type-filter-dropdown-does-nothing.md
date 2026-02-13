---
id: BUG-002
type: bug
status: in_progress  # planned | in_progress | complete | deprecated
severity: major  # critical | major | minor
phase: PHASE-04
sprint: SPRINT-09
owner: ""
---

# BUG-002: Inventory type filter dropdown does nothing

## Problem
The Inventory type filter ComboBox selection does not change filtering.

## Reproduction Steps
1. Complete Interview.
2. Go to Inventory and run a scan.
3. Choose Drivers/Services/Packages/Apps in the type dropdown.

## Expected
Inventory grid filters by the selected type.

## Actual
No visible change; selection does not affect filtering.

## Fix Strategy
- Correct ComboBox binding so the selected value updates `InventoryViewModel.FilterType`.

## Verification
- [ ] Not reproducible
- [ ] No regressions
