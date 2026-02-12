---
id: PHASE-02
type: phase
status: complete
owner: ""
completed: 2026-02-12
---

# PHASE-02: Scoring Engine & AI Advisor

## Goal

Implement the deterministic scoring engine (rules.yml-driven), hard-block enforcement, OpenAI tool-calling integration, and the proposal system. By the end of this phase, the engine produces a scored `DecisionPlan` and the AI Advisor can read inventory/plan data and create pending proposals.

## In Scope

- Deterministic scoring engine:
  - Rule loading from `rules.yml`
  - Signal evaluation (non-present, vendor keyword match, service disabled, running penalty, unknown vendor)
  - Score computation with clamping (0–100)
- Hard-block enforcement:
  - `MICROSOFT_INBOX`, `BOOT_CRITICAL`, `PRESENT_HARDWARE_BINDING`, `POLICY_PROTECTED`, `DEPENDENCY_REQUIRED`
  - Hard blocks override score-based recommendations → `BLOCKED`
- Recommendation band assignment
- `DecisionPlan` and `PlanItem` persistence
- AI Advisor:
  - OpenAI client integration (chat completions with tool-calling)
  - System prompt with safety rules
  - 7-tool API implementation (get_session, get_inventory_latest, get_inventory_item, get_plan_current, get_hardblocks, create_proposal, list_proposals, get_proposal)
  - AI delta score clamping (±25 default, ±40 with user-fact confirmation)
- Proposal system:
  - Create, list, get proposals
  - Proposal status lifecycle (PENDING → APPROVED/REJECTED)
  - Proposal merge into DecisionPlan on approval
- Local REST API server (loopback only)
- Policy tests and eval harness integration

## Out of Scope

- Execution of actions
- Desktop UI
- Local LLM support

## Sprints

- [SPRINT-03](../sprints/SPRINT-03.md)
- [SPRINT-04](../sprints/SPRINT-04.md)

## Completion Criteria

- [x] Scoring engine produces correct baseline scores for all eval fixtures (S1–S10)
- [x] Hard blocks prevent removal recommendations for protected items
- [x] AI Advisor can call all 7 exposed tools and receive valid responses
- [x] AI cannot call approve/execute/rescan endpoints
- [x] Proposals are created as PENDING and require UI approval
- [x] AI delta clamping enforced (±25 default, ±40 with user-fact)
- [x] Local API serves on 127.0.0.1:17831
- [x] Eval scenarios S1–S7 pass

## Completion Notes

Phase 2 delivered the full deterministic scoring engine and AI Advisor integration across two sprints (SPRINT-03, SPRINT-04). Key deliverables:

- **Scoring engine** (SPRINT-03): YAML rules loader (YamlDotNet), signal evaluator with 5 operators, baseline scorer with band assignment, hard-block evaluator for all 5 block types, and decision plan generation with SQLite persistence.
- **AI Advisor & API** (SPRINT-04): ASP.NET Core minimal API (13 endpoints, loopback-only), OpenAI client with tool-calling loop, safety guardrails (forbidden-phrase detection, delta clamping, allowed-tool enforcement), proposal system with full CRUD lifecycle, and plan merge service.
- 162 total tests all passing across both sprints.
