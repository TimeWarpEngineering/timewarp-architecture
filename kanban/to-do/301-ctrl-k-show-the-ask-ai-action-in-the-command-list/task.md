# Ctrl-K: show the Ask AI action in the command list

## Description

The app bar "Ask AI" button opens the Ask panel through a TimeWarp.State action, but that action
is not in the Ctrl-K command list. Steven suspected the action is missing the attribute that
registers it in the catalog. Confirmed: it is.

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

Root cause: `AgentSurfaceState.OpenAskPanelActionSet.Action` was never given `[CatalogAction]`;
the palette got a hand-wired footer button instead.

## Requirements

- Add `[CatalogAction(DisplayName = "Ask AI", Description = "Open the Ask AI panel to ask about this app or get help with the current page.", Visibility = ActionVisibility.Human)]`
  (wording may be tuned) to `OpenAskPanelActionSet.Action`, so the palette shows a typed,
  ranked "AgentSurface: Ask AI" row (or adjust the owner label if "AgentSurface" reads poorly).
  Human-only: the agent must not open its own panel.
- Running the row closes the palette and opens the docked Ask panel (the runner already closes
  the modal before executing).
- Decide signed-out behavior and record it: the roster rule shows commands to signed-in users
  only; the app bar button is visible signed-out and Ask then shows "Sign in to use Ask". Keep
  the rule unless there is a good reason, and say so in Notes.
- Icon and shortcut: rows have neither today. If adding an icon to the row model is cheap, add it
  for both pages (`PageRegistryEntry.NavIcon`) and commands; otherwise record it as follow-up.
  No new keyboard shortcut is required.
- Remove or justify the hand-wired footer "Ask" button in `CommandPalette.razor:278` (it may
  stay as a shortcut to the same action, but it must dispatch the catalogued action).
- Guard against recurrence: a test (or analyzer) that fails when an app-chrome button in
  `TimeWarpPage` dispatches a parameterless user-facing action that is not in `IActionCatalog`;
  at minimum a test asserting `AgentSurface.OpenAskPanel` is a palette command. Update the
  pinned set at `command-palette-tests.cs:60`.
- Note the convention in the relevant skill (tw-blazor / command palette docs): user-facing
  chrome actions get `[CatalogAction]`.

## Checklist

- [ ] `[CatalogAction]` on `OpenAskPanelActionSet.Action` with DisplayName and Description
- [ ] Palette row label reads well ("Ask AI")
- [ ] Footer "Ask" button removed or dispatching the same catalogued action
- [ ] Signed-out decision recorded
- [ ] Icon on rows added or follow-up recorded
- [ ] Guard test/analyzer for uncatalogued chrome actions; pinned command set updated
- [ ] Real-browser (Playwright) test: Ctrl-K, type "ask", the Ask AI row is highlighted, Enter, palette closes and `[data-qa=AgentAsk]` panel is open
- [ ] Skill/docs note
- [ ] Screenshot in the PR (palette filtered to "ask" showing the row; panel open after)

## Notes

- Steven, 2026-10-11 00:19 ICT: "the 'Ask AI' button has an action why isn't it showing in the
  Ctrl-K list we must not have added the attribute?"
- Related: task 300 (Ask app-wide page context).
- Acceptance: full ganda walk via `ganda task work 301 --yes` (implement, review, audit,
  done-move commit, PR), CI green, merge only via `ganda pr merge` after Steven approves.
  Don't run the app on TWE-001; browser proof comes from CI.

## Session

- Created: 309324 (2026-10-11 ICT)
