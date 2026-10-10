# Task 302: Fix Feedback Details textarea still narrow and resize grip not working (FluentTextArea shadow root)

## Description

Follow-up to task 299 (PR #459, merged as `650fa24f2`). Steven rebuilt and ran master
(`490b95d22`, which contains #459) on WebAssembly. The Feedback page Details field is **still**
a small box, about 200px wide and 100px tall, while Kind and Title span the whole form. Since #459
it also shows a vertical resize grip that does not actually resize anything.

#459's Playwright check passed anyway, because it measured the wrong element.

### Root cause found while filing (verify, don't assume it is complete)

Fluent UI Blazor is `Microsoft.FluentUI.AspNetCore.Components` **5.0.0**. `FluentTextArea` renders:

```
<fluent-field style="width: {Width}">            <- Width="100%" lands here
  <fluent-textarea style="height: {Height}" resize="vertical" data-qa="FeedbackBody" ...>   <- Height="9rem" lands here (host)
    #shadow-root
      <label part="label">
      <div class="root" part="root">              <- the VISIBLE bordered box
        <textarea class="control" part="control">
```

The web component's own shadow styles (in `staticwebassets/Microsoft.FluentUI.AspNetCore.Components.lib.module.js`,
search `--inline-size: 18rem`) are roughly:

```
:host { display: inline-block; --min-block-size: 52px; --block-size: var(--min-block-size);
        --inline-size: 18rem; --resize: none; ... }
:host([block]:not([hidden])) { display: block; }
:host([resize='vertical']) { --resize: vertical; }
.root { inline-size: var(--inline-size); min-block-size: var(--min-block-size);
        block-size: var(--block-size); resize: var(--resize); contain: paint layout style var(--contain-size); ... }
:host([block]) .root { inline-size: auto; }
.control { field-sizing: content; max-block-size: 100%; resize: none; ... }
```

So:

- #459's scoped CSS (`.feedback-details ::deep fluent-textarea { display:block; width:100%; min-height:9rem }`)
  and `Width="100%"` / `Height="9rem"` only stretched the **host** element (and the `fluent-field`).
  The visible `.root` inside the shadow DOM keeps `inline-size: var(--inline-size)` = 18rem and
  `block-size` ~52px, so the user still sees a small box. The host is a big, invisible, empty rectangle.
- The resize grip belongs to `.root` (`resize: vertical` from `resize="vertical"`). With the host's fixed
  `height: 9rem` from `Height`, and `.root`'s `block-size`/`contain` rules, dragging it doesn't
  usefully resize the field (verify the exact reason in the browser).
- The #459 Playwright check in `tests/container-apps/web/web-spa-playwright-tests/feedback-attachment-playwright-tests.cs`
  (~line 80-85) compares `BoundingBoxAsync()` of `[data-qa=FeedbackBody]`, i.e. the **host**, against Title.
  The host was 100% wide, so it passed while the visible box stayed narrow. The integration test
  `tests/container-apps/web/web-spa-integration-tests/features/feedback/feedback-details-layout-tests.cs`
  only string-matches the CSS/razor, which proves nothing about layout.

### Likely idiomatic fix (verify in the browser)

Use the component's public styling API rather than fighting the host size:

- Set the `block` attribute on `fluent-textarea` (FluentTextArea passes unknown attributes through
  `AdditionalAttributes`), so `:host([block])` is `display:block` and `.root` uses `inline-size: auto`
  (fills the host), **and/or** set the documented CSS custom properties from scoped CSS with `::deep`:
  `--inline-size: 100%` and `--min-block-size: 9rem` on `fluent-textarea`.
- Drop `Height="9rem"` (fixed host height fights the resize) in favor of `--min-block-size: 9rem`, so the
  visible box starts at ~9rem and can grow.
- Keep `Resize="TextAreaResize.Vertical"` only if dragging the grip really changes the visible height
  in Chromium (Playwright mouse drag). If it can't be made to work cleanly, remove `Resize`
  (or use `AutoResize`) and say so in Results.
- Use `::part(root)` / `::part(control)` only if the custom properties are not enough.
- Follow `skills/tw-blazor-css-strategy` (scoped `.razor.css`, `::deep`, no inline `<style>`, no new global CSS).
- Remove the #459 host-only rules that become redundant. Keep `.feedback-details` as the paste
  listener host from task 295 (paste/upload must keep working).

## Requirements

1. On `/Feedback` (InteractiveWebAssembly), the **visible** Details box (the shadow `part="root"` element,
   and the inner `part="control"` textarea) has the same left and right edges as the Title field's
   visible input box (within ~4px), at the default Playwright viewport and also at a narrow viewport
   (e.g. 600px wide).
2. Its visible height starts at about 9rem (>= 140px at 16px root font size).
3. Vertical resize actually works: a Playwright mouse drag on the root's bottom-right corner increases
   the visible root height by roughly the drag distance. Or, if resize can't work cleanly, resize is
   removed and no grip is shown (assert `resize` computes to `none` on the root) and the decision is in Results.
4. Replace the #459 width assertion so it measures the shadow-DOM visible box, e.g.
   `page.Locator("[data-qa=FeedbackBody]").EvaluateAsync("el => el.shadowRoot.querySelector('[part=root]').getBoundingClientRect()...")`
   (or Playwright's shadow-piercing CSS locator `[data-qa=FeedbackBody] [part=root]`) compared with the Title
   field's visible input box (`[data-qa=FeedbackTitle]` shadow root / `input`), not the hosts.
5. Prove the new test catches the bug: run it against the old CSS/razor (git stash or temporarily revert
   to `490b95d22`'s FeedbackListPage.razor + .razor.css) and record the failing numbers in Results, then
   the passing numbers after the fix.
6. Replace or delete the string-matching `feedback-details-layout-tests.cs` assertions that encode the
   broken approach (host width/min-height). Don't keep tests that only grep CSS for layout claims.
7. Save a screenshot of the Feedback form after the fix (full form visible, Details box at full width)
   as `feedback-details-302.png` in this task's kanban folder (same `ScreenshotPath` pattern as the
   295 tests, pointed at the `302-*` folder), and commit it with the task.
8. Full build (0 warnings) and full test suite (unit, integration, Playwright/WASM) green locally and in CI.

## Checklist

- [x] Reproduce in Playwright: record host box vs shadow root box vs Title box on current master
- [x] Fix with the component's styling API (block attr / --inline-size / --min-block-size), scoped CSS
- [x] Make vertical resize work, or remove it with a recorded reason
- [x] Playwright test measures the shadow root/control box vs Title, at default and narrow widths
- [x] Show the test fails on the old CSS (numbers in Results)
- [x] Replace the string-matching layout test
- [x] Screenshot `feedback-details-302.png` in the task folder
- [x] Paste/upload on Details still works (295 tests green)
- [x] Full build and tests green

## Session

- Created: 218251 (2026-10-11)
- Implement: 2026-10-11
- Review: 2026-10-11, review oracle (Claude Opus 5.5), effort 2, roster general

## Notes

- Reported by Steven 2026-10-11 ~00:30 BKK with screenshots: Details ~200x100px, Kind/Title full width,
  resize grip visible but not working. Running build InformationalVersion `2.0.0-beta.20+490b95d22`.
- Related: task 299 (PR #459) made the host full width; task 295 added the paste/upload host `.feedback-details`.

## Results

The visible Details box is the shadow `part="root"`, not the `fluent-textarea` host. On the pre-fix page the host was already the full column (934×144) while the bordered root stayed 288px wide (18rem). Right edges differed by 646px, which is why the #459 host `BoundingBox` check passed.

Vertical resize stays. Dropping `Height="9rem"` and setting `--min-block-size: 9rem` gives the root a definite 9rem block size, so Chromium's grip changes that box. An 80px drag grew the root from 144px to 224px. The `block` attribute plus `--inline-size: 100%` makes the root fill the field. `.feedback-details` is still the paste host.

### Measured boxes

Old CSS (test failed, default 1280). Shouldly right-edge delta 646 against a limit of 4:

`viewport=default-1280 host=934x144 detailsRoot=293..581 288x144 titleRoot=293..1227 934x32 detailsControl=304..570 266x130 titleControl=304..1216 912x30`

After the fix (same test passed). Edges match exactly at both widths. The narrow pass is after the drag, so its height is the grown box:

`viewport=default-1280 host=934x144 detailsRoot=293..1227 934x144 titleRoot=293..1227 934x32 detailsControl=304..1216 912x130 titleControl=304..1216 912x30`

`drag=80 before=144 after=224 grown=80`

`viewport=narrow-600 host=494x224 detailsRoot=53..547 494x224 titleRoot=53..547 494x32 detailsControl=64..536 472x210 titleControl=64..536 472x30`

Screenshot: `feedback-details-302.png` in this folder.

### Tests

- `./bin/dev build`: 0 warnings, 0 errors.
- `VisibleDetailsBox_Should_MatchTitle_And_Resize` failed on the old markup (numbers above) and passed after the fix.
- `PasteAndUpload_Should_ShowAttachmentsOnTheItem` passed (shadow-box check included).
- `Details_Host_Stays_The_Paste_Target` passed. It only checks the paste wrapper (`feedback-details`, `DetailsHost`, no in-file `<style>`).
- `./bin/dev test`: Playwright 7/7 and the projects that do not need Docker passed in the first run. `api-server-integration-tests`, `aspire-tests`, `web-infrastructure-tests`, and `web-spa-integration-tests` failed in that process with `permission denied` on `/var/run/docker.sock` because the session lacked the `docker` supplementary group. Rerun with `sg docker`: 1, 38, 68, and 184 passed, 0 failed.
- `ganda repo audit`: 31 passed, 0 failed.

### Review

- Rounds: 2. Effort 2, one general reviewer.
- Final counts: 1 bug fixed, 1 suggestion fixed, 0 nits, 0 open, 0 wontfix.
- Disposition: **clean**.
- M1 (bug): `ScreenshotPath` threw in generated apps because the template ships `tests/` but not `kanban/`. It now falls back to the test output folder.
- M2 (suggestion): the attachment test now calls `OpenFeedbackAsync` instead of repeating the goto-and-wait code.
- After the fixes: `./bin/dev build` 0 warnings, Playwright 7/7, `ganda repo audit` passed.
- Artifacts: `review/review-framework.md`, `review/round-2/merged.md`, `review/disposition.md`.

### How to validate

**Smoke**

1. Rebuild and open `/Feedback` signed in, InteractiveWebAssembly.
2. The Details border lines up with the Title input on the left and the right, and the box is about 9rem tall with a grip at the bottom-right corner.
3. Drag that grip downward. The bordered box grows with the pointer.
4. Narrow the window to about 600px. Details still shares Title's left and right edges.
5. Paste an image into Details and pick a file. Both still show up as attachments.

**Expect**

- The bordered box is the full field width, not an 18rem strip inside a wider host.
- An 80px downward drag grows the visible root by about 80px (Playwright recorded 144 → 224).
- Paste and file upload still attach files. The wrapper class `feedback-details` and `@ref="DetailsHost"` are still the paste host.
