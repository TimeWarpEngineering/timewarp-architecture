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

## Amendment (2026-10-14, cockpit per Steve): coordinate with task 361 (assistant name)

301 and 361 run as one stack, **301 → 361** (then 352–354). 361 makes every assistant label come
from a runtime, admin-set `SiteSettings.AssistantName` (default **Akili**). So:

- **Runtime display-name override.** The derived catalog/palette row must support a **runtime**
  display-name (and optionally description/icon) override per action, in addition to the
  compile-time convention and optional `[CatalogAction]` metadata. Shape suggestion: a small
  `IActionDisplayOverride` / provider the roster consults per entry. Record the shape.
- **No hard-coded "Ask AI".** Do **not** bake "Ask AI" (or any literal) into the convention or
  metadata for `AgentSurfaceState.OpenAskPanel`. The convention default (humanised "Open Ask
  Panel" or similar) is fine as a placeholder. 361 supplies "Ask {AssistantName}" through the
  override.
- Requirements change accordingly: "Ask AI appears with no per-action attribute" now reads
  "**the OpenAskPanel row appears** with no per-action attribute". The Playwright check types
  "ask", which matches either name.
- The removal of the footer "Ask" button stays in 301. The top-bar button label is 361's.

## Checklist

- [x] Catalog/roster derived from TimeWarp.State action registration; no required opt-in attribute
- [x] Convention-based display name/owner/icon; optional metadata override
- [x] Explicit, justified opt-out for internal actions, guarded by a test
- [x] Policy/permission, visibility, parameterless and signed-in filtering kept; signed-out decision recorded
- [x] OpenAskPanel row appears with no per-action attribute; runs and opens the panel
- [x] Runtime display-name override supported by the derived catalog (for 361)
- [x] Footer "Ask" button removed
- [x] Pinned command-set test replaced by derivation assertion
- [x] Real-browser (Playwright) test: Ctrl-K, type "ask", Enter, palette closes and `[data-qa=AgentAsk]` is open
- [x] Skill/docs note
- [ ] Screenshot in the PR (palette filtered to "ask" showing the row; panel open after) — CI artifact `ctrl-k-ask-row.png` / `ctrl-k-ask-panel-open.png` from AskSignIn_Given_Wasm; attach at PR time (app not run on TWE-001)

## Notes

- Steven, 2026-10-11 00:19 ICT: "the 'Ask AI' button has an action why isn't it showing in the
  Ctrl-K list we must not have added the attribute?"
- Steven, 2026-10-11 (voice call): correction. Derive as much as possible, avoid handwriting.
  Don't fix this by adding `[CatalogAction]` to `OpenAskPanel`; Ctrl-K must list every
  registered action automatically, excluding internal ones only by explicit, justified opt-out,
  and the hand-coded footer "Ask" button goes away.
- Related: task 300 (Ask app-wide page context).
- Derivation source (2026-10-14, implementer): TimeWarp.State's `AddActionCatalog` is opt-in only
  (it reads the `[CatalogAction]` generator's assembly attribute). Instead of changing TimeWarp.State,
  this repo's generators package gained `ActionRosterSourceGenerator`: it lists every
  `*ActionSet.Action` implementing `TimeWarp.Mediator.IAction` (the same discovery rule TimeWarp.State's
  ActionSet method generator uses) as `ActionRoster.All`, with a reflection-free `Execute` that calls the
  generated `State.Method(externalCancellationToken:)` for actions without required constructor
  parameters. Scope is the app's own assembly (web-spa); TimeWarp.State.Plus actions (RouteState etc.)
  are not listed. Possible follow-up (not filed): an opt-out-capable "all actions" mode in
  TimeWarp.State itself so other hosts get the roster without this generator.
- Opt-out shape: `[CommandPaletteOptOut("reason")]` on the Action class (attribute emitted by the
  generator, like `[Page]`). 36 web-spa actions opted out, each with a reason (loads, state resets,
  ceremony/prompt steps, Ask composer internals, the palette's own Open, CloseModal, demos, ResetStore).
  A blank reason does not opt out; the guard test `Opt_Out_Only_Explicitly_With_A_Reason` fails on one
  and on an action that is both opted out and a Human `[CatalogAction]`.
- `[CatalogAction]` stays optional metadata, joined by Action type: DisplayName, Description,
  Visibility, Permissions override the defaults (Human-only, no permissions, generated label). The agent
  tool set keeps its catalog-only view of the palette (`CommandPaletteRoster.BuildAsync` catalog overload).
- Runtime override shape (for 361): `IActionDisplayOverride.Find(ActionRosterEntry) → ActionDisplay?`
  (`ActionDisplay(Name, Description?)`), registered as scoped services; the palette asks each in
  registration order, first non-null wins; Name replaces the whole label (no owner prefix). None
  registered in 301. Convention label for OpenAskPanel is the placeholder "Agent surface: Open ask panel".
- Signed-out decision: kept the signed-in-only rule for all commands (including derived ones). Signed
  out, Ask opens from the app-bar Ask AI button; the Playwright signed-out paths use that button.
- Icons: not added. Pages have `NavIcon` but actions have no cheap convention icon, and a FluentUI
  `Icon` on the cloned row record is not free. Follow-up candidate: page-row icons only.
- Acceptance: full ganda walk via `ganda task work 301 --yes` (implement, review, audit,
  done-move commit, PR), CI green, merge only via `ganda pr merge` after Steven approves.
  Don't run the app on TWE-001; browser proof comes from CI.

## Results

- New `ActionRosterSourceGenerator` (`source/analyzers/timewarp-architecture-analyzers/generators/action-roster-source-generator.cs`)
  emits `ActionRoster`/`ActionRosterEntry` and `[CommandPaletteOptOut]`; 6 generator tests.
- `CommandPaletteRoster` builds commands from `ActionRoster.All` + optional catalog metadata +
  `IActionDisplayOverride`s; `CommandPaletteRunner` executes `ActionRosterEntry.Execute`.
- Footer "Ask" button (`data-qa="CommandPaletteAsk"`) removed; the derived
  `AgentSurface.OpenAskPanel` row ("Agent surface: Open ask panel") replaces it.
- Palette commands now: AgentSurface.NewConversation, AgentSurface.OpenAskPanel,
  Credentials.AddExistingPasskey, Credentials.AddPasskey, Credentials.LinkMicrosoft365,
  Feedback.ListMyFeedback, Profile.SignOut (permission-filtered).
- Tests: pinned list replaced by derivation assertion; opt-out guard; Ask row ranks first for "ask"
  and opens the panel; runtime override; probe-based exclusion test. Playwright: AskSignIn signed-in
  path is Ctrl-K → "ask" → Enter → palette hidden → `[data-qa=AgentAsk]`; AskSurface (signed out)
  asserts the footer button is gone and opens Ask from the app bar.
- Gates run locally: `dev build` (0 warnings), sourcegenerator tests 114/114, web-spa-integration-tests
  264/264, `dev check-version` OK (2.0.0-beta.20 already ahead of nuget), `dev template-smoke` SUCCEEDED,
  `ganda repo audit` pass. Playwright tests compiled, not run (browser proof from CI per Notes).
- Skill: `skills/tw-blazor/SKILL.md` documents derivation, explicit opt-out with a reason, metadata, overrides.

### How to validate

Smoke:
```bash
dev build
cd tests/analyzers/timewarp-architecture-sourcegenerator-tests && dotnet test -- --filter-class ActionRosterSourceGenerator_Tests
cd tests/container-apps/web/web-spa-integration-tests && dotnet test -- --filter-class CommandPalette_Should_
# CI: web-spa-playwright-tests AskSignIn_Given_Wasm + AskSurface
```

Expect:
- Build 0 warnings/0 errors; ActionRoster generator tests pass; CommandPalette_Should_ passes.
- Signed in, Ctrl-K → type "ask" → the highlighted row is "Agent surface: Open ask panel"; Enter closes
  the palette and the docked `[data-qa=AgentAsk]` panel is open (CI screenshots `ctrl-k-ask-row.png`,
  `ctrl-k-ask-panel-open.png`).
- No `data-qa="CommandPaletteAsk"` anywhere in the palette.
- Adding a new parameterless ActionSet makes it appear in Ctrl-K with no attribute; adding
  `[CommandPaletteOptOut("")]` fails `Opt_Out_Only_Explicitly_With_A_Reason`.

### Review disposition

- Rounds: 2; roster: general; effort 3 (by-diff budget, 1040 lines).
- Final counts: bug 1 fixed; suggestion 5 fixed; nit 1 fixed, 1 wontfix; 0 open.
- Disposition: **accepted-exceptions**. M8 (generated `CommandPaletteOptOutAttribute` lands in
  action-less assemblies) is wontfix: it is harmless and follows the `[Page]` / `[StateAccess]` pattern.
- Review fixes in 5e1dd6dba: Playwright Ctrl-K wait keyed on the filtered Command row; roster completeness
  reflection check; CloseAskPanel opted out; empty fallback description; const/partial opt-out reasons;
  permission rule in tw-blazor skill; generator tests for private/generic/const/partial (118/118).
- Paths: `review/review-framework.md`, `review/round-2/merged.md`, `review/disposition.md`.

## Session

- Created: 309324 (2026-10-11 ICT)
- 2026-10-14: amended for 361 (runtime display-name override; no hard-coded "Ask AI"). Stacked 301 → 361 → 352–354.
- 2026-10-14: implement oracle (ganda task work): generator-derived roster, opt-outs, override seam, tests, skill note; gates green locally.
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-14T10:28:24Z
- Review oracle (claude-opus-5-5): effort 3, general reviewer + fix loop, 2 rounds, accepted-exceptions — 2026-10-14
