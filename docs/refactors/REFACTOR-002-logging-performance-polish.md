---
id: REFACTOR-002
type: refactor
status: planned
risk: low
phase: PHASE-04
sprint: SPRINT-08
owner: ""
---

# REFACTOR-002: Logging and Performance Polish

## Purpose

Improve logging consistency across all subsystems and profile performance bottlenecks. Ensure structured logging with appropriate levels, and optimize slow operations (large inventory scans, AI round-trips).

## Scope

### In Scope

- Structured logging with consistent format (timestamp, level, component, message)
- Log levels: Debug (verbose), Info (operations), Warning (recoverable), Error (failures)
- Performance profiling of inventory scan (target: <30s for typical system)
- Performance profiling of AI round-trip (target: <10s per tool-call round)
- SQLite query optimization for large inventories
- Log file rotation and size limits

### Out of Scope

- UX changes
- Feature additions
- Telemetry or analytics

## Plan

- Standardize all log calls to use structured logging (Serilog or Microsoft.Extensions.Logging)
- Add timing instrumentation to collectors and AI client
- Profile SQLite queries with large datasets
- Implement log file rotation

## Validation

- [ ] All log entries follow structured format
- [ ] No excessive logging (Debug level hidden by default)
- [ ] Scan completes in <30s on typical system
- [ ] Log files don't grow unbounded

## Done When

- [ ] Validation complete
