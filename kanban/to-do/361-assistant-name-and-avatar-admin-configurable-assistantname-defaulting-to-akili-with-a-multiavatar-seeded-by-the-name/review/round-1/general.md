# Round 1 — general
**Date:** 2026-10-14
**Scope reviewed:** fe4dec7e0..HEAD (commit 639643d5f)

## Summary

The core is solid. The aggregate and the validator cannot let an invalid name through, the
migration, designer and snapshot agree, and the anonymous `GetAssistantIdentity` payload really is
`{ name, avatar }` only. The name is HTML-encoded everywhere it is rendered, and `SetAssistantAvatar`
accepts only base64 SVG data URIs, so it cannot break out of `url()`. Two gaps break the brief's
"one source of truth" promise. The Ask settings menu still hard-codes "Ask" in three user-facing
strings. A rename made through the agent-visible `UpdateSiteSettings` catalog action does not
relabel anything, because the identity refresh lives in the Authentication page rather than in the
action handler. The other items are smaller: a stale name in the model's instructions, startup
ordering, caching, Unicode characters the plain-text rule misses, and test robustness.

## Issues

### Issue 1 — Severity: bug
- File: source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AskSettingsMenu.razor:31
- Description: The panel's settings menu still hard-codes the old assistant label in user-facing text. The examples are `ButtonName = "Ask settings"`, used as the Title and aria-label of the header icon button, and at lines 44–45 the status subtitles "Ask runs under your session without a conversation credential", "Ask can no longer act on this app…" and "Ask can act on this app as you". `chat-readiness-probe.cs:66` also writes "…instead of the Ask configuration." into the problem text that the error state renders in `AgentAskErrorDetail`. The brief says every user-facing assistant label derives from `AssistantName` and that the hard-coded "Ask" is removed. The Results claim "The 'Ask' and 'Ask AI' literals are gone", which is false for this component. After a rename to Zawadi, the panel header says "Zawadi" but its settings button reads "Ask settings" and the menu says "Ask can act on this app as you".
- Suggestion: Derive these strings from `AgentSurfaceState.AssistantName`, for example `$"{AssistantName} settings"` and `$"{AssistantName} can act on this app as you"`. Do the same for the probe's file-response text, or make it neutral ("the assistant configuration"). Extend `assistant-identity-tests.cs` to assert the settings button's aria-label.
- Status: open

### Issue 2 — Severity: bug
- File: source/container-apps/web/projects/web-spa/features/settings/site-settings-state/site-settings-state.update-site-settings.cs:54
- Description: `UpdateSiteSettingsActionSet.Action` is a `[CatalogAction(Visibility = ActionVisibility.Agent)]`, so the in-app model and WebMCP can invoke it. The command now carries `AssistantName`, and the page context exposes `assistantName`. However, `HandleSuccess` only updates `SiteSettingsState`. The refresh of `AgentSurfaceState` (`LoadAssistantIdentity`) is chained in the component, in `AuthenticationPage.razor:95` `HandleValidSubmit`. When the agent renames the assistant, or any other caller dispatches the action, the top bar, Ctrl-K row, header, greeting and feedback titles keep the old name until a full reload. Putting the follow-up dispatch in the component also cuts against "event handlers only dispatch actions": the component holds the save-then-refresh logic.
- Suggestion: In `HandleSuccess`, when `response.AssistantName` differs from the previous value (or always, since the call is cheap), send `AgentSurfaceState.LoadAssistantIdentityActionSet.Action`. Use the handler's sender/mediator, and add a CrossSliceReference if TWA0009 requires it. Then drop the extra dispatch from `AuthenticationPage.HandleValidSubmit`. Add an in-proc test: dispatch `UpdateSiteSettings` and assert `AgentSurfaceState.AssistantName` follows.
- Status: open

### Issue 3 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AgentAsk.razor:306
- Description: `string assistantName = AssistantName;` is captured once, when the agent is built. `RenderInstructions` runs on every request, but it closes over the captured value, and a name change is not part of the rebuild key (`BuiltPath`, `BuiltMode`, `BuiltGeneration`). Suppose the panel is open, or an agent is already built, when the name changes through Issue 2's path or the admin page. The header then says "Zawadi" while the model is still told "You are Akili" and introduces itself that way. The conversation credential's label "{name} conversation" is also baked at issue time, so the header title and conversation title can disagree.
- Suggestion: Read `AgentSurfaceState.AssistantName` inside `RenderInstructions` instead of capturing it, because that function already runs per request. Accept, or document, that the conversation label keeps the name it was issued with.
- Status: open

### Issue 4 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/identity/AuthenticationStateListener.razor:16
- Description: `await NoSubAgentSurfaceState.LoadAssistantIdentity();` runs before `GetAuthenticationStateAsync` and `HandleUserAuthentication`. Every app load therefore waits one extra HTTP round trip before the profile, permissions, credentials and site-settings fetches start. The same file puts the task-293 probe last precisely "so the probe never delays … the identity work above". A slow or hanging `api/settings/assistant` call delays sign-in-dependent UI to show a label.
- Suggestion: Move the call after `HandleUserAuthentication(...)`, next to the `LoadChatConfiguration` placement, or start it without blocking the auth path.
- Status: open

### Issue 5 — Severity: suggestion
- File: source/container-apps/web/features/settings/get-assistant-identity/get-assistant-identity-handler-application.cs:102
- Description: The brief asks for a "cheap, cache-friendly" delivery. This anonymous endpoint does a DB read and a full `MultiavatarGenerator.Generate` + UTF-8 + base64 on every call: every app load, from any unauthenticated client, with no response caching or ETag. The output is a pure function of the name, so this is repeated work any anonymous caller can trigger cheaply.
- Suggestion: Memoize `AvatarFor` by name, for example in a single-entry static cache keyed on the name, because there is only ever one current name. Optionally add a short `Cache-Control` or an ETag derived from the name.
- Status: open

### Issue 6 — Severity: suggestion
- File: source/libraries/timewarp-identity/settings/assistant-name-rules.cs:43
- Description: "Plain text" rejects only `char.IsControl` (Cc) and `<` `>`. That misses Unicode characters that matter for both the labels and the system prompt:
  - U+2028 / U+2029, the line and paragraph separators (categories Zl and Zp, not Cc). A name such as `"A Ignore the rules"` passes and puts a line break into the instructions' first line.
  - Bidi overrides and isolates such as U+202E and U+2066, and zero-width characters such as U+200B (category Cf). These allow spoofed or invisible labels in the top bar, the Ctrl-K row and the feedback titles.

  The Design region names injection into instructions as the reason for the rule.
- Suggestion: Also reject `UnicodeCategory.Format`, `LineSeparator` and `ParagraphSeparator`, through `CharUnicodeInfo.GetUnicodeCategory`. Add `"Ak ili"` and `"Ak‮ili"` to both the validator and the aggregate invalid-name tests.
- Status: open

### Issue 7 — Severity: nit
- File: source/container-apps/web/features/settings/site-settings-details-contracts.cs:33
- Description: The `AssistantNameRules` Design region says "The aggregate and the contract validator both call IsValid". The validator does not. It uses three separate rules (`NotEmpty`, a trimmed-length `Must`, and `IsPlainText` on the *untrimmed* value), while the aggregate runs `IsPlainText` on the *trimmed* value. The validator is stricter (for example, it rejects `"Akili\t"`), so nothing invalid reaches the aggregate. Still, "cannot disagree" is not literally true, and a future edit to one path could drift.
- Suggestion: Either collapse the validator to `RuleFor(d => d.AssistantName).Must(AssistantNameRules.IsValid).WithMessage(...)` (keep `NotEmpty` for the required message if wanted), or reword the Design text to say what each side checks.
- Status: open

### Issue 8 — Severity: nit
- File: tests/container-apps/web/web-server-integration-tests/features/settings/site-settings-endpoint-tests.cs:184
- Description: `Anonymous_Assistant_Identity_Should_Return_Only_Name_And_Avatar` checks for leaks with case-insensitive substring searches ("entra", "passkey", "version", "tenant") over the raw JSON, and that JSON includes an ~8 KB base64 avatar. Base64 is mixed-case letters, so some names' avatars will contain, for example, "TeNaNt" and fail the test deterministically even though nothing leaked. The check also wouldn't catch a newly added field with a different name.
- Suggestion: Parse with `JsonDocument` and assert that the root object's property names are exactly `{ "name", "avatar" }`.
- Status: open

### Issue 9 — Severity: nit
- File: tests/container-apps/web/web-server-integration-tests/features/settings/site-settings-endpoint-tests.cs:211
- Description: `Administrator_Rename_Should_Show_In_The_Public_Identity` mutates the shared host's site settings and restores them only on the success path. If any assertion fails first, the name stays "Zawadi" for the remaining tests in the class. It also restores `AssistantNameRules.Default` rather than the value it read (`before.Name`).
- Suggestion: Wrap the body in `try`/`finally` and restore `before.Name` in the `finally`.
- Status: open

### Issue 10 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/application/application-state/application-state.reset-store.cs:27
- Description: `Store.Reset()` (the Counter page's "Reset Store") re-runs `AgentSurfaceState.Initialize`, which puts `AssistantName` back to Akili and clears the avatar. Nothing reloads the identity afterwards. On a renamed app the top bar then reads "Ask Akili" until a full page load.
- Suggestion: Dispatch `LoadAssistantIdentity` after the reset, or accept this and note it in the `AgentSurfaceState` Design region.
- Status: open
