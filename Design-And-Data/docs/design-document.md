# Driver Migration Cleanup Assistant (DMCA)
## Design Document v1.0 (Developer Handoff Baseline)

### Goal
Create a robust, AI-assisted Windows utility for identifying and safely uninstalling obsolete, conflicting, or hardware-tied drivers and related software/services after a major system change (e.g., motherboard/CPU swap), minimizing user confusion and maximizing system stability—without a full OS reinstall.

### Target Users
Tech-savvy power users, IT admins, and anyone upgrading major hardware without a clean OS reinstall.

---

## High-Level Flow
1. **Interview / Context**
   - Optional wizard: old vs new platform, symptoms, preferences, goal aggressiveness
   - Persist `UserFacts` for downstream scoring/AI.

2. **Initial System Scan**
   - Enumerate drivers, non-present devices, driver store packages, services, installed programs
   - Collect system info: motherboard/CPU/OS version
   - Persist an immutable `InventorySnapshot`.

3. **Deterministic Classification**
   - Baseline scoring (0–100) using configurable rules
   - Build protected set / hard blocks (non-overridable in v1)
   - Produce a mutable `DecisionPlan` with `PlanItem`s.

4. **AI Advisor + Proposal Loop**
   - OpenAI-only in v1
   - AI can read snapshot/plan and create **pending proposals** (diff-based changes)
   - User reviews diffs and approves/rejects

5. **Action Queue Build**
   - From approved plan, build staged action queue
   - Default dry run preview

6. **Execute**
   - Create restore point (required for destructive actions)
   - Execute queue with safety revalidation
   - Step-by-step logging

7. **Rescan & Verify**
   - Rescan snapshot
   - Delta report: removed/changed/remaining
   - Next steps guidance

---

## Functional Requirements

### Inventory
- Drivers/devices:
  - Signed drivers (WMI/CIM Win32_PnPSignedDriver)
  - Present/non-present devices (SetupAPI/PNP)
  - Driver store packages (pnputil /enum-drivers)
- Services:
  - Registry + SCM state and dependencies
- Installed programs:
  - Uninstall keys (x64 + WOW6432Node)
- For each item, capture:
  - `displayName`, `vendor/provider`, version, signature, presence, running, startType, relevant IDs/paths

### Deterministic Scoring
- Baseline score with rule weights
- Hard blocks:
  - Microsoft inbox/core components
  - Boot-critical drivers in use
  - Present hardware binding (v1 blocks by default)
  - Dependency required
  - Policy protected/pinned

### AI Advisor (OpenAI)
- Persistent chat session
- Tool-calling against internal API:
  - Read inventory/plan/hardblocks
  - Create **pending** proposals (score delta, recommendation, action suggestions, notes, fact requests)
- AI never executes; UI approves; engine enforces policy.

### Execution
- Actions supported in v1:
  - Create restore point
  - Uninstall driver package (pnputil)
  - Disable service
  - Uninstall program (quiet uninstall if available)
  - Rescan and compare
- No aggressive registry cleaning in v1

### Persistence
- SQLite:
  - sessions, user_facts, snapshots, items, plans, proposals, queues, actions, audit

### UI/UX
- Inventory table with baseline/AI delta/final score + hard block badges
- Details pane with evidence and rationale
- Proposal review diff screen + approvals
- Execute screen with dry run + typed confirm for high-risk

---

## Architecture Principles
- **Inventory snapshots are immutable**
- **Engine owns truth and enforcement**
- **AI can write the plan (via proposals); engine executes the plan**
- **Everything is logged and resumable**
