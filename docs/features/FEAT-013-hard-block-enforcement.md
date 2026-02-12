---
id: FEAT-013
type: feature
status: complete
priority: high
phase: PHASE-02
sprint: SPRINT-03
owner: ""
depends_on: [FEAT-011]
completed: 2026-02-12
---

# FEAT-013: Hard-Block Enforcement

## Description

Implement hard-block evaluation that prevents removal of protected items. Hard blocks are non-overridable in v1 and override any score-based recommendation to `BLOCKED`.

## Acceptance Criteria

- [x] Hard-block conditions from `rules.yml` are evaluated per item
- [x] `MICROSOFT_INBOX`: items with `isMicrosoft = true` or signer contains "Microsoft Windows"
- [x] `BOOT_CRITICAL`: items with `bootCriticalInUse = true`
- [x] `PRESENT_HARDWARE_BINDING`: present drivers/services are blocked by default
- [x] `POLICY_PROTECTED`: user-pinned items blocked
- [x] `DEPENDENCY_REQUIRED`: items required by non-removable items blocked
- [x] Any hard block sets recommendation to `BLOCKED` regardless of score
- [x] Hard blocks are returned by `get_hardblocks` API
- [x] Eval fixtures S2 (Microsoft inbox) and S3 (boot-critical) pass

## Files Touched

| File | Change |
|------|--------|
| `src/Dmca.Core/Scoring/HardBlockEvaluator.cs` | Hard-block logic |
| `src/Dmca.Core/Models/HardBlock.cs` | Hard-block model |

## Testing

- [x] Microsoft-signed item is blocked
- [x] Boot-critical item is blocked
- [x] Present hardware is blocked
- [x] Non-blocked item is not falsely blocked

## Done When

- [x] Acceptance criteria met
- [x] Verified with eval fixtures

## Completion Notes

Implemented `HardBlockEvaluator` covering all 5 hard-block types (`MICROSOFT_INBOX`, `BOOT_CRITICAL`, `PRESENT_HARDWARE_BINDING`, `POLICY_PROTECTED`, `DEPENDENCY_REQUIRED`). Hard blocks override recommendation to `BLOCKED` regardless of score. Eval fixtures S2 and S3 pass. All tests passing.
