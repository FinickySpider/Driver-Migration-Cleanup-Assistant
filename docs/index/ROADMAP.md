
# Roadmap

Keep this file lightweight. Details live in phases and sprints.

## Milestones

### M1 — Foundation Complete (PHASE-01) ✅
- Project scaffolding, SQLite persistence, session management
- All five inventory collectors operational
- Immutable InventorySnapshot persisted
- **Target:** SPRINT-01 + SPRINT-02
- **Status:** complete (2025-07-15) — 57 tests passing

### M2 — Scoring & AI Operational (PHASE-02) ✅
- Deterministic scoring engine with rules.yml
- Hard-block enforcement
- OpenAI tool-calling integration
- Proposal system (create, approve, merge)
- **Target:** SPRINT-03 + SPRINT-04
- **Status:** complete (2026-02-12) — 162 tests passing

### M3 — Execution & UI (PHASE-03) ✅
- Action queue builder and execution engine
- Desktop UI (WPF) with full workflow
- Restore point, driver uninstall, service disable, program uninstall
- **Target:** SPRINT-05 + SPRINT-06
- **Status:** complete (2026-02-12) — 223 tests passing

### M4 — v1.0 Pre-Release (PHASE-04) ✅
- Rescan and delta reporting
- Full eval harness CI (S1–S10 passing)
- Error handling hardening
- Documentation and polish
- **Target:** SPRINT-07 + SPRINT-08
- **Status:** complete (2025-07-19) — 275 tests passing

## Notes

- Strategic changes should be recorded as ADRs.
- Local LLM (Ollama) support deferred to v2.
- Code signing and store distribution deferred to post-v1.
