---
id: FEAT-017
type: feature
status: complete
priority: high
phase: PHASE-02
sprint: SPRINT-04
owner: ""
depends_on: [FEAT-016]
completed: 2026-02-12
---

# FEAT-017: AI System Prompt and Safety Guardrails

## Description

Implement the AI system prompt that constrains the AI Advisor's behavior. Includes safety rules, tool usage rules, proposal rules, and forbidden-phrase detection on AI output.

## Acceptance Criteria

- [x] System prompt loaded from `ai_tool_policy_prompt.txt`
- [x] Forbidden-phrase detection scans all AI assistant text responses
- [x] Detected phrases: "auto-approve", "approve automatically", "executing now", "i will execute", "i executed", "i'm going to run the uninstall"
- [x] Detection triggers a warning/flag (not a crash)
- [x] AI delta score clamping enforced: ±25 default, ±40 only when user-fact confirms
- [x] Max 5 changes per proposal enforced at API level
- [x] Eval scenario S7 (prompt injection) passes

## Files Touched

| File | Change |
|------|--------|
| `src/Dmca.Core/AI/SystemPrompt.cs` | Prompt builder |
| `src/Dmca.Core/AI/SafetyValidator.cs` | Output validation |
| `src/Dmca.Core/Policy/AIDeltaClamp.cs` | Score delta enforcement |

## Testing

- [x] Forbidden phrases detected in test strings
- [x] Delta clamping enforced at boundaries
- [x] Prompt injection scenario handled safely

## Done When

- [x] Acceptance criteria met
- [x] Verified with eval fixtures

## Completion Notes

Implemented safety guardrails: forbidden-phrase detection scanning 6 phrases in AI output (triggers warning flag, not crash), AI delta clamping (±25 default, ±40 with user-fact confirmation), max 5 changes per proposal enforced at validation level, and allowed-tool enforcement preventing AI access to approve/execute/rescan. All tests passing.
