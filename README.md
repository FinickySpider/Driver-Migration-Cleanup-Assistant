# Driver Migration Cleanup Assistant (DMCA)

A Windows desktop utility for intelligently managing drivers and software after major hardware migrations. Built with .NET 8, WPF, and AI-powered recommendations.

## Features

- **Automated Inventory** — Scans drivers, services, driver packages, and installed programs using WMI, Registry, and pnputil
- **Smart Scoring** — Deterministic rules engine assigns removal confidence scores to each item
- **AI Advisory** — OpenAI integration provides context-aware recommendations with user-fact verification
- **Safe Execution** — Hard blocks protect system-critical items; all actions require explicit approval
- **Full Audit Trail** — Immutable snapshots and resumable sessions for complete traceability

## Project Status

| Phase | Sprint | Status | Features |
|-------|--------|--------|----------|
| **Phase 1** | Sprint-01 | ✅ Complete | Foundation, session mgmt, user facts, persistence |
| | Sprint-02 | ✅ Complete | 5 inventory collectors (drivers, services, packages, apps) + snapshot assembly |
| **Phase 2** | Sprint-03 | 🔄 Active | Scoring engine & rules evaluation |
| | Sprint-04 | Planned | AI advisor integration |
| **Phase 3** | Sprint-05 | Planned | WPF UI & batch operations |
| | Sprint-06 | Planned | Approvals workflow |
| **Phase 4** | Sprint-07 | Planned | Execution engine & cleanup |
| | Sprint-08 | Planned | Polish, eval validation, release |

## Tech Stack

- **Runtime:** .NET 8 on Windows 10/11
- **UI:** WPF with MVVM Toolkit (future phase)
- **Database:** SQLite via Microsoft.Data.Sqlite + Dapper
- **AI:** OpenAI API (chat completions with tool-calling)
- **API:** ASP.NET Core minimal APIs (loopback only)
- **Testing:** xUnit (57 tests, all passing)

## Quick Start

### Prerequisites
- .NET 8.0+ SDK
- Windows 10/11
- (Optional) OpenAI API key for AI features

### Build & Test
```bash
dotnet build Dmca.slnx
dotnet test Dmca.slnx
```

### Run
```bash
dotnet run --project src/Dmca.App/Dmca.App.csproj
```

First run will create the SQLite database at `%LOCALAPPDATA%\DMCA\dmca.db`.

## Architecture

### Core Layer (`src/Dmca.Core`)
- **Models:** Session, UserFact, InventoryItem, InventorySnapshot
- **Interfaces:** Repository contracts, collector contracts
- **Services:** SessionService, UserFactsService, ScanService, SessionStateMachine

### Data Layer (`src/Dmca.Data`)
- **DmcaDbContext:** SQLite connection + schema management
- **Repositories:** Append-only and immutable-by-design implementations

### App Layer (`src/Dmca.App`)
- **Collectors:** PnpDriverCollector, DevicePresenceCollector, DriverStoreCollector, ServiceCollector, InstalledProgramsCollector, WmiPlatformInfoCollector
- **Program.cs:** Bootstrap and DI wiring

### Key Design Principles

1. **Inventory snapshots are immutable** — never modify a persisted snapshot
2. **Engine owns truth and enforcement** — hard blocks are non-overridable
3. **AI proposes, engine executes** — AI never executes directly
4. **Everything is logged and resumable** — full audit trail
5. **Deterministic scoring** — consistent results across runs

## Safety Rules

**Hard blocks** (non-removable in v1):
- `MICROSOFT_INBOX` — Microsoft-signed core components
- `BOOT_CRITICAL` — Boot drivers currently in use
- `PRESENT_HARDWARE_BINDING` — Drivers bound to present hardware
- `POLICY_PROTECTED` — Windows Update protected items
- `DEPENDENCY_REQUIRED` — Items with active dependencies

## Documentation

- [`docs/design/DESIGN.md`](docs/design/DESIGN.md) — Architecture & design rationale
- [`docs/index/MASTER_INDEX.md`](docs/index/MASTER_INDEX.md) — Project roadmap & phase tracking
- [`docs/index/ROADMAP.md`](docs/index/ROADMAP.md) — Timeline overview
- [`docs/decisions/DECISION_LOG.md`](docs/decisions/DECISION_LOG.md) — Key decisions (ADRs)

## Testing

All unit tests use in-memory SQLite for isolation:
```bash
dotnet test Dmca.slnx --verbosity normal
# → 57 tests, 100% pass rate
```

Test coverage:
- State machine transitions
- Session lifecycle (CRUD)
- Repository round-tripping
- Snapshot immutability
- Collector integration
- pnputil output parsing

## Development

See [`docs/_system/`](docs/_system/) for:
- [`agent-guidance.md`](docs/_system/agent-guidance.md) — Work rules & conventions
- [`bootstrap.md`](docs/_system/bootstrap.md) — Initial setup
- [`lifecycle.md`](docs/_system/lifecycle.md) — Phase/sprint workflows

## License

Proprietary — See LICENSE file



