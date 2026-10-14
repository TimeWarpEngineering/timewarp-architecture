# Round 1 — general
**Date:** 2026-10-14
**Scope reviewed:** master...HEAD (43f043ebf)

## Summary
The derivation works and matches the spec. `ActionRosterSourceGenerator` finds all 108 web-spa
ActionSets (108 declarations, 108 roster entries, none dropped). The generated
`State.Method(externalCancellationToken:)` call matches what TimeWarp.State 12.0.0-beta.13's
`ActionSetMethodSourceGenerator` emits: it takes parameters from the first explicit ctor, keeps
defaults, and passes a `CancellationToken? externalCancellationToken = null` tail. Filtering
(HasFlag(Human) is right: ActionVisibility is `[Flags]` 1/2/3), the signed-in-only rule, the
permission checks, the catalog-only agent overload and the footer removal all check out.
`CommandPalette_Should_` passes 42/42 locally. The main problem is a race in the new Playwright
Ctrl-K step. The other points are guard-test strength, opt-out consistency and generator
robustness.

## Issues
### Issue 1 — Severity: bug
- File: tests/container-apps/web/web-spa-playwright-tests/ask-sign-in-playwright-tests.cs:420
- Description: `OpenAskAsync(signedIn: true)` calls `FillAsync("ask")` and then waits for `.twe-palette__row--active`. With an empty query, `CommandPaletteState.Apply` already highlights row 0 (the first page row), so the wait returns at once on the stale pre-filter row. The `InnerTextAsync().ShouldContain("ask")` on line 422 can read that row before the WASM `@bind:after=FilterAsync` re-render lands. That makes the check flaky, and `ctrl-k-ask-row.png` can capture the unfiltered list. Enter itself is safe because the component re-filters when `Query != QueryText`.
- Suggestion: Wait on the filtered state, e.g. `Page.Locator(".twe-palette__row--active", new() { HasText = "ask" })` (or `Assertions.Expect(active).ToContainTextAsync("ask", new() { IgnoreCase = true })`) before the screenshot and Enter.
- Status: open

### Issue 2 — Severity: suggestion
- File: tests/container-apps/web/web-spa-integration-tests/features/application/command-palette-tests.cs:124
- Description: The second loop in `Opt_Out_Only_Explicitly_With_A_Reason` is a tautology. `excluded` (`IsOptedOut || Execute is null || Visibility == Agent`) is the exact complement of `IsPaletteCommand` (`!IsOptedOut && Execute != null && HasFlag(Human)`) for every valid ActionVisibility, so `shown ^ excluded` can never fail. The derivation assertion in the roster test also rebuilds the same predicate. The guard that has teeth is the blank-reason / Human-`[CatalogAction]` check in the first loop. Nothing checks the roster itself against the real action set, so an ActionSet the generator silently skips (generic, private, non-IAction Action) would be "excluded by omission" with no test failing.
- Suggestion: Drop the tautological loop, or replace it with a completeness check. Reflect over the web-spa assembly for every `*ActionSet+Action` implementing `TimeWarp.Mediator.IAction` (test-only reflection is fine) and assert each type appears in `ActionRoster.All`.
- Status: open

### Issue 3 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/application/agent-surface-state/agent-surface-state.close-ask-panel.cs:21
- Description: The opt-outs are inconsistent. `AgentSurface.ToggleAskExpand` is opted out because "offering it with the panel closed does nothing visible". `AgentSurface.CloseAskPanel` has the same property and is shown, so it appears in Ctrl-K even when the panel is closed. For the query "ask" it ties with OpenAskPanel on tier (both are name word-start). OpenAskPanel wins only because its label is one character shorter ("Open" vs "Close"). The unit test `List_Open_Ask_Panel_...` and the Playwright Enter both depend on that tie-break.
- Suggestion: Either opt out `CloseAskPanel` with the same panel-chrome reason (the panel's own close control and Esc own it), or keep it and record why it differs from ToggleAskExpand. Either way, don't rely on label length for the "ask" ranking. That goes away once 361's "Ask {AssistantName}" override is a name prefix, but it is fragile until then.
- Status: open

### Issue 4 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/application/command-palette/command-palette-roster.cs:82
- Description: An uncatalogued derived command gets the description `$"Run {pair.Action.Name}"`, e.g. "Run AgentSurface.OpenAskPanel". That puts a code identifier into user-visible palette text. The ranker also matches descriptions, so the query "run" matches every derived command at description tier.
- Suggestion: Use the humanized label for the fallback description (e.g. `Label(...)` without the owner), or an empty string. Keep the stable name only as `Target`.
- Status: open

### Issue 5 — Severity: suggestion
- File: source/analyzers/timewarp-architecture-analyzers/generators/action-roster-source-generator.cs:115
- Description: `GetOptOutReason` reads the attribute syntactically from one declaration. A reason given as a `const` (`[CommandPaletteOptOut(OptOutReasons.StartupLoad)]`) or an interpolated constant reads as `""`, so the action is treated as not opted out, lands in the palette, and the guard fails with "needs a reason" even though one was given. An attribute on another `partial` declaration of the Action is not seen at all. The Design region documents the literal-only rule, but nothing tells the author.
- Suggestion: Read `actionSymbol.GetAttributes()` and take `ConstructorArguments[0].Value as string` (it is already in the semantic transform; the model stays a string). Or report a diagnostic when the argument is not a string literal.
- Status: open

### Issue 6 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/application/command-palette/command-palette-roster.cs:83
- Description: A derived command with no `[CatalogAction]` defaults to no permissions, so every signed-in user is offered it. Today the only such rows are the three AgentSurface actions, which is fine. But a future parameterless admin action (e.g. a `RoleState`/`FeedbackAdminState` refresh that is not a load) would appear for every signed-in user unless its author remembers to catalogue it or opt it out. That is the "by omission" failure mode the task set out to remove, moved from visibility to permissions.
- Suggestion: Record the default in the skill note as a rule ("an action behind a permission must carry `[CatalogAction(Permissions = …)]` or opt out"). Better, add a guard that fails when an uncatalogued, shown action lives in a slice whose pages all require a policy.
- Status: open

### Issue 7 — Severity: nit
- File: tests/analyzers/timewarp-architecture-sourcegenerator-tests/action-roster-source-generator-tests.cs:1
- Description: The new test file has no `#region Purpose` (AGENTS.md: TWA0004 regions on every source file; its sibling tests are mixed). The generator tests also don't cover the exclusion paths the Design region claims: private/generic Actions, a non-literal opt-out reason (`""`), and the `params` / primary-ctor cases. The emitted roster is never compiled in the tests (string matching only).
- Suggestion: Add a Purpose region. Add one source case each for the private, generic and non-literal-reason paths.
- Status: open

### Issue 8 — Severity: nit
- File: source/analyzers/timewarp-architecture-analyzers/generators/action-roster-source-generator.cs:46
- Description: `CommandPaletteOptOutAttribute.g.cs` is emitted unconditionally into every project that references the generator: yarp, api-server, aspire-app-host, web-domain, web-infrastructure, web-server, timewarp-identity. None of them has an ActionSet or a palette. It is harmless (same pattern as `PageAttribute`, internal), but it is dead code shipped in every generated app's server assemblies.
- Suggestion: Emit the attribute only when the compilation references TimeWarp.State (check `compilation.GetTypeByMetadataName("TimeWarp.Mediator.IAction")`), or accept it as consistent with `[Page]`.
- Status: open
