# Round 1 — merged findings
**Date:** 2026-09-30
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 2 | 0 |
| suggestion | 0 | 3 | 1 |
| nit | 0 | 2 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/source/features/command-palette.ts:30
- Description: A second Ctrl-K while the palette is open records the palette's own input as the focus-return target; on close it is detached and focus drops to `<body>`.
- Suggestion: Capture the return target once per open.
- Source: general (Issue 1)
- Disposition notes: `open()` keeps an existing connected target (`returnFocusTo` is only replaced when null or detached); RestoreFocus/Dispose clear it.

### M2 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/modals/command-palette/CommandPalette.razor:131
- Description: Registration awaited after first render can complete after Dispose (fast navigation) → document keydown listener leaked; cancellation can throw out of OnAfterRenderAsync.
- Suggestion: Disposed check after await, dispose the late handle, catch cancellation/disconnect; `.catch` the JS → .NET call.
- Source: general (Issue 2)
- Disposition notes: `Disposed` flag; a handle that arrives after Dispose is disposed immediately; `OperationCanceledException` / `JSDisconnectedException` swallowed only when disposed; TS `invokeMethodAsync(...).catch(() => {})`.

### M3 — Severity: suggestion — Status: fixed
- File: CommandPalette.razor:194
- Description: Highlight changes do not scroll the active row into view.
- Suggestion: scrollIntoView({block: "nearest"}) on the active row after render.
- Source: general (Issue 3)
- Disposition notes: handle export `ScrollIntoView(id)`; called from OnAfterRenderAsync when the highlighted index changes while active.

### M4 — Severity: suggestion — Status: fixed
- File: CommandPalette.razor:87
- Description: Clicking inside the panel (list gutter/scrollbar, empty text, hint) moves focus off the input, leaving Esc/arrows dead.
- Suggestion: Keep focus on the input for non-input clicks.
- Source: general (Issue 4, first half)
- Disposition notes: `@onmousedown:preventDefault` on the list, empty-state text and hint so focus stays in the query box (row click still runs via `@onclick`).

### M5 — Severity: suggestion — Status: fixed
- File: CommandPalette.razor:97
- Description: Enter reads the highlight of the last completed Filter, which may lag the typed text → possible near-miss run.
- Suggestion: Re-filter when `CommandPaletteState.Query != QueryText` before reading Highlighted.
- Source: general (Issue 5)
- Disposition notes: implemented as suggested.

### M6 — Severity: nit — Status: fixed
- File: CommandPalette.razor:172
- Description: Dangling `aria-controls`, constant `aria-expanded`, unnamed dialog (`modal-title` absent), Up/Down move the caret.
- Suggestion: Bind expanded/controls to list presence; give the dialog a name; suppress caret movement.
- Source: general (Issue 6)
- Disposition notes: `aria-expanded`/`aria-controls` follow `Matches.Count > 0`; visually hidden `<h2 id="modal-title">` names the dialog (only the active modal renders, so the id is unique); the TS document keydown `preventDefault`s ArrowUp/ArrowDown on the `data-command-palette-input` element.

### M7 — Severity: nit — Status: fixed
- File: tests/container-apps/web/web-spa-integration-tests/features/application/command-palette-tests.cs:6
- Description: Keyboard dispatch and JS focus/listener lifecycle are untested (no bUnit; manual browser check not performed).
- Suggestion: Record double-Ctrl-K focus return and navigation re-registration in the manual check list.
- Source: general (Issue 7)
- Disposition notes: added to task.md Results → How to validate (manual browser checklist).

### M8 — Severity: suggestion — Status: wontfix
- File: source/container-apps/web/projects/web-spa/features/application/components/ModalContainer.razor:50
- Description: No focus trap in the `aria-modal` dialog (Tab reaches the page behind); clicks on the shared panel padding still move focus off the input.
- Suggestion: Trap Tab inside the panel.
- Source: general (Issue 4, second half)
- Disposition notes: wontfix on this id (orchestrator). Both belong to the shared `ModalContainer`, which every modal uses; a focus trap there is a cross-modal a11y change, not palette UI. Backdrop click and Esc-after-refocus recover. Candidate follow-up under parent 239 / a ModalContainer a11y task.

## Duplicates / conflicts

- Issue 4 split into M4 (palette-owned surfaces, fixed) and M8 (shared ModalContainer behavior, wontfix).
