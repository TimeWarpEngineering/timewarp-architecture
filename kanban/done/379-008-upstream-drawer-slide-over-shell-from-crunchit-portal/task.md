# Upstream Drawer slide-over shell from Crunchit portal

## Goal (implementer restatement)

A generated app needs a brand-neutral slide-over so a detail or edit form can sit beside the current page. This task adds that shell as a native modal `<dialog>` (`twe-drawer`). It is not a FluentDialog wrapper, and it does not reuse the existing div modal.

The drawer is a leaf. It shows a required title, body content, and an optional footer. It takes a read-only `IsOpen` and an `OnClose` callback, plus which side it sits on (end by default, or start), a size (medium by default, or wide), `Class`, and unmatched attributes. It does not two-way bind open state, and it does not read `ApplicationState`. The caller owns the flag. On the style guide, the opener dispatches `SetActiveModal` with a drawer id, `IsOpen` compares `ActiveModalId` to that id, and both close paths dispatch `CloseModal`.

While open, the root is a dialog labelled by its level-2 heading. One small script calls `showModal()` and `close()`, and moves the dialog onto `document.body` first because the shell is `display: contents`. The browser supplies the top layer, an inert background, and focus return to the opener. Chromium does not wrap Tab out of a Fluent button's shadow root, so the script wraps Tab. Escape (the `cancel` event, held so the caller still owns the flag), a backdrop click, and the header dismiss button all raise `OnClose`. The panel slides in unless the user prefers reduced motion. Closing is immediate. Document scroll locks with one global `body:has(dialog.twe-drawer:modal)` rule.

The only adoption is the style guide: a Drawers card after Empty States, with one opener, a short body, and a footer close button. Static render tests pin the markup. One browser test on that card pins focus moving in, Escape, focus returning to the opener, and Tab staying inside.

Done means the component, the side and size enums, the script and its C# wrapper, token-only CSS, the style-guide card, the overview row, the render tests, and the browser test are in place; `./bin/dev build`, `./bin/dev template-smoke`, and `ganda repo audit` pass; and the `source` and `tests` diff contains no portal product name. The existing modal overlay stays unchanged. It still has no Escape or focus handling, and this task records that gap instead of fixing it.

The shell still has to meet the earlier review contract. A show that is still waiting for the dismiss button's custom element must leave the dialog closed when `Close` or `Dispose` has already run. End and Start sit on the inline edges: the panel, its inner border, and the slide follow the writing direction, and under a right-to-left document the slide comes from the opposite physical side. A native close this script did not start, including a form with `method="dialog"` inside the body, calls the same `OnClose` path so the caller's flag and the style-guide modal id clear. A close this script does start — `Close`, `Dispose`, or moving the dialog onto `document.body` — must not call `OnClose` again. The browser fires that `close` event on a later turn, so the skip has to survive past the return of `close()`. The browser check waits for that event before it reads the host calls, and it counts the native close on a cleared list so a leaked module close cannot satisfy it.

## Description

Port Crunchit's `Drawer` into the template as a native, brand-neutral slide-over shell, so a
generated app can show a detail or edit form beside the page without leaving it.

Crunchit's `Drawer` (`components/Drawer.razor`, `.razor.css`, plus a script block in
`wwwroot/index.html`) works like this:

- Parameters: `IsOpen` and `IsOpenChanged` (a two-way bind), `Title` (required), `Footer`
  (`RenderFragment?`), `ChildContent`. Nothing else: no side, no width, no size, no `Class`.
- Markup, rendered only while open: a scrim `<div>` (click closes), then
  `<aside role="dialog" aria-modal="true" aria-labelledby=title-id tabindex="-1">` holding a
  header (`<h2>` title and a `×` close button with `aria-label="Close"`), a scrolling body, and
  an optional footer. The title id comes from a static counter.
- CSS: fixed to the right edge, full height, 560px wide (100vw under 600px), flex column,
  scrim at z-index 199 and panel at 200. It uses hex and `rgba` literals, a hand-drawn shadow, and
  `crunchit-*` variables. There is no slide animation, in spite of the name.
- Open and close: the parent owns the state. The drawer never sets `IsOpen` itself. It calls
  `IsOpenChanged(false)` on scrim click, close button, and Escape.
- Escape and focus: a `@onkeydown` on the panel handles Escape once focus is inside. After each
  render a JS helper (`crunchit.setDrawerSession`, inline in `index.html`) adds a capture-phase
  `document` keydown listener that calls back into `[JSInvokable] OnEscape`, locks scroll on
  `<body>` and the main region, and focuses the first focusable control in the body (else the
  panel). The component implements `IAsyncDisposable` to undo the session.
- What is missing: focus is not restored to the opener on close, focus is not contained (Tab
  leaves the panel), and the background is not inert (a screen reader can still read the page).
  The `×` glyph is the button label's visual only.
- Uses: the staff page (invite form and member detail, one drawer switched by a local
  `DrawerMode` enum, open flag held in the page), and a sample in the style guide. The shell
  also has an unrelated sidebar-toggle state that only shares the word "drawer".

Template today: there is no drawer. The overlay that exists is the store-driven modal
(`ApplicationState.ActiveModalId`, `ModalController`, `ModalContainer`, `twe-modal`). It has a
backdrop click and a dialog role, but no Escape, no focus move, no focus return, and no inert
background. `CommandPalette` and `AgentAsk` add their own focus handling with a
`[DirectComponentSideEffect]` opt-out (TWA0026). FluentUI is 5.0.1. It ships `FluentDialog`
(Modal, `Alignment` Start/End, `PreventDismissOnEscape`, `Show/HideAsync`),
`FluentDialogBody` (with `FixedHeaderFooter`), and `IDialogService.ShowDrawerAsync`. It has no
`FluentDrawer` component. The tree already has `FluentProviders`
(`layouts/FluentUIRequiredFeatures.razor`). No other panel or slide-over pattern exists.

Defects to fix in the port:

- Focus is not restored to the opener, not contained, and the background is not inert.
- No `Class` or attribute passthrough, hex literals instead of tokens, and `crunchit-*` names.
- The two-way bind plus a page-local `DrawerOpen` bool does not satisfy the rule that every
  interaction is a State action (see Decision).
- Open and close are not animated, and there is no `prefers-reduced-motion` handling.
- No width option. The 560px width is fixed.

Follow the conventions of 379-001 through 379-007:

- A native root with a `twe-` class (`twe-drawer`).
- A `Class` parameter and `[Parameter(CaptureUnmatchedValues = true)] AdditionalAttributes`,
  splatted on the root, as `Card.razor` does.
- No `DisplayComponent` or `CssBuilder`.
- Isolated `.razor.css` with `--twe-*` tokens from `wwwroot/css/tokens.css`. No hex values, no
  `<style>`. Add tokens only if needed (panel width, scrim colour, drawer z-index).
- Purpose and Design regions (TWA0004).
- Static `HtmlRenderer` Jaribu tests in
  `tests/container-apps/web/web-spa-integration-tests/components/`, modelled on
  `empty-state-render-tests.cs`.
- The component exposes only `EventCallback`s. A page handler only dispatches
  TimeWarp.State actions (`tw-blazor`). No page-local navigate, service or JS methods.
- No TypeScript patches of library internals (`tw-blazor-css-strategy`, memory). Any script is
  the component's own presentational module, not a patch of FluentUI.
- The generated code contains no client name.

## Decision

- **Open state is a State action, not a bind** (default chosen for this task): the component takes
  `IsOpen` (read-only `bool`) and `OnClose` (`EventCallback`). There is no `IsOpenChanged` and
  no `@bind-IsOpen`. The page computes `IsOpen` from a store and `OnClose` dispatches a close
  action. For the style guide and any page with no store of its own, use the existing
  `ApplicationState.ActiveModalId`: the opener dispatches `SetActiveModal` with a drawer id,
  `IsOpen="@(ApplicationState.ActiveModalId == DrawerId)"`, `OnClose` dispatches `CloseModal`.
  That also gives "one overlay at a time" for free. The component itself stays a leaf and does
  not reference `ApplicationState`.
- **Slots and parameters** (default chosen): `Title` (required), `ChildContent`, `Footer`,
  `Side` (`DrawerSide.End` default, `Start`), `Class`, `AdditionalAttributes`. Width comes from
  one token with a `Size` of `Medium` (default) and `Wide`; do not copy the fixed 560px as a
  literal. No `IsOpenChanged`.
- **Close paths** (default chosen): scrim click, the header close button, and Escape all call
  `OnClose`. The close button is a `FluentButton` with a FluentUI dismiss icon and
  `aria-label="Close"`, replacing the `×` glyph.
- **Motion** (default chosen): a short slide-in using a transform, off under
  `prefers-reduced-motion`. Closing is immediate, because the parent removes the drawer.
- **No Crunchit `DrawerMode` enum port.** A page that needs two drawers uses two ids.

- **Native `<dialog>`** (Steve, 2026-10-23): `<dialog class="twe-drawer">` opened with
  `showModal()` and closed with `close()` through one small presentational TypeScript module.
  The browser supplies the top layer, inert background, Tab containment, Escape (the `cancel`
  event, forwarded to `OnClose`) and focus return to the opener. `::backdrop` is the scrim. Body
  scroll lock is one global rule, `body:has(dialog.twe-drawer:modal)`. The component carries a
  `[DirectComponentSideEffect]` opt-out like `CommandPalette`. Not a `FluentDialog` wrapper, and
  not a port of Crunchit's `<aside>` plus scroll-lock helper.
- **One Playwright test** (default chosen for this task): HtmlRenderer cannot see focus or keys,
  and the accessibility behaviour is the point of the component, so one browser test pins
  focus-in, Escape, focus-return and Tab containment on the style-guide drawer.

## Adoption site

Style guide only, like 379-005. The only candidate is the admin feedback item
(`features/admin/feedback/pages/FeedbackAdminItemPage.razor`, opened from the grid link
`/Admin/Feedback/{id}`), but that is a routed page with a permalink, a loaded-by-id state flow
and its own Section layout. Turning it into a slide-over would drop deep links and the
`OpenFeedbackForAdmin` page flow for no gain, which is not an honest fit. Crunchit's staff
invite and detail forms have no counterpart in the template. A later task can adopt the
drawer where a real in-page edit form appears.

## Checklist

- [x] `components/elements/Drawer.razor` and `.razor.css`. It goes under `elements/`: the drawer
      owns no state and holds no child components of its own, as `Card` and `EmptyState` do.
  - Root and parameters as in Decision. Keep the `<dialog>` closed when `IsOpen` is false. Title id unique per instance (no static counter shared across
    circuits; derive from the instance).
  - Root labelled by its title: `role="dialog"` (implicit on `<dialog>`), `aria-modal`, and
    `aria-labelledby` pointing at the title element's id. Heading level is fixed at 2.
  - Footer renders only when set. The body is the only scroll area; header and footer stay
    pinned.
  - Escape, scrim click, and the close button call `OnClose`.
- [x] `components/elements/drawer-side.cs` and, if kept, `drawer-size.cs` for the enums
      (kebab file names, TW0001).
- [x] The presentational module in TypeScript under `source/features/` (compiled to
      `wwwroot/js`, like `web-authn.ts`), with a C# wrapper in `services/` like
      `command-palette-js-module.cs`, and a `[DirectComponentSideEffect("…")]` reason on the
      component. Dispose the module reference and guard `JSDisconnectedException`. No edits to
      FluentUI internals.
- [x] CSS in the isolated stylesheet: fixed to the chosen side, full height, width from a token,
      `--twe-paper`, `--twe-shadow`, `--twe-rule`/border token, spacing from `--twe-space-*`,
      header title at 600 in `--twe-ink`, a body that scrolls, a footer with a top border and a
      flex gap. Narrow screens: full width. Slide-in via transform, disabled under
      `prefers-reduced-motion`. Add tokens (`--twe-drawer-width`, `--twe-scrim`, a drawer z
      layer) to `tokens.css` only if needed. No hex values and no `<style>`.
- [x] Style guide: add a "Drawers" card (after "Empty States") with one opener button and one
      drawer containing a short body and a footer with a close button. The opener dispatches
      `SetActiveModal`, `IsOpen` reads `ActiveModalId`, `OnClose` and the footer button
      dispatch `CloseModal`. No new local field. Do not extend the StyleGuidePage contract
      (379-017).
- [x] Add a row to `components/overview.md`.
- [x] Render tests in `components/drawer-render-tests.cs`. They pin only what the static
      render shows:
  - Closed: no panel markup (or a closed dialog), and no title text.
  - Open: root carries `twe-drawer`, the side modifier, the title, `aria-modal`, and
    `aria-labelledby` equal to the id of the title element.
  - Two instances get different title ids.
  - Footer is omitted when unset and present when set.
  - `Class` and additional attributes (`data-qa`) merge on the root.
  - The close button has `aria-label="Close"` and the icon is `aria-hidden`.
- [x] One Playwright test in
      `tests/container-apps/web/web-spa-playwright-tests/` (model: `ask-focus-playwright-tests.cs`
      and its sign-in helper) for focus-in, Escape, focus-return, and Tab containment on the
      style-guide drawer.
- [x] A deferred `show` does not call `showModal` after `Close` or `Dispose`.
- [x] End and Start use inline insets and borders, and the slide flips under `:dir(rtl)`.
- [x] A native close the module did not start calls `RequestClose`.
- [x] A close the module starts does not call `RequestClose` after the queued `close` event.
      The Playwright check waits for that event, and counts the native close on a cleared list.
- [x] Gates:
  - `./bin/dev build` reports 0 warnings and 0 errors.
  - `./bin/dev template-smoke` passes.
  - `ganda repo audit` passes.
  - `git diff origin/master -- source tests | rg -i crunchit` prints nothing.

## Accessibility

| Requirement | Crunchit port (rejected) | FluentDialog (rejected) | Native `<dialog>` (chosen) | Render test can pin it? |
|---|---|---|---|---|
| `role="dialog"`, `aria-modal` | build | library | native | yes (markup) |
| Labelled by the title | build | build (title template) | build | yes (id match) |
| Escape closes | build (JS) | library (`PreventDismissOnEscape` false) | native `cancel`, forward it | no |
| Focus moves in on open | build (JS) | library | native | no |
| Focus returns to opener | build (not done in Crunchit) | library | native | no |
| Focus contained | build (not done in Crunchit) | library | native | no |
| Background inert | build (not done in Crunchit) | library ("windows under a modal dialog are inert") | native | no |
| Body scroll lock | build (JS) | check | one `:has()` rule | no |
| Reduced motion | build | build | build | CSS only, not by render test |

HtmlRenderer sees markup only: roles, attributes, ids, classes, slot presence. It cannot run
JavaScript, move focus, press keys, or know what is inert. The Playwright suite exists in the
repo (`web-spa-playwright-tests`, with focus tests for the Ask panel), so a browser test is
possible, and this task adds one.

## Session

- Spec drafted: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-23)
- Implemented the native dialog drawer, the style-guide card, the render tests, and the Playwright focus test (2026-10-23). The shell is `display: contents`, so the module moves the dialog onto `document.body` before `showModal`. Chromium still does not wrap Tab out of a `fluent-button` shadow host, so the module wraps Tab.
- Implement oracle: implemented by implementer-cursor (cursor, model grok-4.7-high), session not reported, max-turns uncapped (no --max-turns flag; bound 500), Done — 2026-10-22T20:37:52Z
- Review oracle round 1 (2026-10-23). Reviewer: general (claude-opus-5-5) at effort 3; the session id is not exposed in print mode. Gates re-run: build 0/0, render tests 7/7, Playwright 1/1, template-smoke passed, audit passed, portal-name grep printed nothing. Findings: 0 bug, 2 suggestion, 2 nit. M4 is wontfix; M1, M2, and M3 are open and need a fix pass. Files: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`.
- Review oracle: review by reviewer-claude (claude, model claude-opus-5-5), session not reported, max-turns 200 — 2026-10-22T20:46:14Z
- Fix pass for review round 1 (2026-10-23). M1: `wantOpen` cancels a deferred `showModal` after `Close` or `Dispose`. M2: End and Start use inline insets and borders, and `:dir(rtl)` plays the slide from the opposite physical side. M3: a `close` the module did not start calls `RequestClose`. M4 stays wontfix. Gates: build 0/0, render tests 7/7, Playwright 1/1, template-smoke succeeded, audit 32 passed, portal-name grep printed nothing.
- Review oracle round 2 (2026-10-23). Reviewer: general (claude-opus-5-5) at effort 2, reviewing the fix delta `138a8cbea..93befc533`. The session id is not exposed in print mode. M1 and M2 are fixed, and M3's listener landed. New: M5 (bug): `moduleClosing` resets before the queued `close` event fires, so every module `Close` also calls `RequestClose`. This was reproduced against the compiled `drawer.js` in Playwright Chromium. M6 (bug): the Playwright check reads the result before that event fires. Gates were not re-run this round. A fix pass is needed for M5 and M6. Files: `review/round-2/general.md`, `review/round-2/merged.md`.
- Fix pass for review round 2 (2026-10-23). M5: `pendingModuleClose` is taken by the queued `close` listener, so `Close`, `Dispose`, and the move onto `document.body` do not call `RequestClose`. A `method="dialog"` close still does. M6: the Playwright check waits for that `close` event before it reads the host-call count, and it counts the native close on a cleared list. Gates: build 0/0, render tests 7/7, Playwright 1/1, template-smoke succeeded, audit 32 passed (1 skipped), portal-name grep printed nothing.
- Review oracle round 3 (2026-10-23). Reviewer: general (claude-opus-5-5) at effort 1, reviewing the fix delta `5d03e880b..92103e67a`. The session id is not exposed in print mode. M5 and M6 are fixed. In Playwright Chromium against the compiled `drawer.js`: `Close`, `Dispose`, and the `placeOnBody` close made no host call. A native close made exactly one, including one queued behind a module close. No new findings. Gates were not re-run this round. Disposition: accepted-exceptions (M4 wontfix), 0 open. Files: `review/round-3/general.md`, `review/round-3/merged.md`, `review/disposition.md`.

## Results

`components/elements/Drawer.razor` is a native `<dialog class="twe-drawer">`. Open state stays with the caller (`IsOpen` and `OnClose`). The style guide Drawers card, after Empty States, opens it through `SetActiveModal` and `ActiveModalId`, and both close paths dispatch `CloseModal`. Width comes from `--twe-drawer-width` and `--twe-drawer-width-wide`. The scrim is `--twe-scrim`. There is no drawer z-index token: a modal dialog paints in the top layer. Body scroll locks with `body:has(dialog.twe-drawer:modal)`.

`ModalContainer` is unchanged. It still closes on a backdrop click and exposes `role="dialog"`, and it still has no Escape handling, no focus move, no focus return, and no inert background.

The script keeps a `wantOpen` flag. `Show` sets it, and `Close` and `Dispose` clear it, so a `show` that is still waiting on `fluent-button` returns without `showModal` after the caller has closed. End and Start use `inset-inline-*` and `border-inline-*`. Under `:dir(rtl)` the slide comes from the opposite physical side, and `prefers-reduced-motion` still drops it. A `close` event the module did not start, such as a form with `method="dialog"`, calls `RequestClose`. A close the module starts increments `pendingModuleClose` before `close()`. The browser queues that event, and the listener takes one count and returns, so `Close`, `Dispose`, and the move onto `document.body` do not call `RequestClose`.

### How to validate

Smoke: `./bin/dev build`; `./bin/dev template-smoke`; `ganda repo audit`; `dotnet test -c Debug --nologo -- --filter-class Drawer_Should_` in `tests/container-apps/web/web-spa-integration-tests`; `dotnet test -c Debug --nologo -- --filter-class StyleGuideDrawer_Given_Wasm` in `tests/container-apps/web/web-spa-playwright-tests`; `git diff origin/master -- source tests | rg -i crunchit`.

Expect: build reports 0 warnings and 0 errors. Template smoke succeeds. Audit passes. The seven render tests pass (closed dialog hides the title, open markup carries `twe-drawer`, the side class, `aria-modal`, and a title id match, two instances differ, the footer is optional, `Class` and `data-qa` land on the root, and the dismiss icon is `aria-hidden` with `aria-label="Close"`). The Playwright test passes: the header close control receives focus, Tab reaches the footer close and wraps back to the header, Escape removes the open dialog, and focus returns to the opener. End sits on the right in LTR and the left in RTL, Start is the mirror, and the inner border follows the inline edge. A `method="dialog"` submit removes the title and leaves the dialog shut. A show that resolves only after `Close` stays shut. After the queued `close` event, a module `Close` has asked the host zero times. A native `close`, counted on a cleared list, asks once. The portal-name search prints nothing.

### Review

Round 1 found M1, M2, and M3, and the first fix pass implemented them. M4 stays wontfix. Round 2 confirmed that M1 and M2 are fixed and that M3's listener landed. It opened two bugs. The second fix pass closed them, and round 3 confirmed the fixes:

- M5: `pendingModuleClose` counts a close the module starts. The listener takes that count when the queued `close` event fires, so `OnClose` does not run a second time and a later `SetActiveModal` is left in place.
- M6: the Playwright check waits for that event before it reads the host-call count, and it counts the native close on a cleared list.

- Round 1: general reviewer, effort 3. M1 (suggestion, a deferred `showModal` could open after `Close`), M2 (suggestion, End and Start used physical CSS), and M3 (nit, no `close` listener) are implemented. Wontfix: M4 (nit, interop on every render; it keeps the dialog in sync and costs little). No bugs.
- Round 2: general reviewer, effort 2, fix delta only. M5 and M6 (bugs) are implemented in this pass. Suggestion 2 fixed; nit 1 fixed and 1 wontfix.
- Round 3: general reviewer, effort 1, fix delta only. M5 and M6 were re-verified as fixed in Chromium against the compiled module. No new findings.
- Final counts: bug 0 open / 2 fixed; suggestion 0 open / 2 fixed; nit 0 open / 1 fixed / 1 wontfix.
- **Disposition: accepted-exceptions.** 0 open. The one exception is M4 (nit, wontfix; the review oracle decided it in round 1). No escalations.
- Paths: `review/review-framework.md`, `review/round-3/merged.md` (final ledger), `review/disposition.md`.

## Review

- Round 1: M1, M2, and M3 implemented in the fix pass. M4 wontfix.
- Round 2: M5 and M6 implemented in the fix pass.
- Round 3: Done
## Notes

- Crunchit source is under
  `/home/steve/worktrees/github.com/Crunchitfs/crunchit/master/source/container-apps/web/projects/web-spa/`:
  - `components/Drawer.razor(.css)`.
  - `wwwroot/index.html` (inline `crunchit.setDrawerSession` and `setBodyScrollLocked`).
  - Usages: `features/staff/pages/StaffPage.razor(.cs)` and the style guide sample. The
    `features/application/**/*sidebar-drawer*` files are the mobile nav toggle, not this component.
- Template targets:
  - `source/container-apps/web/projects/web-spa/components/elements/`
  - `features/style-guide/pages/StyleGuidePage.razor`
  - `wwwroot/css/tokens.css`
  - `features/application/components/ModalContainer.razor` (existing overlay; do not extend it
    in this task, and say in Design how the drawer differs from it).
- Boundaries with other children: the confirm dialog is 379-009 and may share the overlay
  base chosen here; the shell slots are 379-015; the StyleGuidePage contract is 379-017. This
  task does not touch `ModalContainer` or the shell.
- The template's modal has no Escape or focus handling. That is a separate gap; note it in the
  Results rather than fixing it here.
