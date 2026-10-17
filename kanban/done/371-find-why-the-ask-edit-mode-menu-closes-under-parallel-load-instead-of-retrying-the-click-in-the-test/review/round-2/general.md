# Round 2 — general
**Date:** 2026-10-17
**Scope reviewed:** fix delta `b1fe9f792..bffc9d4f6` (round-1 base to HEAD): `tooltip-hint.ts`, `web.spa.lib.module.ts`

## Summary

The fix pass removes the document `MutationObserver` (`watchTooltips` / `markHint`) and the
`queueMicrotask(wrapWhenDefined)` re-wrap. Only the `showPopover` prototype wrapper is left, and it is
installed from `customElements.whenDefined("fluent-tooltip").then(wrapShowPopover)`. `whenDefined`
resolves with the constructor, so passing `wrapShowPopover` directly is correct.

I checked the claims against the FluentUI 5.0.0 bundle
(`~/.nuget/packages/microsoft.fluentui.aspnetcore.components/5.0.0/staticwebassets/…lib.module.js`).
`showTooltip` shows through `this.showPopover()` from a `setTimeout`. FluentUI's override
(`w.prototype.showPopover=function(){this.isConnected&&h.call(this)}`) also sits on the prototype and
calls the function it captured. Whichever override is installed second wraps the first, so either order
sets `hint` before the native show runs. The new Design text says this, and it is accurate. The only
`togglePopover` call in the bundle is on a listbox, not a tooltip, so no tooltip show bypasses the
wrapper. The lib-module Design note was updated to match. Prior M1 and M2 are fixed.

Gates I re-ran: `./bin/dev build --quiet` succeeded. `AskSurface_Given_Wasm` passed 1/1 (Release,
`--no-build --no-restore`). The compiled `wwwroot/js/features/tooltip-hint.js` has no `watchTooltips`,
so the test ran against the fixed script. I did not re-run the 50× load loop. The fix delta only
removed redundant paths, and the show path the load proof exercised is unchanged.

The fix-pass restatement in `task.md` matches the round-1 findings and the original task. No misread.

## Issues

<!-- None. -->
