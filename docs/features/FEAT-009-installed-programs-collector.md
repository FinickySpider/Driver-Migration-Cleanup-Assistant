---
id: FEAT-009
type: feature
status: complete
priority: high
phase: PHASE-01
sprint: SPRINT-02
owner: ""
depends_on: [FEAT-002]
completed: 2025-07-15
---

# FEAT-009: Installed Programs Collector

## Description

Implement a collector that enumerates installed programs from the Windows registry Uninstall keys (both x64 and WOW6432Node). Captures display name, publisher, version, quiet uninstall command, and install date.

## Acceptance Criteria

- [x] Collector reads `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall`
- [x] Collector reads `HKLM\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall`
- [x] Each program mapped to `InventoryItem` with `itemType = APP`
- [x] Item ID generated as `app:<vendor>:<name>`
- [x] Quiet uninstall string captured when available
- [x] Duplicates between x64 and WOW6432 are deduplicated

## Files Touched

| File | Change |
|------|--------|
| `src/Dmca.Core/Collectors/InstalledProgramsCollector.cs` | Registry reader |

## Testing

- [x] Collector returns programs on a real system
- [x] Both registry hives are read
- [x] Deduplication verified

## Done When

- [x] Acceptance criteria met
- [x] Verified manually
