# Decide & document CSS strategy: isolation-first hybrid

Parent: 059. **Do this first — it governs 001/004/005.**

## Decision (adopt the crunchit verdict, do not re-litigate)

The reference repo already researched this end-to-end (its
`kanban/to-do/022-research-css-strategy-for-components-isolated-css-pain-points.md`).
Adopt **"isolation-first hybrid"**:

1. **Default = Blazor CSS isolation (`*.razor.css`).** An isolated component MUST render a
   **native HTML root** (`<section>/<button>/<div>`), never a Fluent/child component.
2. **Brand tokens stay global** in `wwwroot/css/tokens.css`; consume via `var(--twa-*)`
   (pick the project prefix here — `--twa-*` suggested; crunchit uses `--crunchit-*`).
3. **Exception A — inside a Fluent primitive (shadow DOM):** `::part()` + CSS custom
   properties only. Nothing else pierces the shadow boundary (verified `fluent-button`/
   `fluent-text` have open shadow roots in v5 RC).
4. **Exception B — runtime-dynamic CSS, or styling a Fluent light-DOM child you can't
   wrap:** own a scope handle + co-located `<style>` scoped to it. Reach Fluent light-DOM
   children via `Class=` (NO isolation, NO `::deep`, NO inline `style=` — CSP-unsafe).
   - Multi-instance component → scope by `.@(Id)` from the state base component.
   - Singleton (e.g. the layout/shell) → a fixed namespaced root class
     (crunchit uses `.crunchit-shell`).

### Two base-class tiers (keep separate)

- **Tier-1 leaf primitives** (Card/Button/StatusBadge/fields): a thin
  `XxxComponent : ComponentBase` providing only the `AdditionalAttributes` splat. **No `Id`.**
  Isolation + native root. (crunchit: `CrunchitComponent`.)
- **Tier-2 state/container components** (layout/shell/sections): existing
  `BaseComponent : TimeWarpStateComponent`, which already exposes public `string Id` — the
  Exception-B scope handle. Nothing new to build.

## Why (two walls — both present in v4 AND v5)

- **Wall A — isolation scope:** isolated CSS only stamps the scope attr on native elements
  the component itself authors; a child component's root (even a light-DOM FluentStack div)
  never gets it. → need a self-owned scope handle.
- **Wall B — shadow DOM:** Fluent primitives are web components; internals reachable only
  via `::part()` + CSS variables. No scoping strategy reaches inside.

Inline `Style=` is forbidden as the system (CSP-prohibited on locked-down browsers); reserve
`Style=@Value` for genuinely dynamic per-instance values only.

## Tasks

- [x] Read crunchit task 022 in full.
- [x] Choose the CSS-variable prefix → **`--twa-*`**.
- [x] Write the convention as an **exportable skill**: `skills/blazor-css-strategy/SKILL.md`
      (root `skills/` is the home for exportable skills, per Steven). Includes the rule, both
      exceptions, the two base tiers, a decision quick-reference, and copyable examples.
- [x] Confirm the state base component exposes `Id` → **yes**:
      `BaseComponent : TimeWarpStateDevComponent, IAttributeComponent`
      (`web-spa/features/base/base-component.cs`).

## Outcome (2026-06-22)

DONE. Strategy documented in `skills/blazor-css-strategy/SKILL.md`.

The repo **already uses the Tier-2 / Exception-B pattern** —
`web-spa/components/composites/time-warp-page/TimeWarpPage.razor` wraps a
`FluentMultiSplitter` with `Class=@($"{Id} timewarp-page")` and a `.{Id}`-scoped co-located
`<style>`. That is the canonical in-repo example cited in the skill. No new base class is
needed for Tier-2; the Tier-1 leaf base will be introduced when the first leaf primitive is
built (005).
