# How to remove the demo features (Counter, EventStream)

Every generated solution includes two small demo features in `web-spa`. They are teaching
material — each demonstrates a pattern you will reuse — and they are **not** template flags:
you remove them by deleting code once you no longer need the reference.

| Feature | Path (under `source/container-apps/web/web-spa/`) | Demonstrates |
|---------|---------------------------------------------------|--------------|
| Counter | `features/counter/` | TimeWarp.State action sets, JS-interop dispatch, `[StateAccess]` |
| EventStream | `features/event-stream/` | mediator pipeline middleware (`EventStreamBehavior`) |

## The fast way

Ask your coding agent:

> Remove the counter feature: delete `web-spa/features/counter/`, then fix every compile error
> that removal causes (nav links, global usings, `_Imports`, tests) until `dev build` is 0/0.

The compiler is the checklist — deleting the folder surfaces every referencing site as an error,
and the repo's analyzers (TWPA rules) catch the conventions on the way back to green.

## The manual checklist

Deleting a demo touches, at minimum:

1. The feature folder itself (state, pages, components, notification handlers).
2. `components/NavMenu.razor` — its nav link.
3. `global-usings.cs` / `_Imports.razor` — its namespace usings.
4. `tests/container-apps/web/web-spa-integration-tests/` — its test folder, plus any shared
   pipeline tests that exercise its state (e.g. `CloneStateBehavior_Tests` uses `CounterState`).
5. Anything the Style Guide page borrowed from it (the counter's throw-exception demo backs the
   "exception → toast" button).

Build after each step; stop when `dev build` reports 0 warnings / 0 errors.
