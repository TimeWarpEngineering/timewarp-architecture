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

- [ ] Hotkey + search-field trigger + overlay
- [ ] Rows from registry + catalog; ranking; keyboard
- [ ] Permission filter
- [ ] Tests
- [ ] GitHub #102 closed from Results
- [ ] Gates; no AppHost

## Depends on

- 239-001
- 239-002

## Notes

- Parent 239.
- Cockpit session: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED

## Session

- Created: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED (2026-09-30)
