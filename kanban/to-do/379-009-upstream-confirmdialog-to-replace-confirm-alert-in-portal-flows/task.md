# Upstream ConfirmDialog to replace confirm alert in portal flows

## Description

Destructive actions need a confirmation step that is accessible, themed, and works with the
State rule that every interaction is an action. This task adds a brand-neutral `ConfirmDialog`
to the template, built on a native modal `<dialog>` like the 379-008 Drawer.

What Crunchit actually has: there is no `ConfirmDialog` component in Crunchit's web-spa, and no
`alert()` call. The title "replace confirm/alert" describes a gap. Three page handlers call the
browser's `window.confirm` through `IJSRuntime.InvokeAsync<bool>("confirm", message)` and then
carry on in the same method:

- `features/staff/pages/StaffPage.razor.cs`, `RevokeInviteAsync`: "Revoke the invite for {name}?
  Their one-time link will stop working and the staff record will be removed."
- `features/clients/pages/ClientDetailPage.razor.cs`, `CancelInviteAsync`: "Cancel the pending
  QuickBooks invitation for this client?"
- The same file, `DisconnectAsync`: "Disconnect this client's QuickBooks? Revenue data will stop
  syncing until they re-authorize."

Each is `bool ok = await confirm(...); if (!ok) return;` followed by the destructive command.
Nothing is themed, labelled, or testable, the text is a plain string, the browser blocks the
thread, and the dialog cannot show a danger tone or a safe default button. So this task designs
the component from the three usages, not from an existing Crunchit component. (Crunchit's
`role="alert"` uses in `App.razor` and `ForbiddenPage.razor` are status text, not dialogs.)

What the template does today:

- No component confirms anything. `window.confirm` appears once, in
  `source/features/web-mcp-page-load.ts` ("Allow navigate to {url}?"). That is an agent-initiated
  navigation permission written in TypeScript with a Playwright test
  (`web-mcp-page-load-playwright-tests.cs`). It is not a Blazor flow and stays out of scope.
- `features/identity/components/CredentialList.razor` has an inline two-step revoke: a danger
  button sets a component-local `ConfirmingId`, a block restates the credential, and only
  "Confirm revoke" raises `OnRevoke` (task 248-001). It has render tests and `data-qa` hooks
  (`RevokeConfirm`, `RevokeConfirmText`, `RevokeCancel`). Used by `PasskeysPage` and
  `SettingsPage` (passkeys and Entra accounts), whose `ConfirmRevokeAsync` then dispatches
  `RevokeCredential`.
- `features/admin/ai/pages/AiPage.razor` has "Clear key" (`XaiApiKeyClear`), which dispatches
  `ClearXaiApiKey` with no confirmation at all. It removes the site's xAI secret, so it is the
  clearest unprotected destructive action.
- No other admin page deletes or revokes anything (roles, principals, feedback, authentication
  settings only create, set, or change).
- The only overlay state is `ApplicationState.ActiveModalId` (`SetActiveModal`, `CloseModal`),
  read by `ModalContainer` (a div overlay with no Escape, focus move, or inert background) and
  by the Drawer. There is no `IDialogService` use in the app.

The Drawer (379-008) is the model for the element: `components/elements/Drawer.razor`, its
`.razor.css`, `services/drawer-js-module.cs`, and `source/features/drawer.ts` (native `<dialog>`
and `showModal()`, caller owns `IsOpen`, `OnClose` is the only signal, `[DirectComponentSideEffect]`
opt-out). Steve chose native `<dialog>` over `FluentDialog` for the Drawer. This task keeps that
choice.

Component shape:

- Parameters: `IsOpen` (read-only), `Title` (required), `Message` (`string?`) and `ChildContent`
  (either; the child content wins when set), `ConfirmLabel` ("Confirm"), `CancelLabel`
  ("Cancel"), `Tone` (`ConfirmDialogTone.Default` or `Destructive`), `Class`,
  `AdditionalAttributes` (`CaptureUnmatchedValues`, splatted on the root like `Card`), and the
  callbacks `OnConfirm` and `OnCancel`. No `IsOpenChanged`, no result `Task`, no service.
- Root: `<dialog class="twe-confirm-dialog">` with `role="alertdialog"`, `aria-modal`,
  `aria-labelledby` (the level-2 title) and `aria-describedby` (the message). Ids are minted per
  instance as in the Drawer. A destructive tone adds `twe-confirm-dialog--destructive` and styles
  the confirm button with the existing danger treatment (`fluent-button.twe-button-danger`, token
  `--twe-danger`).
- Buttons: Cancel and Confirm in a footer, both `FluentButton`. Initial focus goes to Cancel for
  the destructive tone (the safe choice) and to Confirm for the default tone. Escape raises
  `OnCancel`. Backdrop click raises `OnCancel`. Confirm raises `OnConfirm` and nothing else:
  the component never closes itself.
- Tab stays inside, focus returns to the opener on close, and the background is inert. The native
  dialog supplies these (the Drawer added a Tab wrap for `fluent-button` shadow hosts; 408 decides
  whether it stays).

## Decision

- **Native `<dialog>`, caller owns the flag** (default chosen for this task): same contract as
  the Drawer. The component does not read `ApplicationState`. Open state is a store value the
  page passes in, and both buttons are callbacks. It is a leaf in `components/elements/`.
- **`alertdialog` semantics** (default chosen): `role="alertdialog"` on the dialog, labelled by
  the title and described by the message. A confirmation interrupts and needs an answer, which
  is what `alertdialog` means; the Drawer is plain `dialog`.
- **Safe default focus** (default chosen): Cancel for the destructive tone. The first
  implementation tries the `autofocus` attribute on the Cancel `FluentButton`. If the custom
  element does not take it, the module focuses Cancel after `showModal` (the Drawer already waits
  for `fluent-button` to be defined).
- **Dismiss paths** (default chosen): Escape and backdrop click both mean Cancel. A destructive
  confirm is never the result of an accidental dismiss.
- **No `Busy` parameter** (default chosen): the caller closes the dialog in the same handler that
  dispatches the action. A busy state can come later with a real need.
- **No port of `window.confirm` text.** Messages are written for the adoption sites, plain and
  specific ("Clear the xAI API key? The Ask panel stops working until a new key is saved.").
- **No client names** in generated code, comments, or the style-guide text.
- **Share the dialog script after 408** (Steve, 2026-10-23): this task starts only after 408
  has merged. 408 settles whether the Drawer's move onto `document.body` and its Tab wrap are
  needed, and slims `drawer.ts`. This task then extracts the slimmed module into one generic
  modal-dialog module (for example `source/features/modal-dialog.ts` and
  `services/modal-dialog-js-module.cs`), moves the Drawer onto it, and builds ConfirmDialog on
  the same module. One script for both, no second copy. Keep whatever 408 proved and nothing
  else. A ConfirmDialog-only need (focus Cancel for the destructive tone) is an option on the
  shared module, not a fork of it.
- **State shape: per-feature pending target plus `ActiveModalId`** (default chosen for this
  task). A State handler cannot await a dialog, so asking and confirming are two actions. The
  opener dispatches an action that sets any pending target in the feature store and calls
  `SetActiveModal(id)`. The page passes `IsOpen="@(ApplicationState.ActiveModalId == id)"`.
  `OnConfirm` dispatches the real action and `CloseModal`. `OnCancel` dispatches `CloseModal` and
  clears the pending target. "Clear key" has no target, so it needs only the modal id. Not a
  generic `ConfirmationState` that stores and replays an action (fights typed ActionSets and
  TWA0022), and not a target encoded in the modal id string.
- **Adoption: style guide plus the AI key "Clear key"** (default chosen). CredentialList's inline
  two-step revoke (task 248-001, covered by render tests) stays as it is; moving it is its own task.
- **One Playwright test and static render tests**, as for the Drawer: HtmlRenderer cannot see
  focus or keys.

## Checklist

- [ ] `components/elements/ConfirmDialog.razor` and `.razor.css`; `confirm-dialog-tone.cs`
      (kebab file name, TW0001). Native root `twe-confirm-dialog`, parameters as in Description.
  - [ ] Closed: dialog in the tree, no title text and no buttons. Open: `role="alertdialog"`,
        `aria-modal`, `aria-labelledby` to the title id, `aria-describedby` to the message id.
  - [ ] Message from `Message` or `ChildContent`; the description id exists only when there is
        content to describe.
  - [ ] Destructive tone adds `twe-confirm-dialog--destructive` and the danger confirm button.
  - [ ] Ids per instance (as in `Drawer.razor`), no static counter.
  - [ ] Confirm raises `OnConfirm` only. Cancel, Escape, backdrop click raise `OnCancel`. The
        component never closes itself.
  - [ ] `[DirectComponentSideEffect("...")]` reason, Purpose and Design regions (TWA0004).
- [ ] Shared module: extract the post-408 `drawer.ts` and `DrawerJsModule` into a generic
      modal-dialog module and wrapper; move `Drawer.razor` onto it with no behaviour change (the
      Drawer render and Playwright tests still pass unchanged). ConfirmDialog binds the same
      module: Show, Close, Dispose, `cancel` held with `preventDefault`, backdrop click, and an
      option to focus Cancel for the destructive tone. A deferred show must not open after
      Close or Dispose. A native close the module did not start raises the cancel path; a close
      it starts does not.
- [ ] Isolated CSS with `--twe-*` tokens, no hex, no `<style>`: centered panel, width and padding
      from tokens, `::backdrop` scrim token from 379-008 (add a token only if missing), footer
      with the buttons right-aligned and a gap, destructive title/accent from `--twe-danger`.
      Narrow screens: near full width. Fade or scale in under no `prefers-reduced-motion`.
      Body scroll lock: widen the existing `body:has(dialog.twe-drawer:modal)` rule in `app.css`
      to include `dialog.twe-confirm-dialog:modal`.
- [ ] State: for the AI key flow, a request action that calls
      `SetActiveModal` (no pending field is needed, the action has no target) and a confirm
      handler that dispatches `ClearXaiApiKey` then `CloseModal`. No page-local flag. The page
      handlers only dispatch.
- [ ] Adopt on `features/admin/ai/pages/AiPage.razor` ("Clear key" asks first; message names what
      stops working). Keep `data-qa="XaiApiKeyClear"` on the opener; add `ConfirmClearXaiKey` and
      `CancelClearXaiKey` hooks. Update any Playwright or render test that clicks Clear and
      expects an immediate clear.
- [ ] Style guide: a "Confirmations" card after "Drawers" with a default-tone opener and a
      destructive opener, each through `SetActiveModal` and `ActiveModalId`, `CloseModal` on
      both callbacks. No new local field. Do not extend the StyleGuidePage contract (379-017).
- [ ] Row in `components/overview.md`.
- [ ] Render tests `components/confirm-dialog-render-tests.cs` (model: `drawer-render-tests.cs`):
      closed hides the title; open carries `twe-confirm-dialog`, `alertdialog`, `aria-modal`, and
      matching label and description ids; two instances differ; destructive modifier class;
      default labels and custom labels; `Class` and `data-qa` merge on the root; Cancel button
      is present and comes before Confirm in the DOM (focus order for the safe default).
- [ ] Playwright test in `tests/container-apps/web/web-spa-playwright-tests/`
      (`confirm-dialog-playwright-tests.cs`, model: `drawer-playwright-tests.cs`): on the style
      guide, open the destructive dialog and check focus lands on Cancel, Tab and Shift+Tab stay
      inside, Escape cancels and focus returns to the opener, Confirm calls the confirm path
      once, and the dialog matches `:modal` and sits above the shell.
- [ ] Gates:
  - `./bin/dev build` reports 0 warnings and 0 errors.
  - `./bin/dev template-smoke` passes.
  - `ganda repo audit` passes.
  - `git diff origin/master -- source tests | rg -i crunchit` prints nothing.

## Session

- Spec drafted: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-23)

## Notes

- Crunchit source (read only):
  `/home/steve/worktrees/github.com/Crunchitfs/crunchit/master/source/container-apps/web/projects/web-spa/`,
  `features/staff/pages/StaffPage.razor.cs` and `features/clients/pages/ClientDetailPage.razor.cs`.
  There is no Crunchit `ConfirmDialog`. The task describes the gap the three `confirm` calls leave.
- Depends on 408 (`kanban/to-do/408-drawer-verify-the-move-onto-documentbody-and-slim-the-drawer-script`).
  Do not start until 408 has merged. Build on what 408 left in `drawer.ts`.
- Boundaries with other children: the Drawer is 379-008 (done); shell slots are 379-015; the
  StyleGuidePage contract is 379-017. `ModalContainer` is not changed.
- Out of scope: `web-mcp-page-load.ts` (agent navigation permission), CredentialList's inline
  two-step revoke, and the Drawer's own gaps.
- The browser `confirm` is blocking; the replacement is two actions (ask, then confirm). Pages
  must not try to await a result from the component.
