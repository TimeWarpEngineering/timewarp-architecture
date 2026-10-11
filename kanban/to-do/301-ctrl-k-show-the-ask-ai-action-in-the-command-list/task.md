# Ctrl-K: show the Ask AI action in the command list

## Description

The app bar "Ask AI" button opens the Ask panel through a TimeWarp.State action, but that action
is not in the Ctrl-K command list. Steven suspected the action is missing the attribute that
registers it in the catalog. Confirmed: it is. But the fix is NOT to add `[CatalogAction]` to
`OpenAskPanel`. Steven's direction (2026-10-11): derive as much as possible, avoid handwriting.
The Ctrl-K catalog must be derived from the same source that drives the action system (every
registered TimeWarp.State action / ActionSet), so anything registered as an action shows in
Ctrl-K automatically, with no separate hand-written opt-in step.

### Current state (master `650fa24f2`, paths under `source/container-apps/web/projects/web-spa/` unless noted)

- Button: `components/TimeWarpPage.razor:108`
  `<FluentButton ... data-qa="AskAiButton" OnClick="OpenAskPanelAsync">Ask AI</FluentButton>`,
  handler at `TimeWarpPage.razor:64` -> `AgentSurfaceState.OpenAskPanel()`.
- Action: `features/application/agent-surface-state/agent-surface-state.open-ask-panel.cs:15-17`
  `OpenAskPanelActionSet` / `public sealed class Action : IBaseAction;` has **no
  `[CatalogAction]`**. Compare `features/profiles/profile-state/profile-state.sign-out.cs:41-47`
  (`[CatalogAction(Description = ..., Visibility = ActionVisibility.Human)]`), which is why Sign
  out appears in Ctrl-K.
- Registration: `program.cs:149` `AddActionCatalog(web-spa assembly)` is opt-in; only
  `[CatalogAction]` types are in `IActionCatalog`.
- Palette roster: `features/application/command-palette/command-palette-roster.cs:68-79` lists
  catalog entries that pass `IsPaletteCommand` (Human/Both, no required parameters, lines 95-97),
  signed-in only (line 70), permission-checked. `OpenAskPanel` would pass every filter; it is
  simply not catalogued.
- Workaround instead of the catalog: the palette footer has a hand-wired "Ask" button,
  `features/application/modals/command-palette/CommandPalette.razor:59-63` (`OpenAskAsync`:
  CloseModal then `OpenAskPanel`) and `:278` (`data-qa="CommandPaletteAsk"`). It is not a row,
  can't be typed/ranked, and is a second hand-listed entry point.
- Row shape: `features/application/command-palette/command-palette-row.cs:28`
  `CommandPaletteRow(Name, Description, Kind, Target)` has no icon or shortcut field. Ask has no
  keyboard shortcut today.
- Tests pin the command set exactly:
  `tests/container-apps/web/web-spa-integration-tests/features/application/command-palette-tests.cs:60`
  (`AddExistingPasskey, AddPasskey, LinkMicrosoft365, ListMyFeedback, SignOut`).
- No analyzer or test checks that chrome buttons dispatch catalogued actions (nothing under
  `source/analyzers` references CatalogAction).
- Other app bar chrome for the same audit: theme toggle `TimeWarpPage.razor:109`
  (`ThemeState.Update(Theme.Dark)`, has a required argument, so not a palette command as-is) and
  the assembly-info modal `TimeWarpPage.razor:63`.

Root cause: the catalog is opt-in (`program.cs:149` `AddActionCatalog` only collects
`[CatalogAction]` types), and `AgentSurfaceState.OpenAskPanelActionSet.Action` has no
`[CatalogAction]`; the palette got a hand-wired footer button instead. The deeper cause is the
separate hand-written opt-in step: any action without the attribute is invisible to Ctrl-K.

## Requirements

- **Derive the catalog from the action system.** Build `IActionCatalog` (or the palette roster's
  command source) from the same discovery that registers TimeWarp.State actions (the
  `AddTimeWarpState` assembly scan / source generator / DI registration that already finds every
  `ActionSet.Action`), so every registered action is a catalog entry automatically. No
  per-action attribute is required to appear. If TimeWarp.State's `AddActionCatalog` can only do
  opt-in, either use the existing action registration data from the app side or add the
  capability to TimeWarp.State and record the follow-up in Notes (kanban, not a GitHub issue).
- **Naming by convention.** Display name, owner label and icon come from the State/ActionSet
  names (e.g. `AgentSurfaceState.OpenAskPanelActionSet` -> "Ask AI" via humanized "Open Ask
  Panel" or a convention rule). Optional metadata (the existing `[CatalogAction]` DisplayName /
  Description / Visibility) may override but is never required.
- **Exclusion only by explicit opt-out.** Internal/plumbing actions (loads, probes, set-state
  internals, actions needing arguments the palette can't supply) are excluded only by an explicit
  opt-out marker or convention, never by omission. The opt-out list is justified (a reason per
  entry) and guarded by a test that fails when a new action is neither shown nor explicitly
  opted out with a reason.
- **Keep filtering.** Keep the roster's existing rules: Human/Both visibility, parameterless,
  signed-in only, and per-action policy/permission checks via `IAuthorizationService`. Human-only
  for `OpenAskPanel`: the agent must not open its own panel. Record the signed-out decision in
  Notes (default: keep the signed-in-only rule).
- **Ask AI appears with no per-action attribute.** Running the derived row closes the palette and
  opens the docked Ask panel.
- **Remove the hand-coded palette footer "Ask" button** (`CommandPalette.razor:59-63`, `:278`,
  `data-qa="CommandPaletteAsk"`); the derived row replaces it.
- **Icons.** If convention-derived icons are cheap, add an icon to `CommandPaletteRow` for both
  pages (`PageRegistryEntry.NavIcon`) and commands; otherwise record a follow-up.
- **Tests.** Replace the pinned hand list at `command-palette-tests.cs:60` with a derivation
  assertion: every registered, non-opted-out, palette-eligible action is present (and opted-out
  ones are absent). Keep the real-browser test: Ctrl-K, type "ask", Enter, palette closes and
  `[data-qa=AgentAsk]` panel is open.
- Update the relevant skill/docs: actions appear in Ctrl-K by derivation; opt out explicitly with
  a reason.
- **Acceptance.** Full ganda walk via `ganda task work 301 --yes`, CI green, merge only via
  `ganda pr merge` after Steven approves.

## Checklist

- [ ] Catalog/roster derived from TimeWarp.State action registration; no required opt-in attribute
- [ ] Convention-based display name/owner/icon; optional metadata override
- [ ] Explicit, justified opt-out for internal actions, guarded by a test
- [ ] Policy/permission, visibility, parameterless and signed-in filtering kept; signed-out decision recorded
- [ ] "Ask AI" row appears with no per-action attribute; runs and opens the panel
- [ ] Footer "Ask" button removed
- [ ] Pinned command-set test replaced by derivation assertion
- [ ] Real-browser (Playwright) test: Ctrl-K, type "ask", Enter, palette closes and `[data-qa=AgentAsk]` is open
- [ ] Skill/docs note
- [ ] Screenshot in the PR (palette filtered to "ask" showing the row; panel open after)

## Notes

- Steven, 2026-10-11 00:19 ICT: "the 'Ask AI' button has an action why isn't it showing in the
  Ctrl-K list we must not have added the attribute?"
- Steven, 2026-10-11 (voice call): correction. Derive as much as possible, avoid handwriting.
  Don't fix this by adding `[CatalogAction]` to `OpenAskPanel`; Ctrl-K must list every
  registered action automatically, excluding internal ones only by explicit, justified opt-out,
  and the hand-coded footer "Ask" button goes away.
- Related: task 300 (Ask app-wide page context).
- Acceptance: full ganda walk via `ganda task work 301 --yes` (implement, review, audit,
  done-move commit, PR), CI green, merge only via `ganda pr merge` after Steven approves.
  Don't run the app on TWE-001; browser proof comes from CI.

## Session

- Created: 309324 (2026-10-11 ICT)
