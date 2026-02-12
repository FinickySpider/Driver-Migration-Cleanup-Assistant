---
id: FEAT-020
type: feature
status: complete
priority: high
phase: PHASE-03
sprint: SPRINT-05
owner: ""
depends_on: [FEAT-019]
---

# FEAT-020: Action Queue Builder

## Description

Implement the action queue builder that derives an ordered list of executable actions from the approved decision plan. Items recommended for removal are mapped to appropriate action types, with restore point creation as the mandatory first action.

## Acceptance Criteria

- [ ] Queue builds only from items with recommendation REMOVE_STAGE_1 or REMOVE_STAGE_2
- [ ] BLOCKED and KEEP items are excluded
- [ ] REVIEW items included only if explicitly confirmed by user
- [ ] `CREATE_RESTORE_POINT` is always the first action
- [ ] Action type is inferred from item type (DRIVER → UNINSTALL_DRIVER_PACKAGE, SERVICE → DISABLE_SERVICE, APP → UNINSTALL_PROGRAM)
- [ ] Queue supports `DRY_RUN_FIRST` mode
- [ ] Queue is persisted to SQLite
- [ ] Session status transitions to `READY_TO_EXECUTE`

## Files Touched

| File | Change |
|------|--------|
| `src/Dmca.Core/Execution/ActionQueueBuilder.cs` | Queue builder |
| `src/Dmca.Core/Models/ActionQueue.cs` | Domain model |
| `src/Dmca.Core/Models/Action.cs` | Action model |
| `src/Dmca.Data/Repositories/ActionQueueRepository.cs` | Persistence |

## Testing

- [ ] Queue excludes BLOCKED items
- [ ] Restore point is first action
- [ ] Action types match item types

## Done When

- [ ] Acceptance criteria met
- [ ] Verified manually
