# Ask panel focuses the message box when it opens

## Problem (2026-10-17)

Opening the Ask panel leaves focus wherever it was (the header button, the palette's restored
element, or the page). The person must click the message box before typing (Steve, 2026-10-17).
Requirement: whenever the panel opens, the caret is in the message text box, ready to type, on
every open path.

## Findings (investigated read-only, public XML docs only)

- **The library `MessageInput` exposes no focus hook.** `Microsoft.AspNetCore.Components.AI.xml`
  (package `Microsoft.AspNetCore.Components.AI` 0.1.0-preview.1.26466.103) documents only
  `AgentContext`, `Placeholder`, `LeadingActions`, `TrailingActions` and `Dispose` for
  `MessageInput`. There is no `ElementReference`, no `Focus()` method and no autofocus parameter
  (zero hits for focus/ElementReference/autofocus in the XML). Focus therefore goes through the
  DOM, as the panel already does.
- **A focus helper already exists.** `source/container-apps/web/projects/web-spa/source/features/ask-ai.ts`
  exports `FocusComposer()` (focuses `.twe-agent-ask .sc-ai-input__textarea`), reached from C# by
  `AskAiJsModule.FocusComposerAsync` in `services/ask-ai-js-module.cs`. It is used today to return
  focus after the `@` picker closes. Reuse it. `AskEditModePill` also has a `RestoreFocus` /
  `FocusRestored` pair driven by `EditModeFocusPending` in `AgentAsk.razor`: precedent for
  one-shot focus requests.
- Panel: `source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AgentAsk.razor`
  (+ `.razor.cs`, `.razor.css`). `MessageInput` is built only after `EnsureAgentAsync` finishes, in
  `OnAfterRenderAsync`, so the textarea can appear one render after the panel mounts (the probe,
  credential and agent build come first, and signed-out, unconfigured and error states show no
  textarea at all). A focus call on the open render alone can miss it.
- Every open path ends in one State action, `AgentSurfaceState.OpenAskPanelActionSet.Action`
  (`features/application/agent-surface-state/agent-surface-state.open-ask-panel.cs`, sets
  `IsPanelOpen = true`). `TimeWarpPage.razor` mounts `<AgentAsk />` when `IsPanelOpen`.
  Paths:
  - header button: `data-qa="AskAiButton"` in `components/TimeWarpPage.razor`
    (`OpenAskPanelAsync`);
  - Ctrl-K command palette: the derived `AgentSurface.OpenAskPanel` row (see
    `features/application/modals/command-palette/CommandPalette.razor`,
    `agent/ask-assistant-display-override.cs`). The palette restores focus to its opener on close
    (`source/features/command-palette.ts`, `RestoreFocus`), which can land after the panel
    mounts and steal focus back. Order matters here;
  - reopen after close: same action, the thread is restored from `AskConversationThreads`;
  - expand / fullscreen: `ToggleAskExpandActionSet` only flips `IsPanelExpanded`, so the panel
    stays mounted. It is not an open, but the expand button takes focus when clicked, so decide
    and record whether focus returns to the box (lean: yes);
  - there is no dedicated Ask keyboard shortcut today. Ctrl-K plus the "ask" row is the only
    keyboard path. If one exists by implementation time, it is another path.
- Close: `CloseAskPanelActionSet` (`agent-surface-state.close-ask-panel.cs`), called from the
  header close and Esc in `AgentAsk.razor`, and from the sign-in redirect.
- Approval cards: `FunctionApprovalBlock` renderer in `AgentAsk.razor` shows Approve / Reject
  buttons while `ApprovalStatus.Pending`; `TurnInProgress` is true for `Streaming` and
  `AwaitingInput`.
- JS rules: TS under `source/` compiles to `wwwroot/js`; `Spa` is a plain-object namespace
  (`source/spa.ts`) and `InvokeVoidAsync` resolution needs plain objects. `ask-ai.ts` is
  imported on demand through `ask-ai-js-module.cs`, so no new namespace is needed.

## Requirements

- Every open path (header button, Ctrl-K palette row, reopen after close, any keyboard shortcut
  that exists then) leaves the caret in the composer textarea, so typing works at once with no
  click. After a first open, a second open must also focus.
- After sending a message, focus stays in (or returns to) the composer, including after the turn
  finishes and the transcript re-renders.
- On close, focus returns to the element that opened the panel (header button, or the element that
  had focus when a keyboard path opened it). If that element is gone, fall back to the page
  `main` region or body; never leave focus on a removed node. Record the opener when the panel
  opens, not by re-querying at close.
- Do not steal focus when an approval card needs a decision: while a turn is `AwaitingInput` with a
  Pending approval, neither the open-time focus nor the post-send focus moves focus away from the
  card. Record the chosen behaviour in Results (lean: leave focus where it is; after the person
  decides, return focus to the composer).
- Do not steal focus from a deliberate move: a rebuild (route or edit-mode change) never refocuses
  unless the open or send request is still pending; the pending flag is one-shot, like
  `EditModeFocusPending`.
- Signed-out, unconfigured and error states have no textarea. The pending request must not
  throw and must not stick, so a later successful build does not focus unexpectedly after the
  person moved on. Decide and record: lean is to drop the request once the build settles.
- Focus is a side effect of the open action's handling and render, not a page-local method that
  bypasses State actions. Shape (the implementer confirms against the repo's rules, TWA0026 and
  the "all user interactions are State actions" rule): the open action records the request
  (an opener token and a pending-focus flag on `AgentSurfaceState`), and `AgentAsk`'s
  `OnAfterRenderAsync` consumes it on the open transition once the textarea exists, calling
  `AskAiJsModule.FocusComposerAsync` (annotated `[DirectComponentSideEffect]` like
  `ApplyAvatarAsync`). Opener capture and close-time return are the same pattern in `ask-ai.ts`
  (capture `document.activeElement` on open, restore on close, one-shot, like
  `command-palette.ts` `RestoreFocus`). Resolve the palette ordering so the Ask focus wins over
  the palette's focus return.
- The library gives no focus API. Do not fork, reflect into or decompile `MessageInput`. If the
  DOM selector proves fragile, record it and file the upstream ask for a focus parameter.

## Tests (don't mock your friends)

- Playwright in `tests/container-apps/web/web-spa-playwright-tests/`, the Ask lane next to
  `ask-surface-playwright-tests.cs` and `ask-sign-in-playwright-tests.cs`. For each open path
  (header button, Ctrl-K row, close and reopen, expand toggle): assert
  `document.activeElement` is `.twe-agent-ask .sc-ai-input__textarea` (use a retrying expectation,
  the textarea can mount a render late), then `Keyboard.TypeAsync` with no click and assert the
  textarea value.
- After send: focus is in the composer. On close: `document.activeElement` is the opener
  (`AskAiButton`). Approval pending: focus is not moved to the composer (use the existing approval
  fixture in the Ask lane).
- Workers must not run `dev run` (worktree AppHosts share user secrets).
- Gates: `dev check-version` (bump `<Version>` and the platform pins in the same commit),
  `dev build` 0 warnings and 0 errors, `dev test`, `ganda repo audit`.

## Goal (implementer restatement)

Whenever the Ask panel opens (header button, Ctrl-K row, reopen), the caret lands in the
composer textarea so the person types at once. Focus comes back to the composer after a send
once the turn settles, and after the expand toggle. Close hands focus back to whatever opened
the panel (or `main`/body when that element is gone). Focus is never pulled away from a pending
approval card, from a deliberate move (route or edit-mode rebuild), or after a no-textarea state
(signed out, not configured, error) settles. The request is one-shot State (`OpenAskPanel` sets
it, `AgentAsk` consumes it once the textarea exists), and DOM focus goes through `ask-ai.ts`
because the library `MessageInput` has no focus API.

Acceptance: Playwright proves focus + keyboard typing for header open, Ctrl-K open, close and
reopen, and expand; focus in the composer after a send; focus on `AskAiButton` after close; the
composer does not take focus while an approval is pending. Gates: `dev check-version`,
`dev build` 0/0, `dev test`, `ganda repo audit`.

## Checklist

- [x] Confirm `MessageInput` has no focus hook (public API only) and record it; reuse
      `FocusComposer`
- [x] State: open action records the opener and a one-shot pending-focus request; close clears it
- [x] `AgentAsk.OnAfterRenderAsync` consumes the request once the textarea exists (handle the
      late-mount and no-textarea states)
- [x] Palette ordering: Ask focus wins over the palette's focus return
- [x] Focus stays in the composer after send
- [x] Close returns focus to the opener, with a fallback when it is gone
- [x] Approval-pending: no focus steal; record the behaviour
- [x] Decide and record the expand-toggle behaviour
- [x] Playwright tests for each path, send, close and approval
- [x] Gates: `dev check-version`, `dev build` 0/0, `dev test`, `ganda repo audit`

## Session

- Created: 1872794 (2026-10-16)
- Specified: 2026-10-17 (cockpit request from Steve; read-only investigation, no code changed)
- Implemented: 2026-10-17 (ganda task work, implement oracle; Claude Opus 5.5)

## Notes

- Sequencing: run AFTER task 303-001 merges, in one serial stack with task 380 (both touch
  `AgentAsk.razor`; use one `ganda task work-set` stack, never parallel workers). This is stated
  here and not as a `## Depends on` line, because ganda task 412 (predecessor check bug) could
  block claiming after the merge.
- Design questions for Steve if expand-toggle focus, the approval behaviour or the
  no-textarea-state behaviour is contested: one question at a time.

## Results

### What changed

- `AgentSurfaceState.ComposerFocusPending` (new, one-shot): `OpenAskPanel` sets it on every open,
  `CloseAskPanel` clears it, and the new `ClearComposerFocus` action (palette opt-out) lets the
  panel drop it. It is state, not a panel field, because `TimeWarpPage` remounts `AgentAsk` on
  navigation and a remount must not refocus.
- `AgentAsk.razor`: first render with a pending request calls `CaptureOpener`.
  `SettleComposerFocusAsync` (end of `OnAfterRenderAsync`) consumes the request only when the render
  that just ran drew the agent that is still built (handles the late mount), then calls
  `FocusComposerOnOpen` and clears it. The conversation going `Streaming` and the expand button
  each set a local flag that focuses the composer softly once the turn is idle. Disposing a closed
  panel calls `RestoreOpener`. All JS calls are `[DirectComponentSideEffect]` helpers.
- `ask-ai.ts`: `CaptureOpener`, `FocusComposerOnOpen` (retries up to 30 animation frames),
  `FocusComposerInPanel` (soft: only when focus is in the panel or on nothing), `RestoreOpener`
  (opener if connected, else `main` with `tabindex=-1`, else blur a removed node).
- Palette ordering: new `source/features/focus-return-claim.ts` defines one document event.
  `CaptureOpener` raises it on every open. `command-palette.ts` hands over its pending return
  target when Ask found nothing focused, and forgets it either way, so the palette's
  `RestoreFocus` never lands on top of the composer, whichever OnAfterRender runs first. After the
  Ctrl-K row, the Ask opener is the element focused before Ctrl-K.

### Decisions (recorded)

- **No library focus hook.** Confirmed from the public `Microsoft.AspNetCore.Components.AI` XML
  (0.1.0-preview.1.26466.103): `MessageInput` exposes no `ElementReference`, `Focus` or autofocus.
  Focus uses the existing `.twe-agent-ask .sc-ai-input__textarea` selector (now one constant in
  `ask-ai.ts`). It has been stable since task 307. No upstream ask filed yet; file one if the
  library renames the class.
- **Approval pending:** nothing moves focus while the turn is `AwaitingInput`. The open request
  waits, and the post-send focus waits for the turn to go idle. After the person decides, the turn
  streams and goes idle, and the composer takes focus (soft form) unless the approved tool
  navigated away (a remount, which never refocuses).
- **Expand toggle:** returns focus to the composer (soft form; the expand button is in the panel).
- **No-textarea states** (signed out, not configured, error): the request is dropped as soon as the
  panel renders one of them, so a later Retry or sign-in does not pull focus. While the probe is
  unknown or the agent is still starting, the request waits.
- **Deliberate moves:** a route or edit-mode rebuild never refocuses (no pending request).
  Post-send and expand use the soft form, so focus the person moved outside the panel is never
  pulled back.
- **Close:** focus returns to the opener captured on open, falling back to `main`. This runs from
  `Dispose` when `IsPanelOpen` is false, so it covers the close button, the settings-menu close and
  the sign-in close. A navigation remount (panel still open) does not restore.
- Esc: the kitchen mentions Esc closing the panel, but `AgentAsk.razor` has no Esc handler today
  (only the reference picker and tag pick mode handle Esc). Nothing was added for it.

### Gates

- `dev build`: 0 warnings, 0 errors.
- `dev test`: 23 of 23 projects passed (web-spa-playwright-tests 21/21).
- Negative control: with the `FocusComposerOnOpenAsync` call commented out, the new
  `Composer_Should_HaveFocus_OnEveryOpen_AfterSend_And_ReturnItOnClose` and the updated
  `Home_Should_Point_Passkeys_To_Settings_And_Navigate_After_Approval` both failed at their
  open-focus assertions. Restored afterwards.
- `dev check-version`: source 2.0.0-beta.20 is already ahead of NuGet 2.0.0-beta.19, no bump needed.
- `dev template-smoke` (default variant): succeeded.
- `ganda repo audit`: clean (see commit).

### How to validate

- Smoke: `dev build && dev test --no-build --group playwright` (runs
  `ask-focus-playwright-tests.cs` and `ask-sign-in-playwright-tests.cs`). Or by hand (not in a
  worker: `dev run` shares user secrets): open Ask with the top-bar button and type without
  clicking; send with the arrow button; press Expand; close; reopen; sign in and open Ask via
  Ctrl-K "ask"; ask "Where can I add a passkey?" then "Take me there".
- Expect: the caret is in the message box after every open (button, Ctrl-K, reopen), after the
  answer lands and after Expand; Close leaves focus on the "Ask {name}" top-bar button; while the
  navigate approval card waits, the message box does not take focus.
