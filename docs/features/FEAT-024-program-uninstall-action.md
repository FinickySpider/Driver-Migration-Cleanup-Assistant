---
id: FEAT-024
type: feature
status: planned
priority: high
phase: PHASE-03
sprint: SPRINT-05
owner: ""
depends_on: [FEAT-021]
---

# FEAT-024: Program Uninstall Action

## Description

Implement the action that uninstalls a program using the quiet uninstall string from the registry, falling back to the standard uninstall string if quiet is unavailable.

## Acceptance Criteria

- [ ] Action uses `QuietUninstallString` when available
- [ ] Falls back to `UninstallString` with `/S` or `/silent` flags
- [ ] Process exit code determines success/failure
- [ ] Timeout (configurable, default 120s) prevents hanging uninstallers
- [ ] Output captured and logged to audit log
- [ ] Dry-run mode outputs the command without executing

## Files Touched

| File | Change |
|------|--------|
| `src/Dmca.Core/Execution/Actions/ProgramUninstallAction.cs` | Uninstaller wrapper |

## Testing

- [ ] Dry-run produces correct command
- [ ] Timeout handling verified

## Done When

- [ ] Acceptance criteria met
- [ ] Verified manually
