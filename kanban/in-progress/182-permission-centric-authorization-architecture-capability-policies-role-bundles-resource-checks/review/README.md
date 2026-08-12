# Review: task 182 permission-centric authorization

## Purpose

Independent review of the proposed authorization architecture **before** Phase 1 implementation children are cut.

## Inputs

| File | Role |
|------|------|
| `../task.md` | Requirements, phases, constraints, done criteria |
| `../research/decision-brief.md` | Research synthesis and target model |
| Current code hotspots listed in task Notes | Reality check |

## Reviewer charge (Claude)

Produce `review/round-1/<reviewer>.md` (or this folder’s agreed name) that:

1. **Verdict:** Accept / Accept with amendments / Reject (with alternative).
2. **Architecture:** Does permission-centric + role bundles beat RequireRole and COPIC-modules-as-ceiling for a *template*? Challenge assumptions.
3. **Answer every Review question** in decision-brief §7 (or mark N/A with reason).
4. **Risks:** security (privilege escalation, last-admin, SPA trust), TWA0009, dual SPA/server evaluation drift, migration cost.
5. **Phase cuts:** Is Phase 1 the right first vertical slice? What must be child tasks?
6. **Non-goals:** Confirm or contest excluded items (mandatory OpenFGA, COPIC parity, sign-in backfill).

## Rules

- No calendar estimates.
- Prefer concrete amendments over vague “consider…”.
- If accepting, list **blocking** vs **non-blocking** amendments for disposition.

## After review

Author `../disposition.md` (fold-in on this host task—do not create a separate apply-review task). Then create children with:

```bash
ganda kanban create "…" --parent 182
```
