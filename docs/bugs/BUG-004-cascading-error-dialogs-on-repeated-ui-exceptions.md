---
id: BUG-004
type: bug
status: in_progress  # planned | in_progress | complete | deprecated
severity: major  # critical | major | minor
phase: PHASE-04
sprint: SPRINT-09
owner: ""
---

# BUG-004: Cascading error dialogs on repeated UI exceptions

## Problem
When an exception triggers repeatedly (e.g., during XAML parsing/layout), the global exception handler shows repeated MessageBoxes, resulting in a cascade of dialogs.

## Reproduction Steps
1. Trigger a repeated UI exception (e.g., missing XAML StaticResource).
2. Observe multiple dialogs appearing.

## Expected
Only one error dialog is shown for a given crash loop, and a diagnostic report is captured.

## Actual
Multiple dialogs are shown, making the app hard to recover.

## Fix Strategy
- Add a reentrancy guard in the dispatcher exception handler.
- Persist full exception details to a crash log file.

## Verification
- [ ] Not reproducible
- [ ] No regressions
