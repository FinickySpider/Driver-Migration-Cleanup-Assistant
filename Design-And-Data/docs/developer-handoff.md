# Driver Migration Cleanup Assistant (DMCA)
## Developer Handoff Specification v1.0

### Purpose
Windows desktop utility that inventories drivers/services/software after major hardware changes, builds a safe removal plan, and executes user-approved actions. OpenAI-powered advisor can propose plan changes via gated API, but cannot execute without explicit user approval.

---

## Scope (v1)
### In-scope
- Inventory: PnP signed drivers, driver store packages, services, installed programs
- Baseline scoring + protected set (hard blocks)
- AI advisor (OpenAI only) that can propose plan changes via API
- Proposal review UI (diff, approve/reject)
- Action execution engine: restore point, pnputil package removal, service disable, program uninstall
- Rescan + delta report
- SQLite persistence

### Out-of-scope
- Aggressive registry cleaning
- Automatic driver download/install
- Local LLM (Ollama)
- Public distribution hardening and signing

---

## Core Objects
- InventorySnapshot (immutable)
- DecisionPlan (mutable)
- Proposal (diff-based; pending until approved)
- ActionQueue (executable; derived from approved plan)

---

## Safety Model
### Hard blocks (non-overridable in v1)
- Microsoft inbox/core drivers (policy + signature)
- Boot-critical storage drivers in use
- Drivers bound to present hardware IDs (v1 blocks)
- Dependency-required items
- User pinned protected items

### Two-step approval for risky queues
- Checkbox + typed confirmation

---

## Deterministic Scoring
- Baseline score 0–100
- AI delta score bounded (default ±25; up to ±40 with explicit user-fact confirmation)
- Recommendation bands:
  - 80–100 REMOVE_STAGE_1
  - 55–79 REMOVE_STAGE_2
  - 30–54 REVIEW
  - 0–29 KEEP
  - any hard block => BLOCKED

---

## AI Tool-Calling API (v1)
### Read-only
- get_session
- get_inventory_latest
- get_inventory_item
- get_plan_current
- get_hardblocks

### Proposal creation
- create_proposal
- list_proposals
- get_proposal

### UI-only (not exposed to AI tools in v1)
- approve/reject proposal
- build/execute action queue
- rescan
