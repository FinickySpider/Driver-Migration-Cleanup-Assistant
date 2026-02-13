---
id: FEAT-033
type: feature
status: complete
priority: high
phase: PHASE-04
sprint: SPRINT-07
owner: ""
depends_on: [FEAT-010, FEAT-025]
completed: 2025-07-19
---

# FEAT-033: Rescan and New Snapshot Creation

## Description

Implement the rescan operation that creates a new `InventorySnapshot` after action execution. This allows comparison between pre-execution and post-execution states.

## Acceptance Criteria

- [x] Rescan triggers all five collectors
- [x] New immutable `InventorySnapshot` created and persisted
- [x] Previous snapshot remains unchanged (immutable)
- [x] Session can have multiple snapshots (ordered by creation time)
- [x] Session status transitions to `COMPLETED` after rescan
- [x] `POST /v1/rescan` triggers rescan (UI-only endpoint)

## Files Touched

| File | Change |
|------|--------|
| `src/Dmca.Core/Services/RescanService.cs` | Rescan orchestrator |
| `src/Dmca.Core/Interfaces/ISnapshotRepository.cs` | Added GetAllBySessionIdAsync |
| `src/Dmca.Data/Repositories/SnapshotRepository.cs` | Implemented GetAllBySessionIdAsync |
| `src/Dmca.App/Api/DmcaApiEndpoints.cs` | POST /v1/rescan endpoint |
| `src/Dmca.App/App.xaml.cs` | Wired RescanService into ServiceContainer |

## Testing

- [x] Rescan produces new snapshot with different ID
- [x] Original snapshot unchanged
- [x] Session status updated
- [x] Graceful degradation when collectors fail
- [x] Platform info optional on rescan

## Done When

- [x] Acceptance criteria met
- [x] Verified with 8 unit tests (RescanServiceTests)
