---
id: ADR-0003
type: decision
status: complete
date: 2026-02-12
supersedes: ""
superseded_by: ""
---

# ADR-0003: AI Tool-Exposure Matrix — Read + Propose Only

## Context

The AI Advisor (OpenAI) interacts with the application through a tool-calling API. We must decide which endpoints are exposed to the AI and which are restricted to the UI. The risk is that an AI could attempt to approve its own proposals or trigger execution, bypassing user consent.

## Decision

Adopt a strict **read + propose** exposure model for the AI:

**AI-accessible (7 tools):**
- `get_session` — read session metadata
- `get_inventory_latest` — read inventory summary
- `get_inventory_item` — read item details
- `get_plan_current` — read decision plan
- `get_hardblocks` — read hard-block reasons
- `create_proposal` — create pending proposals
- `list_proposals` / `get_proposal` — read proposals

**UI-only (never AI-accessible):**
- `approve` / `reject` proposal
- `execute` action queue
- `rescan`

This is enforced at two levels:
1. The OpenAI tool definitions only include the 7 allowed tools
2. The API server validates caller identity for restricted endpoints

## Consequences

### Positive

- AI cannot approve its own proposals or trigger execution
- Clear separation of concerns: AI advises, user decides, engine executes
- Auditable: all AI actions are visible as pending proposals
- Matches eval framework expectations (S7 injection resistance)

### Negative

- AI cannot trigger rescan even when it would be helpful (minor UX friction)
- Requires two-step flow for every AI suggestion (proposal → approval)

## Links

- Related items:
  - FEAT-016
  - FEAT-017
  - FEAT-018
