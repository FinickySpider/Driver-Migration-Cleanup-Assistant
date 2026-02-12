---
id: FEAT-008
type: feature
status: complete
priority: high
phase: PHASE-01
sprint: SPRINT-02
owner: ""
depends_on: [FEAT-002]
completed: 2025-07-15
---

# FEAT-008: Service Collector

## Description

Implement a collector that enumerates Windows services via Registry and Service Control Manager (SCM). Captures service name, display name, vendor, start type, running state, and dependencies.

## Acceptance Criteria

- [x] Collector enumerates services from SCM and Registry
- [x] Each service mapped to `InventoryItem` with `itemType = SERVICE`
- [x] Item ID generated as `svc:<vendor>:<name>`
- [x] Start type captured (0=Boot, 1=System, 2=Auto, 3=Manual, 4=Disabled)
- [x] Running state captured
- [x] Service dependencies captured as string array
- [x] Vendor/provider extracted from service executable or registry

## Files Touched

| File | Change |
|------|--------|
| `src/Dmca.Core/Collectors/ServiceCollector.cs` | Service enumeration |

## Testing

- [x] Collector returns services on a real system
- [x] Start type and running state are accurate

## Done When

- [x] Acceptance criteria met
- [x] Verified manually
