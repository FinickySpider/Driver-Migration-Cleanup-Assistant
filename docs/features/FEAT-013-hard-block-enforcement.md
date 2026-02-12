---
id: FEAT-013
type: feature
status: planned
priority: high
phase: PHASE-02
sprint: SPRINT-03
owner: ""
depends_on: [FEAT-011]
---

# FEAT-013: Hard-Block Enforcement

## Description

Implement hard-block evaluation that prevents removal of protected items. Hard blocks are non-overridable in v1 and override any score-based recommendation to `BLOCKED`.

## Acceptance Criteria

- [ ] Hard-block conditions from `rules.yml` are evaluated per item
- [ ] `MICROSOFT_INBOX`: items with `isMicrosoft = true` or signer contains "Microsoft Windows"
- [ ] `BOOT_CRITICAL`: items with `bootCriticalInUse = true`
- [ ] `PRESENT_HARDWARE_BINDING`: present drivers/services are blocked by default
- [ ] `POLICY_PROTECTED`: user-pinned items blocked
- [ ] `DEPENDENCY_REQUIRED`: items required by non-removable items blocked
- [ ] Any hard block sets recommendation to `BLOCKED` regardless of score
- [ ] Hard blocks are returned by `get_hardblocks` API
- [ ] Eval fixtures S2 (Microsoft inbox) and S3 (boot-critical) pass

## Files Touched

| File | Change |
|------|--------|
| `src/Dmca.Core/Scoring/HardBlockEvaluator.cs` | Hard-block logic |
| `src/Dmca.Core/Models/HardBlock.cs` | Hard-block model |

## Testing

- [ ] Microsoft-signed item is blocked
- [ ] Boot-critical item is blocked
- [ ] Present hardware is blocked
- [ ] Non-blocked item is not falsely blocked

## Done When

- [ ] Acceptance criteria met
- [ ] Verified with eval fixtures
