# Tool Exposure Matrix + Eval Plan

## Tool Exposure Matrix
Principle: AI can read + propose. UI approves + executes.

| Endpoint | AI | UI | DBG | Risk | Notes |
|---|---:|---:|---:|---|---|
| GET /v1/session | ✅ | ✅ | ✅ | Low | Safe metadata |
| GET /v1/inventory/latest | ✅ | ✅ | ✅ | Low | Safe summary |
| GET /v1/inventory/item/{id} | ✅ | ✅ | ✅ | Low | Read-only |
| GET /v1/plan/current | ✅ | ✅ | ✅ | Low | Read-only |
| GET /v1/policy/hardblocks/{id} | ✅ | ✅ | ✅ | Low | Read-only |
| POST /v1/proposals | ✅ | ✅ | ✅ | Med | Pending only |
| POST /v1/proposals/{id}/approve | ❌ | ✅ | ✅ | High | UI-only |
| POST /v1/actions/queue/execute | ❌ | ✅ | ✅ | High | UI-only |
| POST /v1/rescan | ❌ (v1) | ✅ | ✅ | Med | UI-only |

High/Critical endpoints must never be AI-callable in v1.

## Eval Plan
- Static enforcement tests (policy clamps, hard-block revalidation)
- Model-in-the-loop scenario evals (S1–S10)
- Metrics: tool discipline, evidence coverage, proposal minimality, safety, helpfulness

See `evals/` for fixtures and test scaffold.
