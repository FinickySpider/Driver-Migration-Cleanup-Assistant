---
id: FEAT-006
type: feature
status: complete
priority: high
phase: PHASE-01
sprint: SPRINT-02
owner: ""
depends_on: [FEAT-005]
completed: 2025-07-15
---

# FEAT-006: Device Presence Enumeration

## Description

Implement enumeration of present and non-present (hidden/ghost) devices via SetupAPI/PNP APIs. This extends the driver collector by determining which devices have active hardware and which are orphaned.

## Acceptance Criteria

- [x] Non-present (ghost) devices are detected and flagged with `present = false`
- [x] Present devices flagged with `present = true`
- [x] Device enumeration uses SetupAPI or equivalent .NET interop
- [x] Results merged with driver collector data to enrich `InventoryItem`
- [x] Hardware IDs from device nodes captured

## Files Touched

| File | Change |
|------|--------|
| `src/Dmca.Core/Collectors/DevicePresenceCollector.cs` | Device presence logic |
| `src/Dmca.Core/Interop/SetupApiInterop.cs` | P/Invoke wrappers if needed |

## Testing

- [x] Correctly identifies at least one non-present device on test system
- [x] Present devices are not falsely marked as non-present

## Done When

- [x] Acceptance criteria met
- [x] Verified manually
