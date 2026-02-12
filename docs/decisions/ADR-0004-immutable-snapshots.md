---
id: ADR-0004
type: decision
status: complete
date: 2026-02-12
supersedes: ""
superseded_by: ""
---

# ADR-0004: Immutable Inventory Snapshots

## Context

The system scans the machine and produces an `InventorySnapshot`. This snapshot is used as the basis for scoring, AI analysis, and execution planning. If the snapshot could be modified after creation, it would undermine the audit trail and potentially allow the AI or bugs to alter the evidence base.

## Decision

**InventorySnapshots are immutable** once persisted. No update or delete operations are exposed at any layer (repository, service, API).

- Each scan creates a new snapshot with a new UUID
- Rescans create additional snapshots (not updates)
- Comparison between snapshots is done via delta reports
- The repository interface has `Create` and `Get` methods only — no `Update` or `Delete`

## Consequences

### Positive

- Tamper-proof audit trail
- AI cannot alter the evidence it reads
- Delta reports are reliable (comparing two fixed points)
- Simplifies concurrency (no write conflicts)

### Negative

- Storage grows with each rescan (mitigated: snapshots are not large)
- Cannot correct collector errors without a full rescan

## Links

- Related items:
  - FEAT-010
  - FEAT-033
