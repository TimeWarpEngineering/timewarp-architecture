# Drawer: verify the move onto document.body and slim the drawer script

## Description

379-008 (PR #562) shipped `components/elements/Drawer.razor` on a native `<dialog>` with a
TypeScript module, `source/container-apps/web/projects/web-spa/source/features/drawer.ts`
(211 lines), and its wrapper `services/drawer-js-module.cs`.

Before `showModal()`, the module moves the dialog onto `document.body` (`placeOnBody`). Its
Design region says the reason is that the app shell is `display: contents`, and a modal dialog
under that ancestor does not go into the top layer, so Tab leaves the panel. That claim is
unverified. As far as we know, `showModal()` puts the dialog in the top layer whatever its
ancestors' `display` is (only `display: none` on an ancestor prevents rendering).

Moving a Blazor-rendered element out of its rendered parent is risky. Blazor keeps the node
reference, so attribute and child updates still apply. But when the component is removed (for
example by navigating away while the drawer is open), Blazor removes the node through its
logical parent. That can throw, or leave the dialog behind on `document.body`. The Playwright
test does not cover leaving the page with the drawer open.

The script also grew well past the "few lines that wrap platform calls" the 379-008 Decision
described. Some of the size comes from the move (a close the module starts during the move has
to be counted so it does not raise `OnClose`).

## Goal

1. Find out whether the move is needed. In Chromium (the Playwright project), render the
   style-guide drawer without `placeOnBody` and check: the dialog matches `:modal`, sits in the
   top layer (paints above the shell and header), the background is inert, and focus
   containment behaves the same as with the move.
2. If the move is not needed: remove `placeOnBody` and every bit of bookkeeping that exists
   only for it, and correct the Design region.
3. If the move is needed: record the real cause in the Design region (with the evidence), make
   sure removal is safe, and add a Playwright test that navigates away while the drawer is open
   and asserts no console error and no `dialog.twe-drawer` left in the document.
4. Either way, slim `drawer.ts`. Keep only behaviour the browser does not give. Re-check
   whether the hand-written Tab wrap is still needed after step 2 or 3, and whether the wait for
   the `fluent-button` custom element is still needed. Record in Design why each remaining
   piece exists.

## Goal (implementer restatement)

The drawer moves its `<dialog>` onto `document.body` before `showModal()`, on the claim that the
app shell's `display: contents` keeps a modal out of the top layer so Tab can leave the panel.
That claim has not been checked in this browser, and relocating a Blazor node can throw or leave
the dialog behind when the page goes away while it is open. The script is also larger than the
few platform calls the drawer decision described, partly because the move has to ignore the close
it starts.

I will open the style-guide drawer in this repo's Playwright Chromium with that move turned off
and record whether the dialog matches `:modal`, paints above the shell and header, leaves the
rest of the page inert, and keeps focus inside the way the moved dialog does. If it does, I will
delete the move and every bit of bookkeeping that exists only for it, and correct the Design
regions. If it does not, I will record the real cause with that evidence, make removal safe, and
add a Playwright test that leaves the page while the drawer is open and sees no console error and
no leftover `dialog.twe-drawer`. Either way I will cut `drawer.ts` down to behavior the browser
does not already give, re-check the hand-written Tab wrap and the wait for `fluent-button`, and
say in Design why each remaining piece is there.

The caller still owns `IsOpen`. Escape, a backdrop click, the close button, and a native close
the module did not start each raise `OnClose` once. A `Show` that is already in flight when
`Close` runs stays shut. RTL and reduced motion stay as they are. No FluentUI internals change.
The drawer render tests, the Playwright drawer test, `./bin/dev build` (0 warnings, 0 errors),
`./bin/dev template-smoke`, and `ganda repo audit` pass.

## Checklist

- [x] Experiment: drawer without the move, in Playwright Chromium; record results in `## Results`
- [x] Remove the move, or keep it with the real cause recorded and safe removal proven
- [x] Playwright: navigate away with the drawer open (no console error, no orphaned dialog)
- [x] Slim `drawer.ts`; each remaining branch has a reason in the Design region
- [x] Existing drawer render tests and `drawer-playwright-tests.cs` still pass
- [x] Gates: `./bin/dev build` 0 warnings and 0 errors; `./bin/dev template-smoke`; `ganda repo audit`

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-23), from the 379-008 merge review (Steve asked to file it).
- Implementer: removed `placeOnBody` after the Chromium probe below. The Tab wrap stays. The `fluent-button` wait and `wantOpen` are gone because FluentUI's `beforeStart` defines that element before `Show`.

## Results

`drawer.ts` leaves the dialog where Blazor rendered it, and `showModal` puts that dialog in the top layer. `placeOnBody`, the close it started, and the `wantOpen` / `customElements.whenDefined("fluent-button")` deferral are gone. The Tab wrap stays. The old "Show after Close stays shut" contract was the deferred `showModal` that ran after `fluent-button` became defined. `Show` is synchronous, so `Close` cannot be overtaken by a `showModal` the module already started. A later `Show` from the caller opens the dialog.

### Experiment (Playwright Chromium, style-guide drawer, move removed)

HeadlessChrome/153.0.8010.12. The open dialog's parent was the card body, not `document.body`. Ancestors included `DIV.twe-shell` with computed `display: contents`.

- `dialog.matches(":modal")` was true.
- `elementFromPoint` on the panel where it covers the app bar hit the drawer's header, inside `dialog.twe-drawer`.
- `elementFromPoint` on the scrim (left side, mid-viewport) hit that same dialog. `::backdrop` background was `color(srgb 0.101961 0.113725 0.156863 / 0.48)`.
- `opener.focus()` did not move focus. Focus stayed inside the dialog. This Chrome's `querySelector(":inert")` throws (`:inert` is not a valid selector) and `element.inert` stays false for the opener, so inertness is the focus check.
- The dismiss control was `FLUENT-BUTTON` with an open shadow root. `customElements.get("fluent-button")` was already defined when `Show` ran. FluentUI 5.0.1 `beforeStart` calls `defineComponents()` before the app starts.
- With the module's keydown listener skipped, Tab from the footer button left the dialog: `FLUENT-BUTTON|DrawerFooterClose|inside -> BODY||outside`. With the listener, Tab wrapped back to the header close button, and Shift+Tab wrapped to the footer close button.

### What remains in `drawer.ts` (182 lines, from 211)

- `showModal` / `close`, no-op when the dialog is already in that state. `Show` is synchronous.
- `cancel` with `preventDefault`, and a backdrop click, both call `RequestClose` and do not close here. The caller owns `IsOpen`.
- `pendingModuleClose` so `Close` and `Dispose` do not raise `OnClose`. A native close (a form with `method="dialog"`) raises it once.
- Capture-phase Tab / Shift+Tab wrap, because this Chrome does not wrap Tab out of `fluent-button`.
- `Dispose` removes the listeners and closes an open dialog so navigation returns focus.

### Tests

- `StyleGuideDrawer_Given_Wasm`: passed. Adds the top-layer checks above, Shift+Tab back to the footer, and `Blazor.navigateTo('/')` while the drawer is open (no console error, no `dialog.twe-drawer` left).
- `Drawer_Should_`: 7 passed.
- `./bin/dev build`: 0 warnings, 0 errors. `./bin/dev template-smoke`: succeeded (SmokeDefault OK; generated Jaribu aggregators 405, 9, and 14). `ganda repo audit`: 32 passed, 0 failed.

### How to validate

**Smoke**

```bash
cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Debug --nologo -- --filter-class Drawer_Should_
cd tests/container-apps/web/web-spa-playwright-tests && dotnet test -c Debug --nologo -- --filter-class StyleGuideDrawer_Given_Wasm
```

**Expect**

- Render tests: 7 passed. Closed markup has no title; open markup is a `dialog.twe-drawer` labelled by its `h2`.
- Playwright: 1 passed. The style-guide drawer matches `:modal` while it is still under `.twe-shell` (`display: contents`), paints over the app bar, and `opener.focus()` leaves focus in the panel. Tab wraps to the header close button and Shift+Tab wraps to the footer close button. Escape returns focus to the opener. A `method="dialog"` submit drops the title. `Close` does not call `RequestClose`; a native `dialog.close()` calls it once. Navigating to `/` with the drawer open logs no console error and leaves no `dialog.twe-drawer`.

**Automated gate**

```bash
./bin/dev build
./bin/dev template-smoke
ganda repo audit
```

## Notes

- Files: `components/elements/Drawer.razor`, `Drawer.razor.css`, `services/drawer-js-module.cs`,
  `source/features/drawer.ts`, `wwwroot/css/app.css` (`body:has(dialog.twe-drawer:modal)`),
  `tests/container-apps/web/web-spa-playwright-tests/drawer-playwright-tests.cs`,
  `tests/container-apps/web/web-spa-integration-tests/components/drawer-render-tests.cs`.
- Keep the 379-008 contracts: the caller owns `IsOpen`; Escape, backdrop click, the close button
  and a native close the module did not start all raise `OnClose` once; `Show` after `Close`
  stays shut; RTL and reduced motion behave as they do now.
- No patches to FluentUI internals.
