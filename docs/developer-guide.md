# DMCA — Developer Guide

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Repository Setup](#repository-setup)
3. [Project Architecture](#project-architecture)
4. [Building](#building)
5. [Testing](#testing)
6. [Eval Harness](#eval-harness)
7. [API Reference](#api-reference)
8. [Key Design Decisions](#key-design-decisions)
9. [Code Conventions](#code-conventions)
10. [Adding New Features](#adding-new-features)

---

## Prerequisites

| Tool | Version | Notes |
|------|---------|-------|
| **.NET SDK** | 8.0+ | Download from [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/8.0) |
| **Windows** | 10/11 | Required for WPF and WMI/registry collectors |
| **Admin privileges** | — | Required for running collectors and execution tests |
| **Git** | 2.30+ | Standard Git installation |
| **IDE** | VS 2022 / VS Code / Rider | Any .NET-capable editor |

Optional:
- **OpenAI API Key** — For live AI Advisor testing and live evals
- **.NET 10 SDK** — The solution uses `.slnx` format (new solution format); .NET 10 SDK creates these natively, but .NET 8 SDK with `RollForward=LatestMajor` can build it

---

## Repository Setup

```bash
# Clone
git clone https://github.com/FinickySpider/Driver-Migration-Cleanup-Assistant.git
cd Driver-Migration-Cleanup-Assistant

# Restore NuGet packages
dotnet restore Dmca.slnx

# Build
dotnet build Dmca.slnx

# Run tests
dotnet test Dmca.slnx --verbosity normal
```

### Directory Structure

```
├── .github/workflows/      CI pipeline (build, test, eval)
├── Design-And-Data/         Schemas, rules, AI tools, eval fixtures
│   ├── ai/                  OpenAI tool definitions & system prompt
│   ├── api/                 OpenAPI spec
│   ├── evals/               Eval harness (standalone xUnit project)
│   ├── rules/               YAML scoring rules
│   └── schemas/             JSON schemas
├── docs/                    Project documentation
│   ├── _system/             Operating rules & templates
│   ├── decisions/           Architecture Decision Records
│   ├── design/              DESIGN.md (full specification)
│   ├── features/            Feature specifications
│   ├── index/               MASTER_INDEX.md & ROADMAP
│   ├── phases/              Phase tracking
│   ├── refactors/           Refactor specifications
│   ├── sprints/             Sprint tracking
│   └── templates/           Doc templates
├── src/
│   ├── Dmca.Core/           Domain models, services, scoring, AI, execution
│   ├── Dmca.Data/           SQLite persistence (Dapper)
│   └── Dmca.App/            WPF UI, collectors, REST API
├── tests/
│   └── Dmca.Tests/          Unit tests (xUnit)
├── Directory.Build.props    Shared build properties
├── Dmca.slnx                Solution file (.NET new format)
└── .editorconfig             Code style rules
```

---

## Project Architecture

### Dependency Graph

```
Dmca.App (WPF, net8.0-windows)
  ├── Dmca.Core (net8.0)
  └── Dmca.Data (net8.0)
        └── Dmca.Core

Dmca.Tests (net8.0-windows)
  ├── Dmca.Core
  ├── Dmca.Data
  └── Dmca.App
```

### Layer Responsibilities

| Project | Purpose | Key Types |
|---------|---------|-----------|
| **Dmca.Core** | Domain models, business logic, interfaces | `Session`, `InventoryItem`, `DecisionPlan`, `ActionQueue`, `AiAdvisorService`, `ExecutionEngine` |
| **Dmca.Data** | SQLite persistence via Dapper | `DmcaDbContext`, `*Repository` classes |
| **Dmca.App** | WPF UI (MVVM), system collectors, REST API | `MainViewModel`, `*View.xaml`, `*Collector`, `DmcaApiEndpoints` |
| **Dmca.Tests** | Unit tests | xUnit test classes |

### Key Subsystems

1. **Inventory** — Collectors gather data from WMI, registry, pnputil, SCM
2. **Scoring** — Rules engine (YAML) + signal evaluator + hard-block evaluator
3. **AI Advisor** — OpenAI chat completions with tool-calling loop
4. **Proposal System** — AI creates proposals → user approves → merged into plan
5. **Execution** — Action queue builder → dry run → live execution with audit log
6. **Verification** — Rescan + delta report generation

---

## Building

```bash
# Debug build (default)
dotnet build Dmca.slnx

# Release build
dotnet build Dmca.slnx --configuration Release

# Clean
dotnet clean Dmca.slnx
```

### Build Properties (Directory.Build.props)

- `TargetFramework`: net8.0 (Core/Data), net8.0-windows (App/Tests)
- `Nullable`: enable
- `TreatWarningsAsErrors`: true
- `LangVersion`: 12
- `RollForward`: LatestMajor (allows building with .NET 10 SDK)

---

## Testing

```bash
# Run all tests
dotnet test Dmca.slnx --verbosity normal

# Run specific test class
dotnet test Dmca.slnx --filter "FullyQualifiedName~DeltaReportGeneratorTests"

# Run with coverage
dotnet test Dmca.slnx --collect:"XPlat Code Coverage"
```

### Test Organization

| File | Tests | Scope |
|------|-------|-------|
| `SessionRepositoryTests` | CRUD operations | Data layer |
| `SessionServiceTests` | Session lifecycle | Service layer |
| `SessionStateMachineTests` | State transitions | Domain logic |
| `ScanServiceTests` | Collector orchestration | Service layer |
| `RulesLoaderTests` | YAML parsing | Scoring |
| `SignalEvaluatorTests` | Signal matching | Scoring |
| `BaselineScorerTests` | Score computation | Scoring |
| `HardBlockEvaluatorTests` | Block detection | Scoring |
| `PlanServiceTests` | Plan generation | Service layer |
| `ProposalServiceTests` | CRUD + status | Service layer |
| `PlanMergeServiceTests` | Proposal merge | Service layer |
| `AiSafetyGuardTests` | Guardrails | AI safety |
| `ConversationManagerTests` | Message tracking | AI |
| `ActionQueueBuilderTests` | Queue construction | Execution |
| `ExecutionEngineTests` | Orchestration | Execution |
| `AuditLoggerTests` | Audit logging | Execution |
| `ActionHandlerTests` | Action handlers | Execution |
| `ViewModelTests` | ViewModel behavior | UI |
| `DeltaReportGeneratorTests` | Delta comparison | Verification |
| `RescanServiceTests` | Rescan flow | Verification |
| `ErrorHandlingTests` | Exception types + retry | Error handling |

---

## Eval Harness

The eval harness validates AI behavior against 10 scenarios (S1–S10).

### Running Evals

```bash
# Offline mode (mock responses — no API key needed)
cd Design-And-Data/evals/dmca-evals
dotnet test --filter "Category!=Live"

# Live mode (requires OPENAI_API_KEY)
set OPENAI_API_KEY=sk-your-key
dotnet test --filter "Category=Live"
```

### Scenarios

| ID | Description | Tests |
|----|-------------|-------|
| S1 | Intel→AMD MEI driver removal | Correct targeting |
| S2 | Microsoft inbox driver protection | Must not propose removal |
| S3 | Boot-critical storage driver | Hard block enforcement |
| S4 | Present Realtek + old Intel | Mixed present/absent |
| S5 | Unknown vendor handling | Graceful uncertainty |
| S6 | ASUS Aura retained by user | User preference respected |
| S7 | Prompt injection "remove all" | Safety guardrail |
| S8 | Many Intel leftovers (batching) | Multi-proposal batching |
| S9 | Missing evidence should fail | Evidence requirement |
| S10 | Tool failures | Error recovery |

### CI Integration

The GitHub Actions workflow (`.github/workflows/ci.yml`) runs:
1. **Build & Unit Tests** — All unit tests
2. **Eval Offline** — S1–S10 with mock responses
3. **Eval Live** — (main branch only, with `OPENAI_API_KEY` secret)

---

## API Reference

DMCA runs a local REST API on `127.0.0.1:17831` (loopback only).

### Endpoints

| Method | Path | Description |
|--------|------|-------------|
| GET | `/v1/session` | Get current session |
| GET | `/v1/inventory/latest` | Get latest snapshot summary |
| GET | `/v1/inventory/item/{itemId}` | Get item details |
| GET | `/v1/plan/current` | Get current decision plan |
| GET | `/v1/policy/hardblocks/{itemId}` | Get hard blocks for an item |
| GET | `/v1/proposals` | List all proposals |
| POST | `/v1/proposals` | Create a proposal |
| GET | `/v1/proposals/{id}` | Get proposal details |
| POST | `/v1/proposals/{id}/approve` | Approve a proposal |
| POST | `/v1/proposals/{id}/reject` | Reject a proposal |
| POST | `/v1/rescan` | Trigger rescan (UI-only) |
| POST | `/v1/actions/queue/execute` | Execute action queue (UI-only) |

### AI-Exposed Tools (8 allowed)

| Tool | Maps to |
|------|---------|
| `get_session` | `GET /v1/session` |
| `get_inventory_latest` | `GET /v1/inventory/latest` |
| `get_inventory_item` | `GET /v1/inventory/item/{itemId}` |
| `get_plan_current` | `GET /v1/plan/current` |
| `get_hardblocks` | `GET /v1/policy/hardblocks/{itemId}` |
| `create_proposal` | `POST /v1/proposals` |
| `list_proposals` | `GET /v1/proposals` |
| `get_proposal` | `GET /v1/proposals/{id}` |

Full OpenAPI spec: `Design-And-Data/api/openapi_3_1.yml`

---

## Key Design Decisions

All architecture-shaping decisions are recorded as ADRs in `docs/decisions/`:

| ADR | Decision |
|-----|----------|
| ADR-0001 | WPF chosen for UI (Windows-only, .NET 8 native) |
| ADR-0002 | SQLite for persistence (embedded, zero-config) |
| ADR-0003 | AI tool-exposure matrix (8 read/propose tools only) |
| ADR-0004 | Immutable snapshots (never modify persisted data) |
| ADR-0005 | Deterministic scoring (reproducible, rules-based) |

### Core Principles

1. **Inventory snapshots are immutable** — never modify a persisted snapshot
2. **Engine owns truth and enforcement** — hard blocks are engine-enforced, not AI
3. **AI can propose; engine executes** — AI never directly executes actions
4. **Everything is logged and resumable** — full audit trail

---

## Code Conventions

- **Nullable reference types** enabled project-wide
- **File-scoped namespaces** preferred
- **`required` keyword** for required init properties
- **Explicit types** over `var` for complex types
- **XML doc comments** on all public members
- **TreatWarningsAsErrors** = true (zero warnings policy)
- **LangVersion** = 12 (C# 12 features)

### Naming

- Classes: PascalCase
- Methods: PascalCase with `Async` suffix for async
- Properties: PascalCase
- Local variables: camelCase
- Constants: PascalCase
- Enums: UPPER_SNAKE_CASE for domain enums matching API schemas

---

## Adding New Features

1. **Create a feature spec** in `docs/features/` using the template
2. **Add to the active sprint** in `docs/sprints/`
3. **Implement** following the layer architecture (Core → Data → App)
4. **Write tests** in `tests/Dmca.Tests/`
5. **Build and verify**: `dotnet build Dmca.slnx && dotnet test Dmca.slnx`
6. **Update docs**: feature spec status → complete, sprint checklist, MASTER_INDEX
