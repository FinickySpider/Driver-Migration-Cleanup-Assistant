---
id: BUG-003
type: bug
status: in_progress  # planned | in_progress | complete | deprecated
severity: critical  # critical | major | minor
phase: PHASE-04
sprint: SPRINT-09
owner: ""
---

# BUG-003: Delta Report tab crashes with StaticResource error

## Problem
Navigating to the Delta Report tab triggers a crash dialog: "Provide value on 'System.Windows.StaticResourceExtension' threw an exception." This can repeat/cascade.

## Reproduction Steps
1. Launch app.
2. Navigate to Delta Report.

## Expected
Delta Report page loads (even if no report data exists yet).

## Actual
App throws a StaticResource exception and shows repeated error dialogs.

## Fix Strategy
- Ensure all referenced StaticResources exist (styles/converters).

## Verification
- [ ] Not reproducible
- [ ] No regressions
