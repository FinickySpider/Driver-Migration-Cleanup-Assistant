
# Master Index

Single source of truth. The agent starts here.

## Project

Driver Migration Cleanup Assistant (DMCA)

## Active Phase

- [PHASE-03: Execution & UI](../phases/PHASE-03-execution-and-ui.md) — active

## Active Sprint

- [SPRINT-05](../sprints/SPRINT-05.md) — planned

## In Progress

- (none)

## Phases

- [PHASE-01: Foundation & Inventory](../phases/PHASE-01-foundation-and-inventory.md) — complete (2025-07-15)
- [PHASE-02: Scoring Engine & AI Advisor](../phases/PHASE-02-scoring-engine-and-ai-advisor.md) — complete (2026-02-12)
- [PHASE-03: Execution & UI](../phases/PHASE-03-execution-and-ui.md) — active
- [PHASE-04: Verification & Hardening](../phases/PHASE-04-verification-and-hardening.md) — planned

## Sprints

- [SPRINT-01](../sprints/SPRINT-01.md) — complete (PHASE-01)
- [SPRINT-02](../sprints/SPRINT-02.md) — complete (PHASE-01)
- [SPRINT-03](../sprints/SPRINT-03.md) — complete (PHASE-02)
- [SPRINT-04](../sprints/SPRINT-04.md) — complete (PHASE-02)
- [SPRINT-05](../sprints/SPRINT-05.md) — planned (PHASE-03)
- [SPRINT-06](../sprints/SPRINT-06.md) — planned (PHASE-03)
- [SPRINT-07](../sprints/SPRINT-07.md) — planned (PHASE-04)
- [SPRINT-08](../sprints/SPRINT-08.md) — planned (PHASE-04)

## Features

### SPRINT-01 (PHASE-01)
- [FEAT-001: Project scaffolding](../features/FEAT-001-project-scaffolding.md) — complete
- [FEAT-002: SQLite persistence](../features/FEAT-002-sqlite-persistence.md) — complete
- [FEAT-003: Session management](../features/FEAT-003-session-management.md) — complete
- [FEAT-004: User facts collection](../features/FEAT-004-user-facts-collection.md) — complete

### SPRINT-02 (PHASE-01)
- [FEAT-005: PnP driver collector](../features/FEAT-005-pnp-driver-collector.md) — complete
- [FEAT-006: Device presence enumeration](../features/FEAT-006-device-presence-enumeration.md) — complete
- [FEAT-007: Driver-store collector](../features/FEAT-007-driver-store-collector.md) — complete
- [FEAT-008: Service collector](../features/FEAT-008-service-collector.md) — complete
- [FEAT-009: Installed programs collector](../features/FEAT-009-installed-programs-collector.md) — complete
- [FEAT-010: Inventory snapshot assembly](../features/FEAT-010-inventory-snapshot-assembly.md) — complete

### SPRINT-03 (PHASE-02)
- [FEAT-011: Rules engine](../features/FEAT-011-rules-engine.md) — complete
- [FEAT-012: Signal evaluation](../features/FEAT-012-signal-evaluation.md) — complete
- [FEAT-013: Hard-block enforcement](../features/FEAT-013-hard-block-enforcement.md) — complete
- [FEAT-014: Decision plan generation](../features/FEAT-014-decision-plan-generation.md) — complete

### SPRINT-04 (PHASE-02)
- [FEAT-015: Local REST API](../features/FEAT-015-local-rest-api.md) — complete
- [FEAT-016: OpenAI integration](../features/FEAT-016-openai-integration.md) — complete
- [FEAT-017: AI safety guardrails](../features/FEAT-017-ai-safety-guardrails.md) — complete
- [FEAT-018: Proposal system](../features/FEAT-018-proposal-system.md) — complete
- [FEAT-019: Proposal merge](../features/FEAT-019-proposal-merge.md) — complete

### SPRINT-05 (PHASE-03)
- [FEAT-020: Action queue builder](../features/FEAT-020-action-queue-builder.md) — planned
- [FEAT-021: Restore point creation](../features/FEAT-021-restore-point-creation.md) — planned
- [FEAT-022: Driver uninstall action](../features/FEAT-022-driver-uninstall-action.md) — planned
- [FEAT-023: Service disable action](../features/FEAT-023-service-disable-action.md) — planned
- [FEAT-024: Program uninstall action](../features/FEAT-024-program-uninstall-action.md) — planned
- [FEAT-025: Execution audit logging](../features/FEAT-025-execution-audit-logging.md) — planned

### SPRINT-06 (PHASE-03)
- [FEAT-026: UI shell and navigation](../features/FEAT-026-ui-shell-navigation.md) — planned
- [FEAT-027: Interview wizard](../features/FEAT-027-interview-wizard.md) — planned
- [FEAT-028: Inventory table view](../features/FEAT-028-inventory-table-view.md) — planned
- [FEAT-029: Item detail pane](../features/FEAT-029-item-detail-pane.md) — planned
- [FEAT-030: AI chat pane](../features/FEAT-030-ai-chat-pane.md) — planned
- [FEAT-031: Proposal review screen](../features/FEAT-031-proposal-review-screen.md) — planned
- [FEAT-032: Execute screen](../features/FEAT-032-execute-screen.md) — planned

### SPRINT-07 (PHASE-04)
- [FEAT-033: Rescan snapshot](../features/FEAT-033-rescan-snapshot.md) — planned
- [FEAT-034: Delta report](../features/FEAT-034-delta-report.md) — planned
- [FEAT-035: Eval harness CI](../features/FEAT-035-eval-harness-ci.md) — planned

### SPRINT-08 (PHASE-04)
- [FEAT-036: User guide](../features/FEAT-036-user-guide.md) — planned
- [FEAT-037: Developer guide](../features/FEAT-037-developer-guide.md) — planned

## Refactors

- [REFACTOR-001: Error handling hardening](../refactors/REFACTOR-001-error-handling-hardening.md) — planned (SPRINT-07)
- [REFACTOR-002: Logging and performance polish](../refactors/REFACTOR-002-logging-performance-polish.md) — planned (SPRINT-08)

## Bugs

- (none)

## Decision Log

- [DECISION_LOG](../decisions/DECISION_LOG.md)
- [ADR-0001: UI Framework — WPF](../decisions/ADR-0001-ui-framework-wpf.md) — complete
- [ADR-0002: SQLite Persistence](../decisions/ADR-0002-sqlite-persistence.md) — complete
- [ADR-0003: AI Tool-Exposure Matrix](../decisions/ADR-0003-ai-tool-exposure-matrix.md) — complete
- [ADR-0004: Immutable Snapshots](../decisions/ADR-0004-immutable-snapshots.md) — complete
- [ADR-0005: Deterministic Scoring](../decisions/ADR-0005-deterministic-scoring.md) — complete

## Design Documents

- [DESIGN.md](../design/DESIGN.md) — v1.0

## Operating Rules

- Work only on items listed in the active sprint
- No new work without an ID + file
- If docs conflict, MASTER_INDEX wins
