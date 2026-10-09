# Fix prerender style flash: replace inline encoded style blocks with CSS isolation and server-rendered brand theme

## Description

On page load the app shell paints unstyled (FluentLayout's raw purple bar, a 201px search box
with "Ctrl-K" wrapping, the logo clipped off the top), then snaps to the real layout once Blazor
goes interactive. Steven reported it on 2026-10-09 with screenshots. The cause is component CSS
written as C# strings inside `<style>@(@"…")</style>`. Prerender HTML-encodes that text, so the
browser drops every rule.

Steven's direction (2026-10-09): "don't worry about the churn, this is a template and we want the
best before we replicate." Fix it with best practice, not a patch. Prerender stays on, and no
feature gets disabled.

### Root cause (reproduced 2026-10-09 on TWE-001)

- `web-spa/components/TimeWarpPage.razor:168-169` writes the shell CSS as
  `<style>@(@" … ")</style>`.
- With InteractiveAuto + `Prerender=true` (`configuration/blazor-settings.cs:17-18`), the server
  prerender emits that expression as an **encoded text node**: newlines become `&#xA;`, quotes
  become `&quot;`, and the em dash becomes `&#x2014;`.
- Browsers do **not** decode entities inside `<style>`, so the stylesheet is invalid and all rules
  are dropped. The shell has **0 rules before interactive and 24 after**.
- Visible result until interactivity: the FluentLayout purple bar, a 201px search box with
  "Ctrl-K" wrapping, and the logo clipped at y=-34.5. That lasted about 240ms on localhost and
  will be longer on a real network or for a cold WASM download.
- The same pattern appears in:
  - `features/profiles/components/Profile.razor:58-59` (0 vs 2 rules; interpolated `{Id}` form)
  - `components/forms/FormField.razor:42-43`
  - `features/identity/pages/login-page/LoginPage.razor:116-117`
  - `features/application/pages/NotFoundPage.razor:18`: a **static literal** `<style>` (no `@()`),
    so it is not encoded and not broken. Still move it, so components have no inline `<style>`.
    That page also shows "403 Forbidden" copy on the not-found page; fix that copy as a drive-by.
- History: introduced by `a2e107d7f2` (2026-06-23). It spread through `7f42d70ba` (FormField,
  task 234) and `e26265885` (LoginPage move), and because the `tw-blazor-css-strategy` skill
  teaches `<style>@(@"…")</style>` as its **canonical example**.
- **Not** caused by PRs #448–#450.
- Evidence: Playwright script `C:\Users\steve\ovn\flash.js` and screenshots in
  `C:\Users\steve\ovn\flash\` on TWE-001.

### Second issue: blue → purple theme flash

`components/layouts/MainLayout.razor:11-15` applies the brand (purple) theme only in
`OnAfterRenderAsync`. A fresh browser therefore paints the Fluent default blue first, then
switches to purple.

### Historical research: why inline `<style>` instead of `.razor.css`?

Steven remembered "an issue with CSS isolation" but not what it was. Research (read-only,
2026-10-09) found it:

1. **The repo did not abandon isolation.** It runs an "isolation-first hybrid".
   - About 22 `*.razor.css` files exist.
   - `Web.Spa.styles.css` is linked in `web-server/components/App.razor:34`.
   - Inline `<style>` is used only as "Exception B".
   - Decided in done task **059-002** (2026-06-22) and adopted from the crunchit ADR
     `Crunchitfs/crunchit` `kanban/done/022-research-css-strategy-for-components-isolated-css-pain-points.md`.
2. **The issue is "Wall A"**, from Steven's 2024-05-28 conversation with Pete (mrpmorris), quoted
   in crunchit 022:
   - An isolated `.razor.css` stamps its `[b-xxxx]` scope attribute only on **native elements the
     component itself renders**.
   - A child component's root is never stamped. That includes light-DOM ones like
     `FluentStack`/`FluentNav`/`FluentLayout`/`fluent-text-input` hosts.
   - So isolated CSS cannot target it: "you can't style it without wrapping it with a div".
   - `::deep` was rejected as "painful, slow and error-prone".
   - Inline `style=` was rejected because it is CSP-unsafe.
   - `global.css` was disliked because it has no locality.
   - Result: Steven's "old way", a co-located `<style>` scoped by `.{Id}` or a fixed root class
     (the copic pattern).
3. **Concrete recurrence:**
   - Task 234 review M1 (2026-09-17): FormField's isolated `.twe-form-field__control > *` was
     rewritten to `> *[b-0phzzyrekv]` and didn't match Fluent hosts. It was "fixed" by moving to
     Exception B inline `<style>` (`7f42d70ba`), which is now one of the flashing files.
4. **Wall B** (FluentUI primitives are open-shadow-DOM web components, reachable only via
   `::part()` + CSS custom properties) is separate and applies to every strategy.
5. **Secondary, already fixed:** before `a2e107d7f2`, the scoped-CSS pipeline was broken by
   Tailwind-era csproj plumbing (`CopyScopedCss` + dummy-CSS targets), and the host never linked
   the bundle. That commit removed the plumbing and linked `Web.Spa.styles.css`, so this is not a
   current blocker.

**Assessment:** Wall A is Blazor's documented isolation design, not a bug, so .NET 11 is not
expected to change it. Step 0 must still verify this on the current SDK. The inline `<style>`
approach was the right instinct (class-scoped, co-located), but writing CSS as a C# expression is
what breaks prerender.

## Requirements

1. **Step 0 gate: decide before coding, and record the result in this task's Results.**
   - Confirm the historical problem above (Wall A: the scope attribute is not stamped on child
     component roots) and verify on .NET 11 whether it still applies. Build a minimal repro
     component and inspect the rendered `[b-*]` attributes.
   - If Wall A is fixed or not applicable, use CSS isolation everywhere.
   - If it still applies (expected):
     - Keep isolation for components with a native root.
     - For styling FluentUI light-DOM children, pick **one**:
       - (i) isolation with a native wrapper root plus targeted `::deep`;
       - (ii) a static, class-scoped stylesheet in `wwwroot/css/` linked in `App.razor` `<head>`.
     - Choose by measured correctness and simplicity, document why, and note that (ii) brings
       back a global file, which Steven disliked. Keep it per-area, not a dumping ground.
   - Either way, every rule must be in a real stylesheet the browser loads **before first paint**.
2. **Component styles go to `.razor.css`** (bundle `{assembly}.styles.css` linked in `App.razor`
   head). Use `::deep` only where needed for FluentUI children. `AgentAsk.razor.css` is existing
   precedent.
3. **App-wide tokens and rules** (appbar height, brand colors as CSS custom properties) live in
   one global `wwwroot` stylesheet (`tokens.css`/`app.css`), linked in head **before** the
   isolation bundle.
4. **Per-instance values** (Profile's `{Id}`-based CSS) become CSS custom properties on the
   element (`style="--avatar-size: …"`), with static rules in `.razor.css`. No generated CSS.
   This is the one sanctioned use of `style=`: custom properties only.
5. **Remove every inline `<style>` block** from components: TimeWarpPage, Profile, FormField and
   LoginPage, plus the static NotFoundPage one. `MarkupString`/`(MarkupString)ShellCss` is
   **explicitly NOT the chosen fix**.
6. **The brand theme is in the first server render.**
   - No blue → purple flash.
   - The theme must not depend only on `MainLayout.OnAfterRenderAsync`.
   - Set it from the head or the prerendered layout, e.g. brand tokens or the Fluent theme
     attributes and custom properties in static CSS or the prerendered markup.
7. **Rewrite the `tw-blazor-css-strategy` skill** (repo copy `skills/tw-blazor-css-strategy/SKILL.md`):
   - Add the new rules.
   - Add the Wall A/B history and the step-0 outcome.
   - Add an explicit **ban on `<style>@(…)</style>`, or any inline `<style>`, in components**,
     with the prerender-encoding reason.
   - In Results, note that the shared Grok Bot copy at
     `/home/box/agent-data/workflows/tw-blazor-css-strategy/SKILL.md` needs the same update. The
     implementer cannot edit it; it gets synced separately.
8. **Regression tests:**
   - (a) Assert that no prerendered `<style>` contains encoded entities (`&#x`, `&quot;`, `&amp;`).
     Also, preferably, assert that components emit no `<style>` at all.
   - (b) A Playwright test with **JavaScript disabled**, or a check before interactivity, asserts:
     - `.twe-appbar` (or its replacement) is `display:flex` with the correct background;
     - the logo is visible (not clipped, top ≥ 0);
     - the search box is full width, and "Ctrl-K" does not wrap.
   - (c) The header color is the brand purple on first paint.
9. **Small item: the "AI not configured" hint** in the Ctrl-K Ask surface.
   - It currently shows
     `dotnet user-secrets set "XAI:ApiKey" ... --project source/container-apps/web/projects/web-server/web-server.csproj`.
   - That path is relative to the repo root and fails when run from the web-server folder.
     Steven hit this on 2026-10-09.
   - Make it work from any directory, preferably with
     `dotnet user-secrets set "XAI:ApiKey" "<key>" --id 0e53fdd3-6f93-4d5a-9c86-040621f7929e`,
     in pwsh-valid syntax.
   - Update any docs or tests that assert the old text.

### Acceptance

- The first paint (JS disabled, or before interactive) matches the settled layout: correct
  appbar, logo visible, full-width search, brand purple header.
- No component renders an inline `<style>`, and no prerendered `<style>` contains entities.
- Prerender stays on, and no feature is disabled to hide the flash.
- The PR includes before and after real-browser screenshots of the first paint and the settled
  state. Playwright tests pass and CI is green.

## Checklist

- [x] Step 0: verify Wall A on .NET 11 with a minimal repro; record the decision and why in Results
- [x] TimeWarpPage shell CSS → stylesheet (per step 0); inline `<style>` removed
- [x] Profile → `.razor.css` (no per-instance values existed, so no custom properties or `style=`; see Results); inline `<style>` removed
- [x] FormField → stylesheet per step 0 (Fluent host stretch still works; keep the task-234 M1 behavior); inline `<style>` removed
- [x] LoginPage → `.razor.css`; inline `<style>` removed
- [x] NotFoundPage → `.razor.css`; inline `<style>` removed (fix the 403 copy)
- [x] Tokens and app-wide rules in global stylesheet, linked before `Web.Spa.styles.css`
- [x] Brand theme in first server render (no blue → purple)
- [x] Rewrite `skills/tw-blazor-css-strategy/SKILL.md`; ban inline `<style>` in components
- [x] Test (a): no encoded entities, no `<style>` in prerendered components
- [x] Test (b): JS-off / pre-interactive appbar layout (flex, background, logo visible, search full width)
- [x] Test (c): brand header color on first paint
- [x] "AI not configured" hint works from any directory (`--id` form, pwsh)
- [x] Template copy (`TimeWarp.Architecture/` if it carries these files) kept in sync
- [ ] PR with before/after real-browser screenshots of first paint (JS off / pre-interactive) and settled state; CI green

## Session

- Created: 1058716 (2026-10-09)

## Notes

- Reproduction artifacts on TWE-001: `C:\Users\steve\ovn\flash.js`, `C:\Users\steve\ovn\flash\`.
- Sources for the history:
  - `a2e107d7f2` commit message;
  - `kanban/done/059-002-…` (decision);
  - `kanban/done/059-web-spa-migrate-to-fluentui-v5-rc-and-replace-tailwind-with-plain-css/task.md`;
  - `kanban/done/234-…/review/round-1/merged.md` (M1);
  - crunchit `kanban/done/022-research-css-strategy-for-components-isolated-css-pain-points.md`
    (the Steven ↔ Pete conversation, 2024-05-28).
- Current `App.razor` head order: `tokens.css`, `app.css`, `Web.Spa.styles.css`, the Fluent
  `bundle.scp.css`, then `ai-chat.css`.

## Results

### Step 0: Wall A on .NET 11 (decided before coding)

Minimal repro on SDK `11.0.100-rc.1.26425.128` (runtime `11.0.0-rc.1.26425.128`), FluentUI 5.0.0:
a `Repro.razor` + `Repro.razor.css` rendered with `HtmlRenderer`.

```razor
<div class="native-root">
  <FluentStack Class="fluent-child"><span class="authored-in-child-content">x</span></FluentStack>
  <FluentTextInput Class="fluent-input" />
  <Child />
</div>
```

Rendered (trimmed):

```html
<div class="native-root" b-71g489ft2r>
  <div class="fluent-stack-horizontal fluent-child" style="…"><span class="authored-in-child-content" b-71g489ft2r>x</span></div>
  <fluent-field class="fluent-input" …><fluent-text-input …></fluent-text-input></fluent-field>
  <section class="child-root">child</section></div>
<div id="…-container"><div class="fluent-layout twe-shell" style="…--layout-header-height: 44px…">…
<fluent-menu …><div slot="trigger" class="trig" b-71g489ft2r>t</div><fluent-menu-list>…
```

- **Wall A still applies on .NET 11.** The scope attribute is on elements the component authors,
  including ones inside a Fluent `ChildContent`. It is never on a child component's root:
  FluentStack's div, `fluent-field`, FluentLayout's root, `fluent-menu-list`, or `Child`'s
  `<section>`.
- `::deep` compiles as expected on .NET 11: `.a ::deep > fluent-field` →
  `.a[b-x]  > fluent-field`, `.a ::deep fluent-menu > fluent-menu-list` →
  `.a[b-x]  fluent-menu > fluent-menu-list`.
- **Decision: option (i), isolation everywhere with a native anchor + targeted `::deep`.** Every
  component keeps its CSS in its own `.razor.css` (locality, which was the reason for the old
  inline blocks), and the bundle is in `<head>`, so it applies on first paint. Where the component
  root is a Fluent component (TimeWarpPage → FluentLayout, Profile → FluentMenu) a native root div
  is added as the anchor, with `display: contents` so it adds no box. Option (ii), static
  class-scoped stylesheets in `wwwroot/css`, would also work but brings back per-area global files
  away from their components, so it was not chosen.
- Side finding: FluentUI itself emits inline `style=` attributes and a raw container-query
  `<style>` (FluentLayout, FluentStack). It is framework markup, rendered raw (not encoded).

### CSP and per-instance values (Profile)

- The app sends no Content-Security-Policy today (no CSP header or meta in `source/`). The rule
  against inline `style=` comes from the crunchit 022 research (strict CSP / locked-down browsers).
- Profile's `{Id}` CSS had **no per-instance values**; `{Id}` was only a scope handle. Isolation's
  scope attribute replaces it, so Profile.razor.css is fully static: no `style=`, no
  `style="--x: …"`, no generated CSS. Requirement 4's sanctioned `style="--avatar-size: …"` was not
  needed and was not used.
- For future genuine per-instance variation the skill now says: a class or `data-*` attribute
  selected in `.razor.css`, never generated CSS or `style=`. ForbiddenPage's inline `style=`
  attributes were moved to `ForbiddenPage.razor.css` as a drive-by.
  `StyleGuidePage.razor` (`style="background: var(@token)"`) and `ServiceList.razor` still carry
  inline `style=` and are left for a follow-up.

### What changed

- **TimeWarpPage**: native `<div class="twe-shell">` root (`display: contents`); all shell CSS in
  `TimeWarpPage.razor.css`. Fluent roots via `.twe-shell ::deep …` (layout height vars,
  header/footer frame, `.twe-nav`); breadcrumbs via `.twe-page__crumbs ::deep …`. Header frame is
  now `var(--twe-purple)` (#55409c) instead of Fluent's runtime `--colorBrandBackground` (#6b55a9),
  so it is identical before and after Fluent's script runs (same pattern the footer already used
  with `--twe-blue`). Brand and actions get `flex: 1 1 0` so the search box is centred in the bar
  regardless of side content, and an anchored `:not(:defined)` rule reserves the search field's
  32px box until Fluent defines `fluent-text-input`.
- **Profile** → `Profile.razor.css` (native `.twe-profile` root, `::deep fluent-menu > fluent-menu-list`).
- **FormField** → `FormField.razor.css` (`.twe-form-field__control ::deep > fluent-…`; task 234 M1
  host stretch kept).
- **LoginPage** → `LoginPage.razor.css` (plain isolation).
- **NotFoundPage** → `NotFoundPage.razor.css`; copy fixed to 404 / "Page not found" with a home link
  (the 403 text and dead `#` links are gone). Same Card layout as ForbiddenPage.
- **Tokens**: `--twe-appbar-height` / `--twe-footer-height` in `tokens.css` (linked before
  `Web.Spa.styles.css`).
- **Theme**: `App.razor` renders `<body data-theme-color="#55409c" data-theme="light">`
  (`MainLayout.BrandColor`). Fluent 5.0.0's `beforeStart` initializer
  (`Theme.initializeThemeSettings`) builds the brand ramp from it, so Fluent is purple from script
  start. `MainLayout`'s `OnAfterRenderAsync` `SetThemeAsync` is removed, which also proves the new
  path: the settled `--colorBrandBackground` is still the purple ramp (#6b55a9).
- **Skills**: `skills/tw-blazor-css-strategy/SKILL.md` rewritten (rules, Wall A/B, step-0 result,
  ban on any `<style>` element in components with the prerender-encoding reason, history);
  `skills/tw-blazor-layout/SKILL.md` references updated. **The shared Grok Bot copy at
  `/home/box/agent-data/workflows/tw-blazor-css-strategy/SKILL.md` needs the same update; it was
  not edited here and gets synced separately.**
- **"AI not configured" hint**: `XaiChatDefaults.SetupCommand` and dev-cli `XaiPreflight.SetupCommand`
  are now `dotnet user-secrets set "XAI:ApiKey" "<your-xai-key>" --id 0e53fdd3-6f93-4d5a-9c86-040621f7929e`
  (pwsh-valid, works from any directory). readme updated; dev-cli test pins the id to
  web-server.csproj's `<UserSecretsId>`.
- **Template copy**: the repo root is the template (`.template.config`), so there is no separate
  copy to sync.
- **CI**: `workflow.yml` uploads `artifacts/playwright/` as `playwright-<run>` (retention 1 day).

### Tests

- (a) `FirstPaint_Given_PrerenderedShell.Components_Should_AuthorNoStyleElements`: no `<style>` in
  any `source/**/*.razor` (Razor comments ignored). Before the fix it listed the five files.
  `FirstPaint_Should_MatchTheSettledShell` fetches the prerendered `/` and fails on `&#x`, `&quot;`
  or `&amp;` inside any `<style>` (before the fix: `&#x` found).
- (b) Same test, three real Chromium passes against the in-proc host with InteractiveAuto +
  prerender on: JavaScript disabled, `blazor.web.js` blocked, and settled interactive. Each asserts
  `.twe-appbar` `display:flex` + white, logo top ≥ 0 and 48px tall, search ≥ 400px wide,
  "Ctrl-K" on one line; app bar, logo and search-input boxes must be identical to the settled pass.
- (c) Header frame `rgb(85, 64, 156)` in all three passes; settled `--colorBrandBackground` is a
  purple ramp, not Fluent's `#0f6cbd`.
- FormField render test updated (rules now in `.razor.css`, no `<style>` rendered).

Measured (1280×800):

| Pass | App bar | Logo | Search input | Header |
|---|---|---|---|---|
| Before, JS off | 201×305 at y=-130, `display:block`, transparent | y=-130 (clipped) | 201px wide | transparent |
| After, JS off | 1264×54 at (8,8), flex, white | (30,10) 201×48 | (382,18) 520×32 | rgb(85,64,156) |
| After, blazor.web.js blocked | same | same | same | same |
| After, settled (Server) | same | same | same | same |

Screenshots: `screenshots/before-*.png` (unchanged master, same test) and `screenshots/after-*.png`.

### Residual (not in scope)

Before Fluent's script runs (or with JS off) Fluent web components are undefined and Fluent's own
design tokens are unset, so Fluent-rendered pieces (nav item styling, avatar, message bars, the
search placeholder) are plain until the script defines them. The shell geometry and brand colors
no longer change; with JS on, Fluent's script defines them well before the page is interactive.

