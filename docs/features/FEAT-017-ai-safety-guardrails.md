---
id: FEAT-017
type: feature
status: planned
priority: high
phase: PHASE-02
sprint: SPRINT-04
owner: ""
depends_on: [FEAT-016]
---

# FEAT-017: AI System Prompt and Safety Guardrails

## Description

Implement the AI system prompt that constrains the AI Advisor's behavior. Includes safety rules, tool usage rules, proposal rules, and forbidden-phrase detection on AI output.

## Acceptance Criteria

- [ ] System prompt loaded from `ai_tool_policy_prompt.txt`
- [ ] Forbidden-phrase detection scans all AI assistant text responses
- [ ] Detected phrases: "auto-approve", "approve automatically", "executing now", "i will execute", "i executed", "i'm going to run the uninstall"
- [ ] Detection triggers a warning/flag (not a crash)
- [ ] AI delta score clamping enforced: ±25 default, ±40 only when user-fact confirms
- [ ] Max 5 changes per proposal enforced at API level
- [ ] Eval scenario S7 (prompt injection) passes

## Files Touched

| File | Change |
|------|--------|
| `src/Dmca.Core/AI/SystemPrompt.cs` | Prompt builder |
| `src/Dmca.Core/AI/SafetyValidator.cs` | Output validation |
| `src/Dmca.Core/Policy/AIDeltaClamp.cs` | Score delta enforcement |

## Testing

- [ ] Forbidden phrases detected in test strings
- [ ] Delta clamping enforced at boundaries
- [ ] Prompt injection scenario handled safely

## Done When

- [ ] Acceptance criteria met
- [ ] Verified with eval fixtures
