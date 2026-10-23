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

## Checklist

- [ ] Experiment: drawer without the move, in Playwright Chromium; record results in `## Results`
- [ ] Remove the move, or keep it with the real cause recorded and safe removal proven
- [ ] Playwright: navigate away with the drawer open (no console error, no orphaned dialog)
- [ ] Slim `drawer.ts`; each remaining branch has a reason in the Design region
- [ ] Existing drawer render tests and `drawer-playwright-tests.cs` still pass
- [ ] Gates: `./bin/dev build` 0 warnings and 0 errors; `./bin/dev template-smoke`; `ganda repo audit`

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-23), from the 379-008 merge review (Steve asked to file it).

## Notes

- Files: `components/elements/Drawer.razor`, `Drawer.razor.css`, `services/drawer-js-module.cs`,
  `source/features/drawer.ts`, `wwwroot/css/app.css` (`body:has(dialog.twe-drawer:modal)`),
  `tests/container-apps/web/web-spa-playwright-tests/drawer-playwright-tests.cs`,
  `tests/container-apps/web/web-spa-integration-tests/components/drawer-render-tests.cs`.
- Keep the 379-008 contracts: the caller owns `IsOpen`; Escape, backdrop click, the close button
  and a native close the module did not start all raise `OnClose` once; `Show` after `Close`
  stays shut; RTL and reduced motion behave as they do now.
- No patches to FluentUI internals.
