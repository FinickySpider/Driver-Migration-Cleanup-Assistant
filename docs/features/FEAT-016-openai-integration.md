---
id: FEAT-016
type: feature
status: planned
priority: high
phase: PHASE-02
sprint: SPRINT-04
owner: ""
depends_on: [FEAT-015]
---

# FEAT-016: OpenAI Client and Tool-Calling Integration

## Description

Implement the OpenAI client that sends chat completions with tool definitions matching `openai_tools.json`. The client handles the tool-calling loop: send message → receive tool calls → execute tools locally → return results → repeat until final response.

## Acceptance Criteria

- [ ] OpenAI client sends chat completion requests with 7 tool definitions
- [ ] Tool-calling loop handles multiple rounds of tool calls
- [ ] Tool call results are dispatched to the correct local API handler
- [ ] API key is loaded from secure configuration (environment variable or user secrets)
- [ ] API key is never logged
- [ ] Client handles rate limiting and transient errors with retry
- [ ] Conversation history is maintained for multi-turn sessions

## Files Touched

| File | Change |
|------|--------|
| `src/Dmca.Core/AI/OpenAIClient.cs` | OpenAI client |
| `src/Dmca.Core/AI/ToolDispatcher.cs` | Tool call routing |
| `src/Dmca.Core/AI/ConversationManager.cs` | Chat history |

## Testing

- [ ] Mock OpenAI responses processed correctly
- [ ] Tool dispatch routes to correct handlers
- [ ] API key not present in logs

## Done When

- [ ] Acceptance criteria met
- [ ] Verified with mock and live OpenAI
