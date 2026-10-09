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

- [ ] Step 0: verify Wall A on .NET 11 with a minimal repro; record the decision and why in Results
- [ ] TimeWarpPage shell CSS → stylesheet (per step 0); inline `<style>` removed
- [ ] Profile → `.razor.css` + CSS custom properties for per-instance values; inline `<style>` removed
- [ ] FormField → stylesheet per step 0 (Fluent host stretch still works; keep the task-234 M1 behavior); inline `<style>` removed
- [ ] LoginPage → `.razor.css`; inline `<style>` removed
- [ ] NotFoundPage → `.razor.css`; inline `<style>` removed (fix the 403 copy)
- [ ] Tokens and app-wide rules in global stylesheet, linked before `Web.Spa.styles.css`
- [ ] Brand theme in first server render (no blue → purple)
- [ ] Rewrite `skills/tw-blazor-css-strategy/SKILL.md`; ban inline `<style>` in components
- [ ] Test (a): no encoded entities, no `<style>` in prerendered components
- [ ] Test (b): JS-off / pre-interactive appbar layout (flex, background, logo visible, search full width)
- [ ] Test (c): brand header color on first paint
- [ ] "AI not configured" hint works from any directory (`--id` form, pwsh)
- [ ] Template copy (`TimeWarp.Architecture/` if it carries these files) kept in sync
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
