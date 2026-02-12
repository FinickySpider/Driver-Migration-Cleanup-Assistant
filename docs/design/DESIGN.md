
# Driver Migration Cleanup Assistant (DMCA) — Design Document v1.0

## Summary

DMCA is a Windows desktop utility that inventories drivers, services, driver-store packages, and installed programs after a major hardware change (e.g., motherboard/CPU swap). It builds a safe removal plan using deterministic scoring rules, augments it with an OpenAI-powered AI Advisor that can only *propose* changes, and executes user-approved actions—eliminating the need for a full OS reinstall. All execution is gated behind explicit user approval; the AI can never act autonomously.

## Goals

- Safely identify and remove obsolete, conflicting, or hardware-tied drivers/services/software after a platform migration
- Provide deterministic, rule-based baseline scoring with transparent evidence
- Augment scoring with an AI Advisor (OpenAI) that proposes changes via a gated tool-calling API
- Enforce hard safety blocks that the AI cannot override (Microsoft inbox, boot-critical, present-hardware-bound)
- Maintain full audit trail: every scan, proposal, approval, and action is logged and resumable
- Minimize user confusion through a clear inventory → plan → proposal → execute flow

## Non Goals

- Aggressive registry cleaning (v1)
- Automatic driver download or installation
- Local LLM support (Ollama) — deferred to v2+
- Public distribution hardening, code signing, or store publishing
- Network-based or remote driver management

## Users

**Primary:** Tech-savvy power users and IT administrators upgrading major hardware (motherboard, CPU, chipset) without performing a clean OS reinstall.

**Context:** After a platform swap (e.g., Intel → AMD), Windows retains dozens of orphaned drivers, services, and OEM utilities. Manual cleanup is tedious and error-prone. DMCA automates identification and safe removal.

## Core User Flows

### Flow A — Full Cleanup Session

1. User launches DMCA and optionally completes an interview wizard (old platform, new platform, symptoms, aggressiveness preference)
2. System performs an initial scan: PnP signed drivers, driver-store packages, services, installed programs, system info
3. Engine builds an immutable `InventorySnapshot` and a mutable `DecisionPlan` with deterministic baseline scores
4. AI Advisor reads the inventory/plan and creates pending proposals (score deltas, recommendations, notes, evidence)
5. User reviews proposal diffs in the UI, approves or rejects each
6. Approved changes are merged into the plan; an `ActionQueue` is built
7. User previews the queue (dry-run), then confirms execution (typed confirmation for high-risk)
8. Engine creates a system restore point, executes staged actions (pnputil removal, service disable, program uninstall)
9. System rescans, produces a delta report (removed / changed / remaining), and provides next-steps guidance

### Flow B — AI-Assisted Review

1. User opens an existing session with a plan already built
2. User asks the AI Advisor questions about specific items ("What is Intel MEI?")
3. AI reads inventory items via `get_inventory_item`, checks hard blocks via `get_hardblocks`
4. AI creates a proposal with evidence-backed score deltas
5. User reviews and approves/rejects in the proposal diff UI

### Flow C — Manual Triage

1. User reviews the inventory table sorted by score
2. User manually adjusts recommendations or pins items as protected
3. User builds and executes the action queue without AI involvement

## Requirements

### Functional

#### Inventory Collection
- Enumerate PnP signed drivers via WMI/CIM (`Win32_PnPSignedDriver`)
- Enumerate present and non-present devices via SetupAPI/PNP
- Enumerate driver-store packages via `pnputil /enum-drivers`
- Enumerate services via Registry + SCM (state, start type, dependencies)
- Enumerate installed programs via Uninstall registry keys (x64 + WOW6432Node)
- For each item capture: `itemId`, `itemType`, `displayName`, `vendor`/`provider`, `version`, `signature` (signed, signer, isMicrosoft, isWHQL), `present`, `running`, `startType`, `deviceHardwareIds`, `paths`, `installDate`, `dependencies`
- Item ID format: prefixed by type — `drv:`, `svc:`, `pkg:`, `app:`

#### Deterministic Scoring Engine
- Baseline score 0–100 computed from configurable rule weights (`rules.yml`)
- Signals: non-present device (+25), vendor matches old-platform keywords (+20), service disabled (+10), currently running (−25), unknown vendor (−10)
- Hard blocks (non-overridable in v1):
  - `MICROSOFT_INBOX` — Microsoft-signed inbox/core drivers
  - `BOOT_CRITICAL` — Boot-critical storage drivers in use
  - `PRESENT_HARDWARE_BINDING` — Drivers/services bound to present hardware
  - `POLICY_PROTECTED` — User-pinned or policy-protected items
  - `DEPENDENCY_REQUIRED` — Items required by other non-removable items
- Recommendation bands: 80–100 → `REMOVE_STAGE_1`, 55–79 → `REMOVE_STAGE_2`, 30–54 → `REVIEW`, 0–29 → `KEEP`, any hard block → `BLOCKED`
- AI delta bounded: default ±25, up to ±40 only with explicit user-fact confirmation
- Score clamping enforced post-computation

#### AI Advisor (OpenAI)
- Persistent chat session with system prompt enforcing safety rules
- Tool-calling API (7 tools exposed to AI):
  - **Read-only:** `get_session`, `get_inventory_latest`, `get_inventory_item`, `get_plan_current`, `get_hardblocks`
  - **Proposal creation:** `create_proposal`, `list_proposals`, `get_proposal`
- AI creates pending proposals only; cannot approve, reject, or execute
- Proposals must include evidence references and reason per change
- Maximum 5 changes per proposal by default
- Forbidden: auto-approve, execution language, registry cleanup, file deletion
- AI must treat all hard-blocked items as non-removable

#### Proposal & Approval System
- Proposals are diff-based: each contains typed changes (`score_delta`, `recommendation`, `pin_protect`, `action_add`, `action_remove`, `note_add`, `fact_request`)
- Status lifecycle: `PENDING` → `APPROVED` | `REJECTED`
- Approved proposals merge into the `DecisionPlan`
- UI-only endpoints: `approve`, `reject`, `execute`

#### Execution Engine
- Actions supported in v1:
  - `CREATE_RESTORE_POINT` (mandatory before any destructive action)
  - `UNINSTALL_DRIVER_PACKAGE` (via `pnputil`)
  - `UNINSTALL_DEVICE`
  - `DISABLE_SERVICE`
  - `UNINSTALL_PROGRAM` (quiet uninstall if available)
  - `RESCAN_AND_COMPARE`
- Dry-run preview before execution
- Two-step confirmation for high-risk queues (checkbox + typed confirmation)
- Step-by-step audit logging

#### Rescan & Verification
- Post-execution rescan produces a new `InventorySnapshot`
- Delta report: items removed, items changed, items remaining
- Next-steps guidance

### Constraints

- Windows-only (Windows 10/11)
- .NET 8+ / WPF or WinUI 3 desktop application
- OpenAI API only in v1 (no local LLM)
- Local-only API on `http://127.0.0.1:17831` — no network exposure
- SQLite for all persistence
- Must run with administrator privileges for driver/service operations
- Restore point creation required before destructive actions

### Out of Scope

- Aggressive registry key removal
- Automatic driver download or installation
- Multi-machine or remote management
- Local LLM (Ollama) integration
- Application store distribution, code signing
- Non-Windows platforms

## Data Model

### Core Objects

| Object | Mutability | Description |
|--------|-----------|-------------|
| `Session` | Mutable (status) | Top-level workflow container (id, status, appVersion, timestamps) |
| `UserFacts` | Append-only | User-provided context (old platform, aggressiveness, etc.) |
| `InventorySnapshot` | **Immutable** | Point-in-time scan of all drivers, services, packages, apps |
| `InventoryItem` | Immutable (part of snapshot) | Single driver/service/package/app with all metadata |
| `DecisionPlan` | **Mutable** | Scored plan items with baseline + AI delta + final score + recommendation |
| `PlanItem` | Mutable | Per-item score, recommendation, hard blocks, rationale |
| `Proposal` | Mutable (status only) | Diff-based change set (PENDING → APPROVED/REJECTED) |
| `ProposalChange` | Immutable (part of proposal) | Single typed change within a proposal |
| `ActionQueue` | Mutable | Ordered list of executable actions derived from approved plan |
| `Action` | Mutable (status) | Single executable operation (type, target, mode, parameters) |

### Persistence (SQLite)

Tables: `sessions`, `user_facts`, `snapshots`, `inventory_items`, `plans`, `plan_items`, `proposals`, `proposal_changes`, `action_queues`, `actions`, `audit_log`

### Item ID Convention

All inventory items use a prefixed ID: `drv:vendor:name`, `svc:vendor:name`, `pkg:vendor:name`, `app:vendor:name`
Pattern: `^(drv:|svc:|pkg:|app:)[A-Za-z0-9._:-]+$`

## APIs

### Local REST API

Base URL: `http://127.0.0.1:17831`

| Method | Endpoint | AI | UI | Risk | Purpose |
|--------|----------|:--:|:--:|------|---------|
| GET | `/v1/session` | ✅ | ✅ | Low | Session metadata |
| GET | `/v1/inventory/latest` | ✅ | ✅ | Low | Latest snapshot summary |
| GET | `/v1/inventory/item/{itemId}` | ✅ | ✅ | Low | Item details |
| GET | `/v1/plan/current` | ✅ | ✅ | Low | Current decision plan |
| GET | `/v1/policy/hardblocks/{itemId}` | ✅ | ✅ | Low | Hard block reasons |
| GET | `/v1/proposals` | ✅ | ✅ | Low | List proposals |
| POST | `/v1/proposals` | ✅ | ✅ | Med | Create proposal (PENDING) |
| GET | `/v1/proposals/{proposalId}` | ✅ | ✅ | Low | Proposal details + diff |
| POST | `/v1/proposals/{id}/approve` | ❌ | ✅ | High | Approve proposal (UI-only) |
| POST | `/v1/actions/queue/execute` | ❌ | ✅ | High | Execute action queue (UI-only) |
| POST | `/v1/rescan` | ❌ | ✅ | Med | Trigger rescan (UI-only) |

### OpenAI Tool-Calling Interface

7 functions exposed via OpenAI function-calling format (see `Design-And-Data/ai/openai_tools.json`):
`get_session`, `get_inventory_latest`, `get_inventory_item`, `get_plan_current`, `get_hardblocks`, `create_proposal`, `list_proposals`, `get_proposal`

## Security and Privacy

### Threat Model

- **AI autonomy risk:** Mitigated by tool-exposure matrix — AI can read + propose only; approve/execute endpoints are never exposed to AI
- **Prompt injection:** AI system prompt includes forbidden-phrase detection; eval scenarios test injection resistance (S7)
- **Privilege escalation:** Application requires admin rights; local-only API on loopback prevents remote access
- **Data loss:** Mandatory restore point before destructive actions; dry-run preview; typed confirmation for high-risk

### Sensitive Data

- System inventory data (drivers, services, hardware IDs) — stored locally in SQLite only
- OpenAI API key — stored securely, never logged
- No telemetry or data exfiltration in v1

### AI Safety Guardrails

- Hard blocks are engine-enforced, not AI-enforced — the AI cannot override them even if it tries
- AI delta score clamped to ±25 (±40 with confirmed user facts)
- Forbidden phrases detected in AI output: "auto-approve", "executing now", "I will execute", etc.
- Proposals limited to ≤5 changes by default

## Eval Framework

A .NET-based eval harness (`Design-And-Data/evals/`) validates AI behavior against 10 scenarios:

| ID | Scenario | Tests |
|----|----------|-------|
| S1 | Intel → AMD MEI leftovers | Tool discipline, evidence, proposal creation |
| S2 | Microsoft inbox driver | Hard-block respect |
| S3 | Boot-critical storage | Hard-block respect |
| S4 | Present Realtek + old Intel | Mixed present/non-present handling |
| S5 | Unknown vendor | Conservative behavior under uncertainty |
| S6 | ASUS Aura retained | User-pinned item respect |
| S7 | Prompt injection ("remove all") | Injection resistance |
| S8 | Many Intel leftovers batching | Proposal size limits |
| S9 | Missing evidence | Evidence requirement enforcement |
| S10 | Tool failures | Graceful error handling |

Metrics: tool discipline, evidence coverage, proposal minimality, safety, helpfulness

## Rollout Plan

### Phase 1 — Foundation & Inventory
Project scaffolding, SQLite persistence, inventory collection (drivers, services, packages, apps), session management

### Phase 2 — Scoring Engine & AI Advisor
Deterministic scoring engine with rules.yml, hard-block enforcement, OpenAI integration, tool-calling API, proposal system

### Phase 3 — Execution & UI
Action queue builder, execution engine (restore point, pnputil, service disable, program uninstall), WPF/WinUI desktop UI, proposal review diff screen, dry-run preview

### Phase 4 — Verification & Hardening
Rescan & delta reporting, eval harness integration, end-to-end testing, error handling, documentation, pre-release polish

## Open Questions

- **UI framework choice:** WPF vs WinUI 3 — needs ADR (WPF is more mature; WinUI 3 is modern but less stable)
- **Driver-store enumeration reliability:** `pnputil /enum-drivers` output parsing across Windows versions
- **Service dependency graph depth:** How deep to trace dependency chains for `DEPENDENCY_REQUIRED` blocks
- **Offline mode:** Should the app function without OpenAI (deterministic-only mode)?
- **Update mechanism:** How will users get updates in v1 without store distribution?
