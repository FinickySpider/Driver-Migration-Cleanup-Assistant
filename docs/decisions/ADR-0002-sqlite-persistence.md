---
id: ADR-0002
type: decision
status: complete
date: 2026-02-12
supersedes: ""
superseded_by: ""
---

# ADR-0002: SQLite for Local Persistence

## Context

DMCA needs a local persistence layer for sessions, inventory snapshots, plans, proposals, action queues, and audit logs. Options considered: SQLite, LiteDB, flat JSON files, embedded SQL Server (LocalDB).

The data model involves relational data (sessions → snapshots → items, plans → items, proposals → changes) with query needs (filter by type, sort by score, join across tables). Data volume is moderate (hundreds to low thousands of items per session).

## Decision

Use **SQLite** via `Microsoft.Data.Sqlite` with **Dapper** for lightweight mapping.

Rationale:
- Relational model fits the data naturally
- Single-file database, zero configuration
- Excellent .NET support via Microsoft.Data.Sqlite
- Dapper provides lightweight ORM without EF Core overhead
- WAL mode supports concurrent reads during execution
- Well-tested, battle-proven embedded database

## Consequences

### Positive

- Zero-config deployment (single .db file)
- Full SQL query capabilities
- Transactions for atomic operations
- Dapper keeps mapping simple and explicit
- Easy to inspect with SQLite Browser

### Negative

- No automatic migrations (manual schema management)
- Single-writer limitation (acceptable for single-user desktop app)
- Raw SQL strings require careful maintenance

## Links

- Related items:
  - FEAT-002
