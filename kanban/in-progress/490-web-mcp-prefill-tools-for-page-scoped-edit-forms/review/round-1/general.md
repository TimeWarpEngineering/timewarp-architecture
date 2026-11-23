# Round 1 — general
**Date:** 2026-11-24
**Scope reviewed:** branch HEAD 5cb923a25 vs merge-base 039361895 (all 26 files), plus the call sites in `web-mcp-dispatcher.cs`, `catalog-agent-functions.cs`, `catalog-agent-schema.cs`, `ProfilePage.razor(.cs)`, and `update-profile-contracts.cs`.

## Summary

The change adds a synthetic `Prefill.<Action>` tool for each `[EditFormSubmit]` page-scoped submit action. It stores a normalized JSON draft through a catalog suggestion action, which the Profile page applies to its working copy and then clears. A dirty form asks first. Agent-filled fields show the Info `StatusBadge` labeled Agent. The tool list, schema, approval, off-page offer, both drivers, and the suggestion state handlers are well built and well tested. The main risk is the page glue in `ProfilePage.OnAfterRenderAsync`, which has no test: a draft that lands while the page is still loading the profile is applied and then overwritten, so it is lost after the tool reports `drafted: true`.

The goal restatement matches the task. `EditFormSubmit` as the opt-in and `SetProfileSuggestion` as the name of the `Prefill<X>` action are design choices, not a misread.

No commands were run. Findings come from reading the diff and the recorded `## Results`.

## Issues

### Issue 1 — Severity: bug
- File: source/container-apps/web/projects/web-spa/features/profiles/pages/ProfilePage.razor:78
- Description: The page applies a suggestion before the working copy is loaded, then the load overwrites it. `OnInitializedAsync` (line 66) awaits `FetchProfileData()` and only then calls `CopyCommandFromProfile()`. Blazor renders, and runs `OnAfterRenderAsync`, while that await is pending, and a store change to `ProfileState` causes another render. If a suggestion is in the store at that point, `OnAfterRenderAsync` sees `Command` and `Baseline` as both `new()`, so `IsDirty()` is false. It applies the draft onto the empty command, sets `HandledSuggestionId`, and dispatches `ClearProfileSuggestion`. When the fetch completes, `CopyCommandFromProfile()` replaces `Command` with `Command.From(profile)` and clears `AgentFilled`. The draft is gone and the badges disappear, but the tool already returned `{"drafted":true}`. The tool's own off-page flow makes this likely: an offer to `/Profile`, then `navigate`, then an immediate `Prefill` call in Automatically edit while `FetchProfileData` is still in flight. WebMCP clients can call tools back to back.
- Suggestion: Do not consume a suggestion until the working copy has been loaded. For example, add a page flag that `CopyCommandFromProfile` sets only after it copies a non-null `Profile`, and return early from `OnAfterRenderAsync` until it is set. The suggestion stays in the store, and the render after the fetch applies it. Add a regression test for "suggestion stored before the profile loads is applied after it loads" (see Issue 2).
- Status: open

### Issue 2 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/profiles/pages/ProfilePage.razor:78
- Description: The page logic that ties the feature together has no test, and the How to validate smoke was not run (`## Results`: "No browser in this session, so the running page was not opened"). That logic is: dirty check → `ConfirmProfileSuggestion` → apply only the supplied fields → `ClearProfileSuggestion` → badge until the person edits the field. The tests cover each action handler, `FormPrefill.Apply`/`Matches` on a bare command, and the badge on a bare `FormField`. Nothing checks that the Profile page dispatches Confirm when dirty, skips it when clean, applies, clears, or shows the badge on the Profile fields. The acceptance line "a dirty form asks before apply" is proven only at the action level. Issue 1 is the kind of defect this gap lets through.
- Suggestion: Add coverage that drives the page lifecycle (an interactive render that runs `OnAfterRenderAsync`, or the repo's browser lane): a clean form applies without a prompt, a dirty form prompts, decline keeps the typed value, the Profile field shows `data-qa="AgentFilled"`, and the Issue 1 regression. If no lane can drive this in the walk, run the How to validate smoke and record it under `## Results`.
- Status: open

### Issue 3 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/application/agent/form-prefill.values.cs:30
- Description: A draft with no recognized fields counts as a success. Empty or blank arguments, `null`, or an object whose keys all fail `TryMatch` (for example `{"name":"Ada"}`) normalize to `"{}"` with `fields = []`. `DispatchAsync` then stores that empty suggestion and returns `{"drafted":true,"fields":[]}`. The agent is told its draft landed when nothing will change. On a dirty form the person also gets "Replace unsaved edits?" for a draft that changes nothing. The schema says `additionalProperties: false`, so an agent that sends a wrong key gets no signal.
- Suggestion: In `TryNormalizeObject` (and the empty-argument path), return false when no field matched. Use an error that names the accepted keys, for example "No draftable fields. Use: displayName, email, language, region, theme, notifications." Nothing is stored and no dialog opens. Update `Apply_Should_CopyOnlySuppliedFields_And_LeaveTheBaseline`, which currently asserts the empty case is true.
- Status: open

### Issue 4 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/profiles/profile-state/profile-state.confirm-profile-suggestion.cs:604
- Description: `Apply` does not move the baseline (by design), so after a first agent draft the form is dirty. A second `Prefill` call then opens the dialog "An agent drafted new profile values. Applying them replaces what you typed." The person typed nothing; the unsaved values were the agent's.
- Suggestion: Word the message so it fits both cases, for example "Applying them replaces unsaved changes on this form." Or skip the prompt when every differing field is still agent-filled and unchanged.
- Status: open

### Issue 5 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/application/agent/web-mcp-dispatcher.prefill.cs:266
- Description: The `else` branch, for a Prefill tool that is not page-bound, cannot run. `FormPrefill.SelectAsync` skips every entry where `!PageAgentScope.IsPageBound(entry.ActionType)`, so every Prefill tool is page-bound. The branch also means `pageBound` is always true.
- Suggestion: Drop the `pageBound` local and the `else` branch, and state in the Design region that Prefill tools are page-bound by construction.
- Status: open
