---
name: blazor-css-strategy
description: How to style Blazor + FluentUI components in this repo without Tailwind. The "isolation-first hybrid" convention — CSS isolation by default, global design tokens, and two documented exceptions for FluentUI shadow-DOM and light-DOM children. Use when authoring or restyling any .razor component, choosing where CSS lives, or styling a FluentUI component.
---

# Blazor CSS Strategy (isolation-first hybrid)

We do not use Tailwind. The design system is hand-written plain CSS built on global design
tokens. This skill is the standard for **where component CSS lives and how to scope it**.

It exists because Blazor CSS isolation has two hard walls that bite the moment a component
composes FluentUI:

- **Wall A — isolation scope.** An isolated `*.razor.css` only stamps its scope attribute
  (`[b-xxxxx]`) on **native HTML elements the component itself authors**. A child component's
  root (even a light-DOM `<FluentStack>` div) never receives the scope attribute, so isolated
  CSS cannot target it. `::deep` only helps when there is a scoped native ancestor to anchor
  on, and is slow/error-prone.
- **Wall B — shadow DOM.** FluentUI interactive primitives (`fluent-button`, `fluent-text`,
  fields, …) are web components with **open shadow roots** — in **v4 and v5 alike**. Their
  internals are reachable **only** via `::part()` + CSS custom properties. No scoping strategy
  pierces the shadow boundary.

## The rules

1. **Default = Blazor CSS isolation (`Foo.razor.css`).** An isolated component **MUST render a
   native HTML root** (`<section>`/`<button>`/`<div>`/`<span>`), never a FluentUI/child
   component as its root. This is what keeps isolation working.
2. **Brand tokens are global** in `web-spa/wwwroot/css/tokens.css` as CSS custom properties.
   Consume them with `var(--twe-*)`. Tokens are the single source of truth for color, type
   scale, radius, elevation, and status palette — never hard-code these in component CSS.
3. **Exception A — styling *inside* a FluentUI primitive (shadow DOM):** use `::part()` +
   CSS custom properties only. Nothing else works.
4. **Exception B — styling a FluentUI light-DOM child you can't wrap, or runtime-dynamic
   CSS:** own a **scope handle** and write a co-located `<style>` scoped to it; pass the
   handle to the FluentUI component via `Class=`. **No `::deep`. No inline `style=`**
   (inline styles are prohibited under strict-CSP / locked-down browsers — reserve
   `Style=@Value` for genuinely dynamic per-instance values only).
   - **Multi-instance** component → scope by `.@(Id)` (the `Id` from the state base
     component, see Tiers below).
   - **Singleton** (e.g. the layout/shell) → a fixed namespaced root class (`.twe-shell`).

## Two base-class tiers (keep them separate)

- **Tier-1 — leaf primitives** (Card, Button, StatusBadge, fields): inherit a thin
  `ComponentBase` derivative that provides only the attribute splat. **No `Id`.** Renders a
  native root → uses isolation (`*.razor.css`). Many instances, static styling — isolation is
  cheapest here.
- **Tier-2 — state/container components** (layout, shell, page composites, sections): inherit
  `BaseComponent : TimeWarpStateDevComponent`, which already exposes a public `string Id`.
  That `Id` is the Exception-B scope handle. Few instances, sometimes dynamic — scope-handle
  fits.

## Canonical in-repo example (Tier-2 / Exception B)

`web-spa/components/TimeWarpPage.razor` (the app shell) does this — it renders `FluentLayout` /
`FluentNav` / `FluentTextInput` (light-DOM children, Wall A) and styles them via a co-located
`<style>` scoped to a fixed root class `.twe-shell` (the shell is a singleton, so a fixed class
rather than `.@(Id)`):

```razor
<FluentLayout Class=@($"{Id} twe-shell")> … <FluentNav Class="twe-nav"/> … </FluentLayout>

<style>
  @(@"
    .twe-shell .twe-nav { background: var(--twe-paper-2); border-right: 1px solid var(--twe-rule); }
    .twe-shell .twe-appbar__search fluent-text-input { width: 100%; }
  ")
</style>
```

`.twe-shell .twe-nav` reaches the FluentNav's light-DOM root (a plain descendant selector from
the Id'd ancestor — no `::deep`, no wrapper div). Use a **verbatim** string `@(@"…")` so CSS
braces are literal; only use the interpolated `@($@"… {{ }} …")` form when you need `{Id}`.

## Tier-1 example (leaf, isolation)

```razor
@* Card.razor *@
<section @attributes="Attributes" class="@CssClass">
  <div class="twe-card__body">@ChildContent</div>
</section>
```
```css
/* Card.razor.css */
.twe-card {
  background: var(--twe-paper);
  border: 1px solid var(--twe-rule);
  border-radius: var(--twe-radius);
}
```

## Decision quick-reference

| Situation | Approach |
|---|---|
| Leaf component with a native root | Isolation (`*.razor.css`) |
| Need a brand color / size / radius | `var(--twe-*)` from `tokens.css` |
| Style a FluentUI light-DOM child (FluentStack, FluentNav, splitter…) | Exception B: `Class=@($"{Id} …")` + `.{Id}` in co-located `<style>` |
| Singleton layout/shell | Exception B with a fixed root class (`.twe-shell`) |
| Change a FluentUI primitive's internals (button bg, text color) | Exception A: `::part()` + CSS variables |
| Truly dynamic per-instance value | `Style=@Value` (sparingly only) |
| Anything | **Never** `global.css` dumping ground; **never** inline `style=` as the system |

## Notes

- FluentUI v5 did **not** remove Wall A or Wall B (verified empirically). The strategy is the
  same across v4 and v5.
- Reference implementation: the crunchit web-spa
  (`Crunchitfs/crunchit` branch `Cramer/2026-05-29/initial`, `source/web-spa`) runs this on
  FluentUI v5 — see its `tokens.css`, `MainLayout.razor` (`.crunchit-shell`), and
  `Card.razor`(.css).
