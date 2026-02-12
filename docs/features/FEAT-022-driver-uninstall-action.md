---
id: FEAT-022
type: feature
status: complete
priority: high
phase: PHASE-03
sprint: SPRINT-05
owner: ""
depends_on: [FEAT-021]
---

# FEAT-022: Driver Package Uninstall Action

## Description

Implement the action that uninstalls a driver-store package via `pnputil /delete-driver <oem*.inf> /uninstall /force`. Handles success, failure, and in-use scenarios.

## Acceptance Criteria

- [ ] Action executes `pnputil /delete-driver` with correct INF name
- [ ] Exit code determines success/failure
- [ ] In-use drivers flagged but not forcefully removed without confirmation
- [ ] Output captured and logged to audit log
- [ ] Dry-run mode outputs the command without executing

## Files Touched

| File | Change |
|------|--------|
| `src/Dmca.Core/Execution/Actions/DriverUninstallAction.cs` | pnputil wrapper |

## Testing

- [ ] Dry-run produces correct command string
- [ ] Error handling for missing package

## Done When

- [ ] Acceptance criteria met
- [ ] Verified manually
