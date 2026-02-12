---
id: ADR-0005
type: decision
status: complete
date: 2026-02-12
supersedes: ""
superseded_by: ""
---

# ADR-0005: Deterministic Scoring with Configurable Rules

## Context

We need a scoring mechanism that assigns a removal confidence score (0–100) to each inventory item. Options: hard-coded logic, ML-based scoring, or rule-engine with configurable weights. The score must be transparent, auditable, and reproducible.

## Decision

Use a **deterministic rule engine** driven by `rules.yml`:

- Signals with explicit weights are evaluated against item properties
- Conditions use a simple operator language (`eq`, `contains_i`, `matches_keywords`, `missing_or_empty`)
- Keyword sets are maintained as named lists
- Hard blocks are evaluated separately and override scores
- Score is clamped to 0–100 after signal summation
- AI delta is a bounded adjustment (±25, or ±40 with user-fact confirmation)

Scoring is fully deterministic: same input always produces same output. The AI can only propose deltas, not override the engine.

## Consequences

### Positive

- Transparent and auditable scoring
- Reproducible: same inventory → same scores
- Configurable without code changes (edit rules.yml)
- AI augments but cannot subvert the engine
- Eval fixtures can assert exact scores

### Negative

- Cannot learn from user feedback automatically
- New signal patterns require manual rules.yml updates
- Complex conditions may be harder to express in YAML

## Links

- Related items:
  - FEAT-011
  - FEAT-012
  - FEAT-013
