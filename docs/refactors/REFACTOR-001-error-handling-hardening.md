---
id: REFACTOR-001
type: refactor
status: planned
risk: medium
phase: PHASE-04
sprint: SPRINT-07
owner: ""
---

# REFACTOR-001: Error Handling Hardening

## Purpose

Harden error handling across all subsystems: collectors, scoring engine, AI client, execution engine, and API server. Ensure graceful degradation, meaningful error messages, and no unhandled exceptions reaching the user.

## Scope

### In Scope

- Collector error handling (WMI failures, access denied, timeout)
- AI client error handling (rate limits, network errors, malformed responses)
- Execution engine error handling (pnputil failures, service access denied, uninstaller hangs)
- API server error handling (invalid input, missing resources, internal errors)
- Global exception handler with user-friendly messages
- Retry policies for transient errors (AI client, WMI)

### Out of Scope

- UX changes beyond error message display
- Feature additions
- Performance optimization

## Plan

- Audit all try/catch blocks for swallowed exceptions
- Add specific exception types for each subsystem
- Implement retry policies (Polly or equivalent) for transient errors
- Add global exception handler in UI layer
- Test each error path with simulated failures

## Validation

- [ ] No unhandled exceptions in any tested scenario
- [ ] Eval scenario S10 (tool failures) passes
- [ ] All error messages are actionable (not stack traces)

## Done When

- [ ] Validation complete
