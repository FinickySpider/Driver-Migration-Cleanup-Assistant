---
id: FEAT-033
type: feature
status: planned
priority: high
phase: PHASE-04
sprint: SPRINT-07
owner: ""
depends_on: [FEAT-010, FEAT-025]
---

# FEAT-033: Rescan and New Snapshot Creation

## Description

Implement the rescan operation that creates a new `InventorySnapshot` after action execution. This allows comparison between pre-execution and post-execution states.

## Acceptance Criteria

- [ ] Rescan triggers all five collectors
- [ ] New immutable `InventorySnapshot` created and persisted
- [ ] Previous snapshot remains unchanged (immutable)
- [ ] Session can have multiple snapshots (ordered by creation time)
- [ ] Session status transitions to `COMPLETED` after rescan
- [ ] `POST /v1/rescan` triggers rescan (UI-only endpoint)

## Files Touched

| File | Change |
|------|--------|
| `src/Dmca.Core/Services/RescanService.cs` | Rescan orchestrator |

## Testing

- [ ] Rescan produces new snapshot with different ID
- [ ] Original snapshot unchanged
- [ ] Session status updated

## Done When

- [ ] Acceptance criteria met
- [ ] Verified manually
