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

## Checklist

- [ ] Confirm `MessageInput` has no focus hook (public API only) and record it; reuse
      `FocusComposer`
- [ ] State: open action records the opener and a one-shot pending-focus request; close clears it
- [ ] `AgentAsk.OnAfterRenderAsync` consumes the request once the textarea exists (handle the
      late-mount and no-textarea states)
- [ ] Palette ordering: Ask focus wins over the palette's focus return
- [ ] Focus stays in the composer after send
- [ ] Close returns focus to the opener, with a fallback when it is gone
- [ ] Approval-pending: no focus steal; record the behaviour
- [ ] Decide and record the expand-toggle behaviour
- [ ] Playwright tests for each path, send, close and approval
- [ ] Gates: `dev check-version`, `dev build` 0/0, `dev test`, `ganda repo audit`

## Session

- Created: 1872794 (2026-10-16)
- Specified: 2026-10-17 (cockpit request from Steve; read-only investigation, no code changed)

## Notes

- Sequencing: run AFTER task 303-001 merges, in one serial stack with task 380 (both touch
  `AgentAsk.razor`; use one `ganda task work-set` stack, never parallel workers). This is stated
  here and not as a `## Depends on` line, because ganda task 412 (predecessor check bug) could
  block claiming after the merge.
- Design questions for Steve if expand-toggle focus, the approval behaviour or the
  no-textarea-state behaviour is contested: one question at a time.

## Results

(none yet)
