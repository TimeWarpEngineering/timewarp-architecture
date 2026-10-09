---
name: tw-blazor-css-strategy
description: How to style Blazor + FluentUI components in this repo without Tailwind. CSS isolation for every component, global design tokens, a native-root anchor plus targeted ::deep for FluentUI light-DOM children, ::part() for shadow DOM, and a ban on <style> elements in components because prerender HTML-encodes them. Use when authoring or restyling any .razor component, choosing where CSS lives, styling a FluentUI component, or fixing a first-paint style flash.
---

# Blazor CSS Strategy (isolation everywhere, anchored ::deep for Fluent children)

We do not use Tailwind. The design system is hand-written plain CSS built on global design
tokens. This skill is the standard for **where component CSS lives and how to scope it**.

Every rule must be in a real stylesheet that `<head>` links, so the browser applies it to the
server-prerendered HTML on **first paint**. That rules out CSS written inside a component.

## Two walls (why the rules look like this)

- **Wall A — isolation scope.** An isolated `*.razor.css` stamps its scope attribute
  (`[b-xxxxx]`) only on **native HTML elements the component itself authors**. That includes
  elements it authors inside a child component's `ChildContent`. A child component's **root**
  (`FluentStack`'s div, `FluentLayout`, `FluentNav`'s `<nav>`, the `fluent-field` /
  `fluent-text-input` hosts, `fluent-menu-list`, another of our components' root) is never
  stamped, so a plain isolated selector cannot match it.
- **Wall B — shadow DOM.** FluentUI interactive primitives (`fluent-button`, `fluent-text-input`,
  fields, …) are web components with **open shadow roots** in v4 and v5. Their internals are
  reachable **only** via `::part()` + CSS custom properties. No scoping strategy pierces the
  shadow boundary.

Wall A was re-verified on **.NET 11 RC1** (SDK `11.0.100-rc.1.26425.128`, FluentUI 5.0.0) in task
290 with a minimal component rendered through `HtmlRenderer`: the component's own `<div>` and a
`<span>` it authored inside `FluentStack`'s `ChildContent` carried `b-…`; FluentStack's root div,
the `fluent-field` host, `fluent-menu-list`, FluentLayout's root and a child component's
`<section>` did not. It is Blazor's isolation design, not a bug, so do not expect a framework
release to remove it.

## The rules

1. **Every component's CSS goes in `Component.razor.css` (CSS isolation).** The build bundles
   them into `Web.Spa.styles.css`, which `App.razor` links in `<head>`.
2. **Never author a `<style>` element in a component** — not `<style>@(@"…")</style>`, not
   `<style>@($@"…")</style>`, not a static `<style>` block, and not `MarkupString` to dodge the
   encoding. Under server prerender Blazor writes a C# expression inside `<style>` as an
   **HTML-encoded text node** (newlines → `&#xA;`, quotes → `&quot;`). Browsers do not decode
   entities inside `<style>`, so the whole block is invalid and every rule is dropped until the
   page goes interactive and the renderer rewrites the text. That is the unstyled first-paint
   flash fixed in task 290. Even a static block is CSS the `<head>` does not know about. The
   regression test `FirstPaint_Given_PrerenderedShell.Components_Should_AuthorNoStyleElements`
   fails the build's test run on any `<style>` in `source/**/*.razor` (Razor comments ignored).
3. **Styling a FluentUI light-DOM child (Wall A): native anchor + targeted `::deep`.** Give the
   component a native element to anchor on — the element that already wraps the child, or a
   native root `<div>` added for the purpose (`display: contents` when it must not add a box) —
   and write `.anchor ::deep .fluent-child`. Blazor compiles that to `.anchor[b-x] .fluent-child`:
   a plain descendant selector, scoped to this component's markup, in the bundle that loads before
   first paint. Keep `::deep` targeted (a named class or a specific element, `>` where possible),
   never `::deep *`.
4. **Brand tokens are global** in `web-spa/wwwroot/css/tokens.css` as CSS custom properties,
   linked in `<head>` before `Web.Spa.styles.css`. Consume them with `var(--twe-*)`. Tokens are
   the single source of truth for color, type scale, radius, elevation, chrome heights and status
   palette. `app.css` holds only the reset and genuinely global element rules.
5. **Styling inside a FluentUI primitive (Wall B):** `::part()` + CSS custom properties only.
6. **No inline `style=` attributes in our markup.** A strict CSP (`style-src` without
   `'unsafe-inline'`) blocks them, and they are CSS outside the stylesheet system. The app sends no
   CSP header today; keep our own markup ready for one. Per-instance variation is a class or a
   `data-*` attribute selected in `.razor.css` (`.twe-avatar[data-size=lg]`), not generated CSS
   and not `style="--x: …"`. (FluentUI itself still writes inline styles, for example
   `FluentLayout`'s height variables and its container-query `<style>`. That is framework markup
   we do not control; it renders as raw markup, not encoded text.)
7. **Undefined web components:** before Fluent's script defines them, `fluent-*` elements are
   unknown inline elements with no box. Where that shifts layout on first paint, reserve the
   upgraded box with a `:not(:defined)` rule anchored the same way (the shell's search field
   does this). The rule stops matching once the element is defined.
8. **Theme on first paint.** `App.razor` renders `<body data-theme-color="#55409c"
   data-theme="light">`; Fluent's `beforeStart` initializer builds the brand ramp from it before
   anything is interactive. Do not apply the theme from `OnAfterRenderAsync` (a fresh browser
   paints Fluent's default blue first). Chrome colors that must be right before any script runs
   (the shell's header/footer frame) use `--twe-*` tokens, not Fluent's runtime tokens.

## Canonical in-repo example (the app shell)

`web-spa/components/TimeWarpPage.razor` renders `FluentLayout`, `FluentNav` (via `NavMenu`),
`TwBreadcrumb` and `FluentTextInput`. Its root is a native `<div class="twe-shell">` with
`display: contents`, and every rule is in `TimeWarpPage.razor.css`:

```razor
<div class="twe-shell">
  <FluentLayout Class=@($"{Id} twe-shell__layout")>
    <FluentLayoutItem Area="LayoutArea.Header">
      <header class="twe-appbar">…</header>   @* authored here → stamped *@
    </FluentLayoutItem>
    …
  </FluentLayout>
</div>
```

```css
/* TimeWarpPage.razor.css */
.twe-shell { display: contents; }

/* Fluent roots (Wall A): anchor on the native root, ::deep to the child. */
.twe-shell ::deep .twe-shell__layout { --layout-header-height: var(--twe-appbar-height) !important; }
.twe-shell ::deep .fluent-layout-item[area=header] { background-color: var(--twe-purple); }
.twe-shell ::deep .twe-nav { background: var(--twe-paper-2); }

/* Elements this component authors are stamped: plain isolated selectors. */
.twe-appbar { display: flex; align-items: center; }
.twe-page__crumbs ::deep .breadcrumb { display: flex; }
```

Other examples: `FormField.razor.css` (`.twe-form-field__control ::deep > fluent-text-input`),
`Profile.razor.css` (native `.twe-profile` root, `::deep fluent-menu > fluent-menu-list`),
`LoginPage.razor.css` and `Card.razor.css` (plain isolation).

## Decision quick-reference

| Situation | Approach |
|---|---|
| Element the component authors (even inside a Fluent `ChildContent`) | Plain selector in `*.razor.css` |
| A FluentUI / child-component root (FluentStack, FluentNav, FluentLayout, fluent-field) | Native anchor + `.anchor ::deep .child` in `*.razor.css` |
| Change a FluentUI primitive's internals (button bg, text color) | `::part()` + CSS variables |
| Brand color / size / radius / chrome height | `var(--twe-*)` from `tokens.css` |
| Per-instance variation | Class or `data-*` attribute + rule in `*.razor.css` |
| Layout shift while Fluent web components are undefined | Anchored `:not(:defined)` rule reserving the box |
| Anything | **Never** a `<style>` element in a component; **never** inline `style=`; **never** a `global.css` dumping ground |

## History

- 2024-05-28 (Steven ↔ Pete, quoted in crunchit `kanban/done/022-research-css-strategy-…`):
  Wall A — "you can't style it without wrapping it with a div"; `::deep` judged painful; inline
  `style=` rejected for CSP; `global.css` disliked for lack of locality. Result: co-located
  `<style>` blocks scoped by `.{Id}` or a fixed root class ("Exception B").
- Task 059-002 (2026-06-22) adopted the isolation-first hybrid with Exception B; commit
  `a2e107d7f2` made `TimeWarpPage` the canonical `<style>@(@"…")</style>` example, and it spread
  to FormField (task 234), LoginPage and Profile.
- Task 290 (2026-10-09): Exception B's `<style>@(…)</style>` was the cause of the prerender
  first-paint flash (0 shell rules before interactive, 24 after). Exception B is retired. Wall A
  is still real on .NET 11, so Fluent children are reached with an anchored `::deep` in the
  component's own `.razor.css` instead: the same locality Exception B wanted, in a stylesheet the
  browser loads before first paint. Static stylesheets in `<head>` for Fluent children were the
  alternative; they bring back a global file per area, so they were not chosen.

## Notes

- FluentUI v5 did **not** remove Wall A or Wall B. The strategy is the same across v4 and v5.
- Proof: `tests/container-apps/web/web-spa-playwright-tests/first-paint-playwright-tests.cs`
  measures the shell with JavaScript off, with `blazor.web.js` blocked, and settled interactive,
  and requires the same app bar, logo and search boxes and the brand header in all three. CI
  uploads its screenshots as the `playwright-<run>` artifact.
