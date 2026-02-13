---
id: REFACTOR-001
type: refactor
status: complete
risk: medium
phase: PHASE-04
sprint: SPRINT-07
owner: ""
completed: 2025-07-19
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

## Implementation

- Created `DmcaExceptions.cs` with exception hierarchy:
  - `DmcaException` (base) → `CollectorException`, `AiClientException`, `ExecutionActionException`, `ApiValidationException`, `SessionStateException`
- Created `RetryHelper.cs` with exponential backoff (generic + void overloads)
- Hardened `ScanService.ScanAsync` with try-catch per collector (graceful degradation)
- Hardened `RescanService.RescanAsync` with same pattern
- Updated `SessionStateMachine.ValidateTransition` to throw `SessionStateException`
- Added global WPF exception handlers in `App.xaml.cs` (Dispatcher, AppDomain, TaskScheduler)
- Pattern-matched `FormatExceptionMessage` for user-friendly messages per exception type

## Validation

- [x] No unhandled exceptions in any tested scenario
- [x] Exception hierarchy tested (21 tests in ErrorHandlingTests)
- [x] All error messages are actionable (not stack traces)

## Done When

- [x] Validation complete
