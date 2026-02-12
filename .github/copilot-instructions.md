# DMCA — Copilot Custom Instructions

## Project Overview

**Driver Migration Cleanup Assistant (DMCA)** is a Windows desktop utility (.NET 8 / WPF) that inventories drivers, services, driver-store packages, and installed programs after a major hardware change, builds a safe removal plan using deterministic scoring, augments it with an OpenAI-powered AI Advisor, and executes user-approved cleanup actions.

## Start Procedure

1. Read `docs/index/MASTER_INDEX.md` — this is the **single source of truth**
2. Read `docs/design/DESIGN.md` — the full design specification
3. Read `docs/_system/*` — operating rules, conventions, lifecycle, templates

If DESIGN.md is missing or incomplete, request it. **Do not invent product direction.**

## Work Rules (CRITICAL)

- **Only work on items listed in the active sprint** — check MASTER_INDEX for the active sprint
- **Do not silently expand scope** — if something isn't in the sprint, create a new work item first
- Prefer minimal viable implementation that satisfies acceptance criteria
- Keep changes traceable: acceptance criteria → implementation → verification
- **ALWAYS update** features, sprints, phases, and MASTER_INDEX after completing each task
- Always use templates from `docs/templates/*` when creating new docs files
- At the start of every new sprint, read through all files in `/docs` and create any missing files
- Always keep all tracking documents up to date:
  - Global: `docs/decisions/DECISION_LOG.md`, `docs/index/MASTER_INDEX.md`, `docs/index/ROADMAP.md`
  - Local: feature checklists, sprint checklists, phase checklists

## Populating Docs from a Design Doc

When asked to populate docs:

1. Create 2–4 phases that cover the design
2. Create 1–3 initial sprints
3. Create work items for the first sprint with testable acceptance criteria
4. Create ADRs for architecture-shaping decisions
5. Update MASTER_INDEX: set active phase, active sprint, list items, link phases/sprints/decision log

## Status Vocabulary (STRICT — no other status words allowed)

- `planned`
- `active` (phases and sprints only)
- `in_progress`
- `blocked`
- `review`
- `complete`
- `deprecated`

## Naming Conventions

| Type | Prefix | Example |
|------|--------|---------|
| Phase | `PHASE-XX` | `PHASE-02-core-features.md` |
| Sprint | `SPRINT-XX` | `SPRINT-03.md` |
| Feature | `FEAT-XXX` | `FEAT-014-batch-export.md` |
| Bug | `BUG-XXX` | `BUG-007-export-crash.md` |
| Refactor | `REFACTOR-XXX` | `REFACTOR-004-state-cleanup.md` |
| Decision (ADR) | `ADR-XXXX` | `ADR-0003-storage-choice.md` |

- Use zero-padded numbers
- Use kebab-case slugs
- Never reuse or renumber IDs once created

## Cross References

Every work item must reference its phase, sprint, and dependencies by ID using standard markdown links with relative paths.

## Architecture Principles (from DESIGN.md)

- **Inventory snapshots are immutable** — never modify a persisted snapshot
- **Engine owns truth and enforcement** — the engine enforces hard blocks, not the AI
- **AI can write the plan (via proposals); engine executes the plan** — AI never executes
- **Everything is logged and resumable** — full audit trail
- Hard blocks are non-overridable in v1: `MICROSOFT_INBOX`, `BOOT_CRITICAL`, `PRESENT_HARDWARE_BINDING`, `POLICY_PROTECTED`, `DEPENDENCY_REQUIRED`
- AI tool-exposure: read + propose only; approve/execute/rescan are UI-only

## Technology Stack

- **Runtime:** .NET 8, Windows 10/11
- **UI:** WPF with MVVM (CommunityToolkit.Mvvm)
- **Database:** SQLite via Microsoft.Data.Sqlite + Dapper
- **AI:** OpenAI API (chat completions with tool-calling)
- **API:** ASP.NET Core minimal APIs, loopback only (`127.0.0.1:17831`)
- **Testing:** xUnit, eval harness (10 scenarios S1–S10)

## Key Design Data (reference)

- Scoring rules: `Design-And-Data/rules/rules.yml`
- AI tool definitions: `Design-And-Data/ai/openai_tools.json`
- AI system prompt: `Design-And-Data/ai/ai_tool_policy_prompt.txt`
- API spec: `Design-And-Data/api/openapi_3_1.yml`
- Schemas: `Design-And-Data/schemas/*.json`
- Eval fixtures: `Design-And-Data/evals/fixtures/S*.json`

## Safety Rules for AI Advisor Code

When implementing AI-related features:

- Only expose the 7 allowed tools to OpenAI: `get_session`, `get_inventory_latest`, `get_inventory_item`, `get_plan_current`, `get_hardblocks`, `create_proposal`, `list_proposals`, `get_proposal`
- Never expose approve/reject/execute/rescan to AI tools
- AI delta score must be clamped: ±25 default, ±40 only with explicit user-fact confirmation
- Max 5 changes per proposal
- Detect forbidden phrases in AI output: "auto-approve", "approve automatically", "executing now", "i will execute", "i executed", "i'm going to run the uninstall"
- All hard-blocked items are non-removable regardless of AI proposals

## Code Style

- Prefer explicit checklists over prose
- Keep scope tight
- Do not invent requirements not in the design
- If scope changes, create an ADR
- Nullable reference types enabled
- Implicit usings enabled
- File-scoped namespaces preferred
