# Round 1 — general
**Date:** 2026-10-05
**Scope reviewed:** commit facb975d (Directory.Packages.props, web-spa program.cs, counter-state.increment-counter.cs, counter.ts, time-warp-state.d.ts, tw-blazor SKILL.md, SPA test hosts, counter-js-dispatch-tests.cs)

## Summary

Both `TimeWarp.State.*` pins move forward to 12.0.0-beta.8. A single public allow-list,
`Program.AllowJavaScriptDispatch`, is registered in production and reused by both SPA test hosts,
so the list exists in one place. The `Counter.Increment` alias is a const on the ActionSet. The
TS literal is checked against that const by the existing source-reading test. The new test feeds
in three rejected names (a non-action type, an unknown alias, and a real action that is not
allowed) and checks the count stays at 3. Design regions are reconciled. Risk is low.

Verified independently: `dotnet test -c Release -- --filter-class JsDispatch` in
web-spa-integration-tests → 5/5 passed. `git grep DispatchRequest` finds no other JS dispatcher.
No `12.0.0-beta.7` pins remain. The one beta.7 mention left, in program.cs, is the
`AddActionCatalog` comment, which correctly dates when that feature arrived.

## Issues

None.
