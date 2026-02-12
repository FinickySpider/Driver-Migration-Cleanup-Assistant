---
id: SPRINT-04
type: sprint
status: complete
phase: PHASE-02
timebox: "2 weeks"
owner: ""
completed: 2026-02-12
---

# SPRINT-04

## Goals

OpenAI integration, tool-calling API, and proposal system are functional. The AI Advisor can read inventory/plan data, create proposals, and proposals can be approved/rejected via UI-only endpoints. Local REST API serves on loopback.

## Planned Work

### Features

- [FEAT-015: Local REST API server](../features/FEAT-015-local-rest-api.md) — complete
- [FEAT-016: OpenAI client and tool-calling integration](../features/FEAT-016-openai-integration.md) — complete
- [FEAT-017: AI system prompt and safety guardrails](../features/FEAT-017-ai-safety-guardrails.md) — complete
- [FEAT-018: Proposal CRUD and lifecycle](../features/FEAT-018-proposal-system.md) — complete
- [FEAT-019: Proposal merge into decision plan](../features/FEAT-019-proposal-merge.md) — complete

### Bugs

- (none)

### Refactors

- (none)

## Deferred / Carryover

- (none)

## Completion Notes

- **Local REST API**: ASP.NET Core minimal APIs on `127.0.0.1:17831`; 13 endpoints implemented covering session, inventory, plan, hardblocks, proposals, actions, and rescan; proper HTTP status codes and JSON schema compliance.
- **OpenAI integration**: `IAiModelClient` / `OpenAiModelClient` with tool-calling loop; `AiAdvisorService` orchestrates multi-turn conversation; tool dispatch routes calls to local service methods.
- **Safety guardrails**: Forbidden-phrase detection (6 phrases), AI delta clamping (±25 default, ±40 with user-fact), max 5 changes per proposal, allowed-tool enforcement preventing AI access to approve/execute/rescan.
- **Proposal system**: `ProposalService` CRUD with `ProposalRepository`; full lifecycle PENDING → APPROVED / REJECTED; all 7 change types supported; evidence and risk summary stored.
- **Proposal merge**: `PlanMergeService` applies approved proposal changes with delta clamping and hard-block protection; plan re-persisted after merge.
- 162 total tests all passing.
