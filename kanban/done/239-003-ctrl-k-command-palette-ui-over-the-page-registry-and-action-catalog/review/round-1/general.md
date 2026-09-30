# Round 1 — general
**Date:** 2026-09-30
**Scope reviewed:** `git diff 7ee73b48...HEAD` (task 239-003): `TimeWarpPage.razor`, `CommandPalette.razor(.css)`, `command-palette-state/*`, `command-palette/*` (ranker, roster, row, runner), `services/command-palette-js-module.cs`, `source/features/command-palette.ts`, `tests/.../command-palette-tests.cs`, checked against task.md Requirements/Results and AGENTS.md. Context read: `ModalController`, `ModalContainer`, `ApplicationState` modal actions, `TimeWarpStateComponent.Dispose` (TimeWarp.State 12.0.0-beta.6), `[Page]` generator policy default, `NavMenu`.

## Summary

The core is sound. The roster and permission filter are correct: every navigable page has a `Policy`, and an omitted one becomes `Policies.Anonymous`. Commands need authentication plus all of their permissions. The ranker is deterministic and matches the stated tiers, the state handlers guard indexes and the empty roster, and no Design region is stale. The real defects are in the JS/focus lifecycle. Pressing Ctrl-K while the palette is open loses the focus-return target. A registration still in flight when the page is disposed can leak the global keydown listener. There are also smaller keyboard/a11y gaps: no scroll-into-view, keyboard dead after clicking inside the panel, a possibly stale highlight on fast Enter, and an unnamed dialog.

## Issues

### Issue 1 — Severity: bug
- File: source/container-apps/web/projects/web-spa/source/features/command-palette.ts:30
- Description: `open()` always overwrites `returnFocusTo` with `document.activeElement`. With the palette already open and its input focused, a second Ctrl-K / Cmd-K keydown (a common double-press, or a user re-pressing the hotkey) records the palette's own `<input>` as the return target. `OpenCommandPalette` then takes the `IsActive` branch (it only refocuses). On close the input has been removed, so `RestoreFocus` returns at `!target.isConnected` and focus drops to `<body>`. That breaks the "focus returns to the trigger" requirement. Not covered by tests (the JS is not exercised).
- Suggestion: Only capture when nothing is recorded yet, or when the active element is outside the palette. For example, `if (returnFocusTo === null) { ... }`, or skip elements inside `[data-qa="CommandPalette"]` / `.twe-modal`. RestoreFocus and Dispose already clear it, so "capture once per open" is safe.
- Status: open

### Issue 2 — Severity: bug
- File: source/container-apps/web/projects/web-spa/features/application/modals/command-palette/CommandPalette.razor:131
- Description: Registration is awaited in `OnAfterRenderAsync(firstRender)` using the component's `CancellationToken`, which `TimeWarpStateComponent.Dispose` cancels. If the page is disposed while the dynamic `import` or `Register` is still pending, `Dispose` runs while `Handle` is still null. This can happen when the user navigates quickly after the first load, since the first import is a network fetch. Two outcomes follow:
  - The JS `Register` still runs, but the .NET await either throws `TaskCanceledException` out of `OnAfterRenderAsync` of a disposed component, or resumes and assigns `Handle` after `Dispose`.
  - Either way nobody ever calls the JS `Dispose`. A document `keydown` listener is leaked: it `preventDefault`s Ctrl-K and calls `invokeMethodAsync` on an already-disposed `DotNetObjectReference` (rejected promise), alongside the next page's listener.

  Every navigation re-creates `TimeWarpPage`, so this lifecycle runs on each page change.
- Suggestion: After the await, check a `Disposed` flag. If set, dispose the handle you just got (`CommandPaletteJsModule.DisposeAsync(handle)`) and return. Also catch `OperationCanceledException` / `JSDisconnectedException` around the register call. Optionally `.catch(() => {})` the `invokeMethodAsync` promise in the TS so that a keypress in the window between `HostReference.Dispose()` and the fire-and-forget JS `Dispose` does not leave an unhandled rejection.
- Status: open

### Issue 3 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/application/modals/command-palette/CommandPalette.razor:194
- Description: Up/Down change `HighlightedIndex` but nothing scrolls the highlighted `<li>` into view. The list is capped at `max-height: min(60vh, 420px)`. A principal with every permission gets about 15 pages plus 3 commands at roughly 50px per row, so the list overflows. Arrowing past the visible area, or wrapping from the top to the last row, leaves the active row off-screen, and `aria-activedescendant` does not scroll it.
- Suggestion: After render, when the highlight changes, call a small TS export such as `element.scrollIntoView({ block: "nearest" })` on the active row id, or have the handle expose `ScrollActiveIntoView(id)`.
- Status: open

### Issue 4 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/application/modals/command-palette/CommandPalette.razor:87
- Description: Esc, Up/Down and Enter are handled only by `@onkeydown` on the `<input>`. A mouse click anywhere in the panel that is not a row moves focus to `<body>`: the hint text, the gaps, the empty-state text, or the list scrollbar gutter. After that the palette stays open but Esc no longer closes it and the arrows do nothing, which only a backdrop click recovers. The dialog has no focus trap either (`aria-modal="true"`), so Tab from the input walks into the page behind the overlay.
- Suggestion: Refocus the input on panel `mousedown` (for example `@onmousedown:preventDefault` on the non-input parts), or handle Escape in the TS document `keydown` while the palette is open. Consider keeping Tab inside the panel.
- Status: open

### Issue 5 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/application/modals/command-palette/CommandPalette.razor:97
- Description: Enter runs `CommandPaletteState.Highlighted`, which reflects the last Filter dispatch that completed, not necessarily the current `QueryText`. Filter goes through the full mediator pipeline (clone, behaviors, render subscriptions), and Blazor does not serialize event handlers across awaits. So if the pipeline yields, typing fast and pressing Enter can run the row highlighted for the previous query. For example, "cou" highlights Counter, then "coux" (no match) and an immediate Enter still navigates to Counter. That is exactly the near-miss auto-run the requirements forbid. This is plausible rather than proven; it depends on whether the pipeline yields.
- Suggestion: In the Enter case, re-sync first: `if (CommandPaletteState.Query != QueryText) await FilterAsync();`, then read `Highlighted`.
- Status: open

### Issue 6 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/application/modals/command-palette/CommandPalette.razor:172
- Description: ARIA wiring has a few loose ends:
  - `aria-controls="twe-palette-list"` and `aria-expanded="true"` are constant, but the `<ul id="twe-palette-list">` is not rendered when there are no matches, so the reference dangles.
  - The dialog wrapper (`ModalContainer`) uses `aria-labelledby="modal-title"`, and no element in the repo has that id, so the palette dialog has no accessible name. This is inherited from `ModalContainer`, but the palette is the first modal a keyboard or screen-reader user will hit constantly.
  - Up/Down in a text input also move the caret to the start or end, because they are not `preventDefault`-ed.
- Suggestion: Always render the `<ul>` (empty is fine) or bind `aria-expanded` to `Matches.Count > 0`. Give the palette panel an accessible name, for example a visually hidden heading with `id="modal-title"`, or let `ModalContainer` take a label parameter. Suppress caret movement for ArrowUp/ArrowDown on the input, from the TS or with a conditional `preventDefault`.
- Status: open

### Issue 7 — Severity: nit
- File: tests/container-apps/web/web-spa-integration-tests/features/application/command-palette-tests.cs:6
- Description: The component's keyboard dispatch is untested: `OnKeyDownAsync` (Escape → CloseModal, Enter with `Highlighted == null` is a no-op, mapping to MoveHighlight) and the `RunAsync` sequencing (CloseModal before the runner). It is covered only at the state/runner seam. The JS focus/listener lifecycle, where Issues 1 and 2 live, is untested and is recorded as a manual browser check that was not performed.
- Suggestion: Acceptable for this task given no bUnit, but record the double-Ctrl-K focus-return case and navigation re-registration in the manual check list (task Results → "How to validate") so the eventual browser pass covers them.
- Status: open
