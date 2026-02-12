---
id: FEAT-030
type: feature
status: planned
priority: high
phase: PHASE-03
sprint: SPRINT-06
owner: ""
depends_on: [FEAT-026, FEAT-016]
---

# FEAT-030: AI Chat Pane

## Description

Implement the AI chat pane where users can interact with the AI Advisor. Shows conversation history, user input, and AI responses including tool-call activity indicators.

## Acceptance Criteria

- [ ] Chat pane shows conversation history with user/AI message bubbles
- [ ] User can type messages and send to AI
- [ ] Tool-call activity shown as subtle indicators (e.g., "Reading inventory...")
- [ ] AI proposals mentioned in chat are linked to proposal review screen
- [ ] Loading indicator during AI response generation
- [ ] Chat history persisted per session

## Files Touched

| File | Change |
|------|--------|
| `src/Dmca.App/Views/AIChatPane.xaml` | Chat UI |
| `src/Dmca.App/ViewModels/AIChatViewModel.cs` | Chat logic |

## Testing

- [ ] Messages sent and received correctly
- [ ] Tool activity indicators appear during tool calls
- [ ] Chat history survives screen navigation

## Done When

- [ ] Acceptance criteria met
- [ ] Verified manually
