# Ctrl-K command palette UI over the page registry and action catalog

## Description

Child of 239. Build the palette in web-spa using the page registry (239-001) and the action catalog
(239-002). Requirements, keyboard behaviour and exclusions are the parent's (read
`kanban/*/239-implement-ctrl-k-command-palette-github-issue-102/task.md`); this child owns the UI.

## Requirements

- Ctrl-K and Cmd-K open an overlay from `TimeWarpPage` (interactive shell only; not
  `TimeWarpFocusedPage`). A small TS module registers the global keydown (none exists today);
  the appbar `FluentTextInput` (`components/TimeWarpPage.razor`, `.twe-appbar__search`) opens the
  same overlay on focus/click.
- Overlay shell: reuse the store-driven modal pattern (`ApplicationState.ActiveModalId`,
  `ModalController`/`ModalContainer`) or FluentUI 5 `FluentDialog` — pick one, record why.
- Rows: pages from the registry (navigate) + catalog entries with `Visibility` Human/Both and no
  required parameters (execute via `ActionCatalogEntry.Execute`). One row shape: name, description,
  kind.
- Ranking: deterministic C# fuzzy filter over name + description (prefix > word-start > substring),
  no Jev/LLM. No auto-run on a near-miss; Enter runs the highlighted row only.
- Keyboard: type to filter, Up/Down, Enter, Esc; click-outside closes; focus returns to the trigger.
- Permissions: filter rows with `IAuthorizationService.AuthorizeAsync` (page `Policy`, entry
  `Permissions`) — the same policies NavMenu uses; never show a row the principal cannot run.
- Outcomes of executed commands go through the shell notification region (TWA0025), not a
  page-local bar.
- Tests: open/close, filter ordering, Enter navigates, Enter executes a parameterless command,
  permission-filtered rows absent, excluded internals absent. Mobile-viewport layout noted.
- Close GitHub timewarp-architecture#102 from Results with a link to the PR.
- Gates: `dev build` 0/0, `dev test`, `dev template-smoke`.
- **Do not start an AppHost** (`dev run`, `aspire run`, `dotnet run` of aspire-app-host) — task worktrees share the master user-secrets id. Record the manual browser check as not performed.

## Checklist

- [x] Hotkey + search-field trigger + overlay
- [x] Rows from registry + catalog; ranking; keyboard
- [x] Permission filter
- [x] Tests
- [ ] GitHub #102 closed from Results (auto-closes on merge: the PR body must carry `Closes #102`)
- [x] Gates; no AppHost

## Depends on

- 239-001
- 239-002

## Results

**Closes #102** (GitHub timewarp-architecture#102, "Ctrl-K Search capabilities"). The issue is
still open on purpose: the palette ships when the PR merges, and the PR body must carry
`Closes #102` so GitHub closes it then (the host `open-pr` node opens the PR; no PR exists yet
to link from here).

- **Overlay shell: store-driven modal, not FluentDialog.** `CommandPalette` is a `ModalContainer`
  under `TimeWarpPage`'s `ModalController` (`ApplicationState.ActiveModalId == CommandPalette.ModalId`).
  Why: it already gives one-modal-at-a-time, backdrop click-to-close and store-visible open state, and
  it needs no `FluentDialogProvider` (headless hosts have none). `ActionContent` shows the key hints
  in place of the Close button. Recorded in the `CommandPalette.razor` Design block.
- **Triggers.** New TS module `source/features/command-palette.ts` (imported on demand via
  `services/command-palette-js-module.cs`, same shape as `SignOutJsModule`) registers one document
  `keydown` for Ctrl-K / Cmd-K (`preventDefault`, so the browser's own search bar stays out) and
  `focusin`/`click` on the element carrying `data-command-palette-trigger`, which is the appbar search field.
  The field is now `ReadOnly` with a real label ("Search pages and commands"). The JS handle
  remembers the element that had focus and `RestoreFocus` returns focus there on close. While it
  restores focus, the trigger's own `focusin` is ignored so the palette does not reopen. The
  palette registers after the first interactive render only. Only `TimeWarpPage` hosts it;
  `TimeWarpFocusedPage` (Login) does not.
- **Rows.** `CommandPaletteRoster` builds one row shape, `CommandPaletteRow(Name, Description, Kind, Target)`:
  - Pages come from `PageRegistry.All` (Title, "Go to /url").
  - Commands come from `IActionCatalog` entries with `Visibility` Human/Both and no required
    parameter. That gives Profile.SignOut, Credentials.AddPasskey and
    Credentials.AddExistingPasskey. Counter.IncrementCounter (int) and every Agent-only entry are
    left out.
  - A command's display name is the readable form of its catalog name ("Profile: Sign out").
  - Rows hold strings only, so state clone never copies executor delegates.
- **Ranking.** `CommandPaletteRanker` is deterministic, ordinal and case-insensitive:
  - Match tiers: name prefix > name word-start (space/punctuation or camelCase) > name substring >
    description prefix > word-start > substring > name subsequence (the loose fuzzy tail).
  - Ties break on shorter name, then name, then kind.
  - An empty query lists everything, pages first.
  - No Jev and no LLM.
- **Keyboard / state.** `CommandPaletteState` (Roster, Query, Matches, HighlightedIndex) has
  actions Open, Filter, MoveHighlight (wraps) and Highlight (pointer hover).
  - Enter runs only the highlighted row. With no match nothing is highlighted and Enter does
    nothing, so a near-miss never auto-runs.
  - Esc and backdrop click close the palette. Up/Down move the highlight. Clicking a row runs it.
  - Running a row is sequenced in the component: `CloseModal`, then `CommandPaletteRunner`
    (handlers do not nest dispatch).
- **Permissions.** Open authorizes every row through `IAuthorizationService` against the current
  `AuthenticationState`:
  - Pages use their `[Page] Policy`. Home carries the Anonymous policy.
  - Commands need all of their `Permissions`. These are the same PermissionIds policies that
    NavMenu's `AuthorizeView` uses.
  - A command with no permissions still requires a signed-in principal. Every cataloged command
    acts on the user's own session, so an anonymous visitor sees pages only.
- **Outcomes.** Navigation goes through `RouteState.ChangeRoute`. Commands run through
  `ActionCatalogEntry.Execute(store, [], ct)`, so their outcomes go wherever the action already
  reports them: the shell `NotificationState` region. A catalog name that has vanished is reported
  there as a Warning. The palette has no message bar of its own (TWA0025).
- **Mobile viewport (by CSS, not browser-checked).** The modal panel is width 100% up to 520px with
  16px gutters, so on a narrow viewport the palette fills the screen. The list scrolls inside
  `min(60vh, 420px)` and the input uses 16px text so mobile browsers do not zoom on focus.
- **Tests.** `tests/container-apps/web/web-spa-integration-tests/features/application/command-palette-tests.cs`,
  23/23. The host is a C-create in-proc SPA container using the SPA's own `PolicyRegistration` and a
  fixed principal. Cases:
  - roster = PageRegistry pages + the 3 parameterless human commands
  - Agent-only, parameterized and internal actions absent (12 inputs, including Fetch*, Clear*,
    FiveSecondTask, TwoSecondTask, ThrowException and Theme.Update)
  - a member principal loses developer/admin/credential rows; an anonymous visitor gets Home only
  - ranking tier order and tie-breaks
  - Filter highlights the best match, and no match leaves nothing to run
  - arrows wrap
  - Enter navigates to /Counter
  - Enter runs a zero-argument probe `ActionCatalogEntry` exactly once
  - a vanished command is reported in the shell region
  - the overlay renders (HtmlRenderer) only while it is the active modal; close clears `ActiveModalId`
- **Gates.**
  - `dev build`: 0 warnings, 0 errors.
  - `dev test`: 21 suites, 0 failed (web-spa-integration 108/108).
  - `dev template-smoke`: SUCCEEDED (generated web-jaribu 212/212, co-located runfiles passed standalone).
  - `ganda repo audit`: 30/30 passed. Local `bin/dev` was missing and was rebuilt with
    `ganda repo audit --fix --checks bin-dev`; it is gitignored, so nothing was committed for it.
- **Manual browser check: not performed.** No AppHost was started, per the task rule. Not covered
  by automation: Ctrl-K / Cmd-K in a real browser, the search-field trigger, focus return, and the
  mobile layout.
- **Follow-ups (not this id).** Theme toggle is not cataloged: `Theme.Update` lives in
  TimeWarp.State.Plus, so it needs an upstream `[CatalogAction]`. WebMCP / agent tools are out of
  scope (parent 239).

### How to validate

**Smoke:**
```bash
cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Release -- --filter-class CommandPalette
cd ../../../.. && dotnet run tools/dev-cli/dev.cs -- build
```

**Expect:** `CommandPalette_Should_` 23/23 passed (roster, exclusions ×12, permission filter,
anonymous, ranking ×2, filter/no-match, arrows, Enter navigates to `http://localhost/Counter`, Enter
executes the probe once with zero arguments, vanished command → shell Warning, overlay renders only
while active). `dev build` reports 0 warnings / 0 errors. In a browser (manual, with an AppHost you
started yourself): Ctrl-K or Cmd-K on any shell page, or a click on the appbar search field, opens
the palette with the input focused. Typing "coun" highlights Counter, and Enter goes to /Counter.
Esc or a backdrop click closes it and returns focus to where it was.

## Notes

- Parent 239.
- Cockpit session: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED

## Session

- Created: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED (2026-09-30)
- 2026-09-30: implement oracle (ganda task work): palette, triggers, roster/ranking/permissions, tests, gates.
