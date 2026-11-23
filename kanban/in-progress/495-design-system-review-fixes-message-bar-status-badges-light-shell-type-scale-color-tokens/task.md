# Design system review fixes: message bar, status badges, light shell, type scale, color tokens

## Goal (implementer restatement)

The Design System page is the reference for components the rest of the app uses, and seven
approved defects there are wrong on the real shell too. Message bars should use the same soft
status tints as the badges, info should read as info (blue, not a gray icon on a near-black
fill), and Copy or Dismiss should sit on the same row as one line of text. The status sample
should drop the Lost pill, because there is no Lost tone, and keep Error with a real label such
as Churned. In the light shell, the Member name should use the app bar's foreground so it is
readable on the white bar, and nav items should rest with no fill in both themes while hover and
active keep theirs. The type-scale rows should use the same weight, case, and tracking as the
real text, with one shared eyebrow style for the catalog headings and the KPI labels. Color
chips should stay visible when the token matches the card, and each chip should show the value
the current theme resolves. Changing design-system part should scroll the content pane back to
the top. The parts list should be only as tall as the space left in the viewport, so the last
group is not trapped under the footer.

## Description

Design review of the developer-only Design System page (`/DesignSystem`,
`source/container-apps/web/projects/web-spa/features/design-system/`). That page defines the
components every real page uses, so a component flaw here repeats across the app. The fixes are
to shared components and the app frame, plus the page's own reference accuracy.

Evidence is in `assets/`. Each screenshot is from https://arch.timewarp.work/DesignSystem at
1440x816, with red boxes and notes drawn on the live page. Steven approved items 1, 2, 4 and 5 on
2026-11-17. He asked for evidence on 3, 6 and 7 before deciding.

### 1. Message bar component (approved)

`assets/01-message-bars-dark.png`. Files: `components/MessageBar.razor`, `components/MessageBars.razor`,
`features/design-system/samples/MessageBarsSample.razor`.

- Dark fills are solid and saturated: success `rgb(5,37,5)`, error `rgb(63,16,17)`, warning
  `rgb(65,18,0)`. Status badges use a soft tint plus a colored dot (`--twe-*-tint`).
- Info is `rgb(20,20,20)` with a gray icon. Nothing marks it as info. The info badge is blue.
- Every bar is `layout="multiline"`, so Copy (and Dismiss in the shell region) sits alone on a second
  row. Each bar is 76px tall for one line of text. Put the actions inline at the right.
- The same bars render in the shell notification region (seen live from the Notifications triggers).

### 2. Status badges: drop the "Lost" sample (approved)

There is no Lost tone. `BadgeStatus` (`components/elements/badge-status.cs`) has Active, Inactive,
Warning, Info, Error. The sixth pill in `StatusBadgesSample.razor` is
`<StatusBadge Status="BadgeStatus.Error" Label="Lost" />`, which is why Error and Lost look the same.

Origin (task 379-012): the customer mockup
`crunchit/master/Documentation/Design/crunchit-portal.html` names the red `--lost: #C2342A` and its
style guide swatch is "Lost / Error", one color. Its `.badge--lost` class is labelled **Churned**
(client status) or **Auth error** (QBO connection), never "Lost". 379-012 chose one enum value for
lost and error with the label carrying the word.

No product page uses Error or Lost today.

- Remove the Lost pill. Keep `BadgeStatus.Error` and `twe-badge--error` as they are.
- To still teach `Label=`, show one Error pill with a real mockup word (`Churned` or `Auth error`).
- Update `design-system-parts.cs` (usage snippet), `status-badge-render-tests.cs`, and
  `design-system-page-contract-tests.cs` (expects "Lost").

### 3. Light theme app frame (approved, Steve 2026-11-23)

`assets/03-light-shell.png` (light) next to `assets/03-dark-shell-compare.png` (dark). Files:
`components/TimeWarpPage.razor`, `components/NavMenu.razor(.css)`.

- The "Member" label in the app bar is white text on a white app bar (`rgb(255,255,255)` on
  `rgb(255,255,255)`). It is there but cannot be seen.
- Every nav item (`a.fluent-navitem`) has a gray fill `rgb(240,240,240)`. Dark items have none.
- Decision (2026-11-23): the Member label uses the app bar's foreground token so it reads in both
  themes. Nav items rest with no fill in both themes: remove the light `rgb(240,240,240)` resting
  fill rather than add one to dark. FluentUI nav items rest transparent, and a fill on every item
  flattens the active-item highlight; hover and active keep their fills.
- Dropped from the first review: the gray pill on the theme button was the mouse hover left by the
  click, not a defect.

### 4. Type Scale reference (approved)

`assets/04-type-scale.png`. File: `features/design-system/samples/TypeScaleSample.razor(.css)`.

- Every sample row renders at weight 600 and line-height 1.1, with no case change.
- Real helper text (`twe-form-field__hint`) is 12px / 400. The "12 — Helper text" sample is bold.
- Real eyebrows are uppercase with letter spacing: catalog headings 11px / 700 / 0.44px, KPI labels
  11px / 600 / 0.66px. The "11 — Section eyebrow" sample is neither. The two real eyebrows also
  disagree with each other; pick one and use it in both.
- Render each row with the class or token the real components use, so the sample cannot drift.

### 5. Color Tokens reference (approved)

`assets/05-color-tokens-dark.png`. File: `features/design-system/samples/ColorTokensSample.razor(.css)`.

- In dark, Background is `rgb(41,41,41)` and Paper is `rgb(48,48,48)`. The card is `rgb(48,48,48)`
  and the chip border is 12% white, so Paper vanishes. Use a stronger chip border or a checker/inset.
- Only the variable name is shown. Show the resolved value for the current theme (read with
  `getComputedStyle` or listed from `wwwroot/css/tokens.css`) so a developer can match the mockup.
- Consider swatches for the status tokens (positive, warning, info) the badges use.

### 6. Detail page keeps the old scroll position (approved, Steve 2026-11-23)

`assets/06-detail-scroll-before.png`, then `assets/06-detail-scroll-after.png`.

Steps: open `/DesignSystem` (All), scroll the content to 4000px, click **Status Badges** in the
parts list. The URL becomes `/DesignSystem/status-badges` but the scroll container keeps
`scrollTop` (clamped to 189, the bottom of the short page). The page title, breadcrumb, filter, and
the card heading are above the window. Fix: reset the content scroll container to the top when the
part changes.

### 7. Parts list cut off at page top (approved, Steve 2026-11-23)

`assets/07-catalog-clipped.png`. File: `features/design-system/components/DesignSystemCatalog.razor(.css)`.

At page top the parts list box is 722px tall and starts at y=212, so it ends 118px below the
816px window. Scrolled to its own end, the Identity group (User card, Sign-in, Role badges, Role
cards, Permission grid) is still hidden under the footer. It only fits after the page itself
scrolls and the sticky list moves up. Fix: size the list to the space left in the viewport
(for example `max-height: calc(100dvh - <top offset>)`), not a fixed height.

## Scope

All seven items are approved (items 3, 6, 7 by Steve, 2026-11-23: "make the decision you think is
best for the ones that are missing; let's get these done"). Screenshots 06-detail-scroll-before/after
and 07-catalog-clipped are referenced but missing from assets/; capture them in the after-pass.

## Checklist

- [x] 1. Message bar: tinted fills, info looks like info, actions inline
- [x] 2. Status badges: drop Lost pill, optional Error with a real label, update snippet and tests
- [x] 3. Light app frame: Member label color, nav item fill
- [x] 4. Type Scale: rows match real weight, case, spacing; one eyebrow style
- [x] 5. Color Tokens: visible Background/Paper chips, show values
- [x] 6. Reset scroll on part change
- [x] 7. Parts list fits the viewport
- [x] Recapture `assets/` after the fix, light and dark

## Session

- Created: ses_e1ad8b2d9ffezBxYfyQnvzq5g7 (2026-11-16)
- Implementer: the seven review fixes are in the shared components, the app frame, and the design-system page (2026-11-24).

## Results

Message bars use the status tint tokens, info is the info blue, and Copy sits on the title row. The Lost sample is gone; the error sample is labeled Churned. The Member label uses the app-bar ink, and nav items rest transparent in both themes. Type-scale rows use the same role classes as the real components, and the eyebrow is the KPI style (11px, weight 600, 0.06em, uppercase). Color chips show the computed color for the current theme, with a stronger border so Background and Paper stay visible, plus positive, warning, and info swatches. Changing the design-system part resets the shell content scroller to the top. The parts list height is the space left in the viewport. After shots, light and dark, are in `assets/08-after-*.png`. The before shots stay.

### How to validate

Smoke: sign in as a developer and open `/DesignSystem` at 1440×816. Switch light and dark. Scroll the All page, then open Status Badges. Open Message bars, Type Scale, and Color Tokens.

Expect: message bars are tinted, info is blue, and Copy is on the same row as the text. Status badges show Churned and do not show Lost. Member is readable on the app bar, and nav items have no resting fill. Type rows match the role (page title 700/28px, helper 400/12px, eyebrow 600/11px uppercase). Color chips print a resolved `rgb(...)` that changes with the theme, and Paper stays visible on the card. The content scroller is at the top after the part change. The parts list ends above the footer and scrolls on its own to Identity.

Gates:

- `./bin/dev build` — succeeded, 0 warnings, 0 errors (19.25s).
- `tests/container-apps/web/web-spa-integration-tests/components/status-badge-render-tests.cs` — not a standalone Jaribu runfile (no `#if !JARIBU_MULTI`). `cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Release --no-build --no-restore -- --filter-class StatusBadge_Should_` — passed 3/3.
- `tests/container-apps/web/web-spa-integration-tests/features/design-system/design-system-page-contract-tests.cs` — not a standalone Jaribu runfile (no `#if !JARIBU_MULTI`). `cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Release --no-build --no-restore -- --filter-class DesignSystemPage_Should_` — passed 6/6.
- Browser, in-proc host, 1440×816, light and dark: catalog stays above the footer and Identity is reachable inside the list; content `scrollTop` returns to 0 on Status Badges; Churned badge; message-bar tints with inline Copy; type roles; color values including dark paper `rgb(48, 48, 48)`; Member ink on the app bar and transparent nav rest. The temporary Playwright method used for that pass was removed. CI runs the project suites.
- `ganda repo audit` — exit 0. 43 passed, 2 skipped, 1 advisory warning: `vscode-window-icon` (`.vscode/settings.json` is missing; gitignored local state, pre-existing). `analyzer-suppressions` passed: 843 suppressions, none above the baseline.

## Notes

- Out of scope by design: sample content on the page (trigger button appearance, chart colors, grouped
  table counts, role badge tones, nested showcase cards, developer-voice copy). The page supplies those.
- Research on Lost: explore sub-agent ses_e1aad4b6cffeV9oE7elCJZRc63 (2026-11-17).
