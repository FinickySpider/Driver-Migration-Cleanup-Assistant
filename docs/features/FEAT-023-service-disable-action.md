---
id: FEAT-023
type: feature
status: planned
priority: high
phase: PHASE-03
sprint: SPRINT-05
owner: ""
depends_on: [FEAT-021]
---

# FEAT-023: Service Disable Action

## Description

Implement the action that disables a Windows service by setting its start type to Disabled (4) via SCM API or `sc.exe config <name> start= disabled`.

## Acceptance Criteria

- [ ] Action sets service start type to 4 (Disabled)
- [ ] Running services are stopped before disabling
- [ ] Service name correctly resolved from item ID
- [ ] Success/failure logged to audit log
- [ ] Dry-run mode outputs the command without executing
- [ ] Does not delete the service (disable only in v1)

## Files Touched

| File | Change |
|------|--------|
| `src/Dmca.Core/Execution/Actions/ServiceDisableAction.cs` | SCM wrapper |

## Testing

- [ ] Dry-run produces correct command
- [ ] Error handling for non-existent service

## Done When

- [ ] Acceptance criteria met
- [ ] Verified manually
