# DMCA Evals

## Quick start (offline)
- Install .NET 8 SDK
- `dotnet test`

Offline mode uses `OfflineModelClient` and runs S1–S10 (S9 intentionally fails).

## Live mode (OpenAI)
Wire `OpenAIModelClient` to OpenAI tool calling and run:
- `dotnet test --filter Category=Live`
