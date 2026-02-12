---
id: PHASE-03
type: phase
status: planned
owner: ""
---

# PHASE-03: Execution & UI

## Goal

Build the action queue, execution engine, and desktop UI. By the end of this phase, users can visually review inventory, interact with the AI Advisor, approve/reject proposals, and execute safe removal actions through a polished desktop interface.

## In Scope

- Action queue builder (from approved plan items)
- Execution engine:
  - `CREATE_RESTORE_POINT`
  - `UNINSTALL_DRIVER_PACKAGE` (pnputil)
  - `UNINSTALL_DEVICE`
  - `DISABLE_SERVICE`
  - `UNINSTALL_PROGRAM` (quiet uninstall)
  - Dry-run mode
  - Step-by-step audit logging
- Desktop UI (WPF or WinUI 3):
  - Interview wizard screen
  - Inventory table with score columns, hard-block badges, sort/filter
  - Item detail pane with evidence and rationale
  - AI chat pane
  - Proposal review diff screen with approve/reject
  - Execute screen with dry-run preview and typed confirmation for high-risk
  - Session management (new, resume, history)
- Two-step confirmation for high-risk actions
- Action queue persistence and resumability

## Out of Scope

- Rescan/delta reporting (Phase 4)
- Eval harness updates
- Distribution packaging

## Sprints

- [SPRINT-05](../sprints/SPRINT-05.md)
- [SPRINT-06](../sprints/SPRINT-06.md)

## Completion Criteria

- [ ] Action queue builds correctly from an approved plan
- [ ] Restore point is created before any destructive action
- [ ] All five action types execute successfully
- [ ] Dry-run mode produces correct preview without side effects
- [ ] UI displays inventory table with all required columns
- [ ] Proposal diff screen shows changes clearly
- [ ] Two-step confirmation works for high-risk queues
- [ ] Audit log captures every executed action
