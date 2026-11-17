# Round 1 — general
**Date:** 2026-11-05
**Scope reviewed:** same as framework (kitchen-only diff vs master)

## Summary

The change adds `proposal.md`, a research proposal for which Roles SPA pieces could be generated
from contracts. It has an inventory, the facts that limit the options (4a–4g), options A–D as code,
and Steve's question at the end. No option is picked. The restatement in `task.md` matches the
original task, so there is no misread. The proposal meets the acceptance criteria. I re-ran its
falsifiable claims and they hold:

- 0 `[Destructive]` uses
- 42 files reference `DefaultApiHandler<`
- no `UpdateRole`/`DeleteRole` use in the SPA
- the Debug fallback at `mock-web-api-service.cs:61-64`
- 2 `ConfirmAsync` overrides
- 19 `HandleError` overrides
- 9 `Last…Succeeded`/`Last…Id` members
- the `[Operation]` "Create a role." vs `[CatalogAction]` "Create a new authorization role." mismatch
- `endpoint-metadata.cs:75`, `contracts-emitter.cs:49`, `program.cs:189`
- package pins `TimeWarp.Contracts` 2.0.0-beta.31 and `TimeWarp.State` 12.0.0-beta.16
- `GetRole.Response : IRoleDetails`
- the Administrator 409 guard
- no test references `DraftPermissionIds`, including co-located tests under `source/`

The open items are two design gaps in options Steve is asked to compare, plus two small accuracy and
staleness nits.

## Issues

### Issue 1 — Severity: suggestion
- File: kanban/in-progress/464-research-generate-front-end-ux-pieces-from-contracts-counterpart-to-461/proposal.md:368-374
- Description: Option D says that with B or C, generated code sets `Failed` in `HandleError`. But
  section 2 counts 19 handlers that override `HandleError` by hand, and section 6 keeps those
  overrides hand-written in every option. A generator cannot emit `HandleError` into a partial
  `Handler` that already declares it, and a hand-written override does not set the slot unless
  someone remembers to. For 19 of 41 handlers, `Failed` is then agreement by memory, which is the
  same failure mode as the 4g flag reset this option is meant to remove. Under "A only", the
  proposed check covers only `HandleSuccess`.
- Suggestion: Say where `Failed` is set when `HandleError` is hand-written. One choice is
  `DefaultApiHandler` or `ApiHandler` setting the slot before it calls the virtual, which is a
  platform change. Another is a generated non-virtual wrapper plus a hook. If neither is offered,
  extend the A-only check to `HandleError`. Then D's cost line ("3 members in Roles and 9
  SPA-wide") reflects the error path too.
- Status: open

### Issue 2 — Severity: suggestion
- File: kanban/in-progress/464-research-generate-front-end-ux-pieces-from-contracts-counterpart-to-461/proposal.md:276-285
- Description: 4f makes the FetchRoles seed hook required, and B shows
  `private partial void OnRolesLoaded(...)`. B does not say when the generator emits that required
  declaration. The generator cannot tell from the contract that FetchRoles seeds local drafts. So
  either (a) every generated fetch gets a required `On…Loaded` hook, and every fetch with nothing to
  seed needs an empty body, or (b) the hook is opt-in through an attribute flag, and then forgetting
  the flag brings back the 4f data-loss case silently. Steve cannot weigh B's safety or cost without
  knowing which.
- Suggestion: State the rule in B, for example "required on every generated query handler". Add its
  cost to B's cost list (empty bodies on fetches that seed nothing). If (b) is the rule, say that 4f
  is then covered only by the test named in section 7.
- Status: open

### Issue 3 — Severity: nit
- File: kanban/in-progress/464-research-generate-front-end-ux-pieces-from-contracts-counterpart-to-461/proposal.md:43
- Description: "6 of them pass `action.Command` through to the request." `git grep "action\.Command"`
  under `SPA/features` finds 5 `DefaultApiHandler` files: create-role, update-profile, and three
  site-settings files. The sixth match is `chat-state.server-to-client-message-action.cs`, which is
  not a `DefaultApiHandler`. CreateRole also copies fields into a new Command rather than passing
  the instance through, which is the shape Option B generates. The count feeds B's "only 6 of 41"
  cost line, and Results repeats it.
- Suggestion: Correct the count, or give the grep that yields 6, and keep `Results` consistent.
- Status: open

### Issue 4 — Severity: nit
- File: kanban/in-progress/464-research-generate-front-end-ux-pieces-from-contracts-counterpart-to-461/task.md:6,17,93-101
- Description: `task.md` still has the pre-461-outcome framing:
  - The Checklist ends with "Split implementation tasks (coordinate with 461)". That contradicts
    the 461 outcome note and the restatement ("do not split implementation tasks"; no back-end
    generator to coordinate with).
  - "Review the kitchen draft with Steven" and line 17 ("Kitchen draft:") point at the superseded
    `contracts-frontend-example.md` instead of `proposal.md`.
  - The 461 path on line 6 is `kanban/to-do/…`, but 461 is in `in-progress` (line 26 says so).
- Suggestion: Point the Description and Checklist at `proposal.md`. Replace the split item with
  "Steve answers section 7". Fix the 461 path.
- Status: open
