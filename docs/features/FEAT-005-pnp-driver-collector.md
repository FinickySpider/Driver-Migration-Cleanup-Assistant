---
id: FEAT-005
type: feature
status: complete
priority: high
phase: PHASE-01
sprint: SPRINT-02
owner: ""
depends_on: [FEAT-002]
completed: 2025-07-15
---

# FEAT-005: PnP Signed Driver Collector

## Description

Implement a collector that enumerates PnP signed drivers via WMI/CIM (`Win32_PnPSignedDriver`). Each driver is mapped to an `InventoryItem` with type `DRIVER`, including display name, vendor, version, signer info, device hardware IDs, and presence state.

## Acceptance Criteria

- [x] Collector queries `Win32_PnPSignedDriver` via CIM/WMI
- [x] Each result mapped to `InventoryItem` with `itemType = DRIVER`
- [x] Item ID generated as `drv:<vendor>:<name>` (sanitized, unique)
- [x] Signature info populated (signed, signer, isMicrosoft, isWHQL)
- [x] Hardware IDs captured where available
- [x] Present/not-present state determined
- [x] Collector handles WMI errors gracefully

## Files Touched

| File | Change |
|------|--------|
| `src/Dmca.Core/Collectors/IInventoryCollector.cs` | Collector interface |
| `src/Dmca.Core/Collectors/PnpDriverCollector.cs` | WMI driver collector |
| `src/Dmca.Core/Models/InventoryItem.cs` | Domain model |

## Testing

- [x] Collector returns items on a real Windows system
- [x] Item IDs follow `drv:` prefix convention
- [x] Graceful error on WMI failure

## Done When

- [x] Acceptance criteria met
- [x] Verified manually on test system
