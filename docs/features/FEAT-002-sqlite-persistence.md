---
id: FEAT-002
type: feature
status: complete
priority: high
phase: PHASE-01
sprint: SPRINT-01
owner: ""
depends_on: [FEAT-001]
completed: 2025-07-15
---

# FEAT-002: SQLite Persistence Layer

## Description

Implement the SQLite database layer using a lightweight ORM or raw ADO.NET (Dapper or Microsoft.Data.Sqlite). Create the schema for all Phase 1 tables: `sessions`, `user_facts`, `snapshots`, `inventory_items`. Include migrations support and a database initializer.

## Acceptance Criteria

- [x] SQLite database file is created on first run
- [x] Schema includes tables: `sessions`, `user_facts`, `snapshots`, `inventory_items`
- [x] All columns match the JSON schemas in `Design-And-Data/schemas/`
- [x] Database initializer creates schema if not present
- [x] Repository interfaces defined in `Dmca.Core`
- [x] Repository implementations in `Dmca.Data`
- [x] Unit tests verify CRUD operations for each table

## Files Touched

| File | Change |
|------|--------|
| `src/Dmca.Data/DmcaDbContext.cs` | Database context / connection manager |
| `src/Dmca.Data/Schema/` | Schema creation scripts |
| `src/Dmca.Data/Repositories/` | Repository implementations |
| `src/Dmca.Core/Interfaces/` | Repository interfaces |

## Implementation Notes

- Use `Microsoft.Data.Sqlite` NuGet package
- Consider Dapper for lightweight mapping
- Database file stored in app data directory

## Testing

- [x] In-memory SQLite tests for all repositories
- [x] Schema creation is idempotent

## Done When

- [x] Acceptance criteria met
- [x] Verified manually
