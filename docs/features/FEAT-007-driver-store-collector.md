---
id: FEAT-007
type: feature
status: complete
priority: high
phase: PHASE-01
sprint: SPRINT-02
owner: ""
depends_on: [FEAT-002]
completed: 2025-07-15
---

# FEAT-007: Driver-Store Package Collector

## Description

Implement a collector that enumerates driver-store packages by parsing the output of `pnputil /enum-drivers`. Each package is mapped to an `InventoryItem` with type `DRIVER_PACKAGE`.

## Acceptance Criteria

- [x] Collector executes `pnputil /enum-drivers` and parses output
- [x] Each package mapped to `InventoryItem` with `itemType = DRIVER_PACKAGE`
- [x] Item ID generated as `pkg:<vendor>:<publishedName>`
- [x] Published name, INF name, provider, version captured
- [x] Collector handles pnputil not found or access denied gracefully

## Files Touched

| File | Change |
|------|--------|
| `src/Dmca.Core/Collectors/DriverStoreCollector.cs` | pnputil parser |

## Testing

- [x] Collector returns packages on a real system
- [x] Output parsing handles various Windows versions

## Done When

- [x] Acceptance criteria met
- [x] Verified manually
