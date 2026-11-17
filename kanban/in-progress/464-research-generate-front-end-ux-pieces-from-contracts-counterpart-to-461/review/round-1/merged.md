# Round 1 — merged findings
**Date:** 2026-11-05
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 2 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: kanban/in-progress/464-research-generate-front-end-ux-pieces-from-contracts-counterpart-to-461/proposal.md:368-374
- Description: Option D has generated code set `Failed` in `HandleError`. But 19 handlers override
  `HandleError` by hand, and section 6 keeps those overrides hand-written. A generator cannot emit
  into a declared override, so `Failed` becomes agreement by memory for 19 of 41 handlers. The A-only
  check covers only `HandleSuccess`.
- Suggestion: State where `Failed` is set when `HandleError` is hand-written: a base-class platform
  change, or a generated wrapper plus a hook. Otherwise extend the A-only check to `HandleError`.
  Update D's scope line to match.
- Source: general
- Disposition notes: fixed in fix pass 1. Option D adds an empty `protected virtual void OnProblem(SharedProblemDetails)` on `ApiHandler`, called before `HandleError`; generated code overrides it to set `Failed`. The A-only check now covers `GetRequest`, `HandleSuccess`, and `OnProblem`. The scope line names the base change.

### M2 — Severity: suggestion — Status: fixed
- File: kanban/in-progress/464-research-generate-front-end-ux-pieces-from-contracts-counterpart-to-461/proposal.md:276-285
- Description: Option B makes `OnRolesLoaded` required (4f), but it does not say when the generator
  emits a required `On…Loaded` declaration. There are two possible rules:
  - Required on every fetch: this costs an empty body on every fetch that seeds nothing.
  - Opt-in: forgetting the opt-in silently brings back the 4f data loss.
- Suggestion: State the rule in B and add its cost to B's cost list. If the rule is opt-in, say that
  4f then rests on the section 7 test.
- Source: general
- Disposition notes: fixed in fix pass 1. B states the rule it assumes (required on every generated fetch, up to 16 empty bodies SPA-wide) and adds that cost to B's list. The opt-in alternative is stated, with 4f then resting on the section 7 test.

### M3 — Severity: nit — Status: fixed
- File: kanban/in-progress/464-research-generate-front-end-ux-pieces-from-contracts-counterpart-to-461/proposal.md:43
- Description: "6 of 41 pass `action.Command` through" does not reproduce. The grep finds 5
  `DefaultApiHandler` files. The sixth hit is chat-state, which is not a `DefaultApiHandler`.
  CreateRole copies fields rather than passing the Command through. B's cost line and `task.md`
  Results repeat the count.
- Suggestion: Correct the count, or cite the grep, and keep Results consistent.
- Source: general
- Disposition notes: fixed in fix pass 1. Section 2 and B's cost line say 5 of 41, name the four pass-through handlers plus CreateRole's copy, and cite the grep and its sixth non-handler hit. `task.md` Results matches.

### M4 — Severity: nit — Status: fixed
- File: kanban/in-progress/464-research-generate-front-end-ux-pieces-from-contracts-counterpart-to-461/task.md:6,17,93-101
- Description: Several parts of `task.md` are stale:
  - The Checklist still says "Split implementation tasks (coordinate with 461)", which contradicts
    the 461 outcome and the restatement.
  - The Description and the Checklist point at the superseded draft.
  - The 461 path on line 6 is `kanban/to-do/…`; 461 is in `kanban/in-progress/`.
- Suggestion: Point both at `proposal.md`. Replace the split item with "Steve answers section 7".
  Fix the path.
- Source: general
- Disposition notes: fixed in fix pass 1. Description and Checklist point at `proposal.md`, the split item is now "Steve answers the question in section 7", and the 461 path is `kanban/in-progress/`.

## Duplicates / conflicts

- None (single reviewer).
