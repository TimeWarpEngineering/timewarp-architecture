# Map agent scopes to permission bundles; scheme-aware evaluator

**Parent:** 182 · **Order:** F (after 182-001–003) · **Depends on:** model + enforcement live

## Description

Agent scopes become permission bundles (parallel to human roles). Unify credential-management two-arm assertion where possible; keep scheme restrictions on admin; no agent token can hold `admin.*` via scope seed.

## Requirements

- Map `identity:read`, `credential:manage`, `demo:invoke` → permission sets in registry/seed.
- Evaluator: agent-token principal grants from scopes only (not human role expansion).
- Keep admin scheme restriction + agent 401 integration pin.
- Fix accidental Member role projection onto agents via claims transform if still present.

## Checklist

- [ ] Scope→permission seed map
- [ ] Scheme-aware evaluator behavior + tests
- [ ] Agent integration pins still green
- [ ] Results + How to validate
