---
name: blazor-layout
description: How to structure Blazor app chrome with the "empty layout + cascaded page-component shell" pattern — keep LayoutComponentBase empty and put header/nav/content/aside/footer in ONE shell component that pages wrap their content in and that cascades itself. Use when designing a Blazor app's layout/navigation, deciding where chrome belongs, building a layout shell, or when chrome must react to a state store or per-navigation lifecycle that a layout can't provide.
---

# Blazor app shell: empty layout + cascaded page-component shell

A technique for where app chrome (header, nav, content area, aside, footer) lives in a Blazor app.

**The pattern:** keep the routed layout (`LayoutComponentBase`) almost empty, and put *all* chrome
in a single **shell component** that (a) inherits your app's state/base component, (b) renders the
layout zones, (c) cascades itself, and (d) is wrapped around each page's content. Pages don't
declare chrome; they render their body *inside* `<Shell>`.

## Why not just put chrome in the layout?

`LayoutComponentBase` is the obvious place, but it has two limits that bite real apps:

- **It can't participate in your state/render pipeline.** A layout isn't your state-store base
  component, so it doesn't get the store's component id, automatic re-render-on-state-change, or
  DI/lifecycle hooks your other components rely on. If any chrome must react to global state — a
  busy/activity indicator, current user, unread count, theme — the layout can't do it cleanly.
- **Layouts persist across navigations.** You get no clean per-navigation lifecycle, and the layout
  can't expose per-page inputs (title, an aside panel) the way a normal component's parameters can.

A **shell that is a normal (state-aware) component** solves both: it re-renders with your store, has
per-instance lifecycle, and takes `Title`/`Aside`/etc. as parameters. You then make it reachable to
descendants by **cascading it**, and you keep the routed layout empty so it doesn't fight the shell.

## How to build it

1. **Empty layout.** Your `LayoutComponentBase` renders just `@Body` (plus any genuinely
   layout-root, run-once concerns — e.g. applying a theme). Guard anything that uses JS interop so it
   only runs when interactive (not during server prerender). No header/nav/footer here.
2. **Shell component.** A normal component that **inherits your state/base component** (not
   `LayoutComponentBase`). It:
   - renders the chrome zones using your UI library's layout primitive (header / navigation /
     content / aside / footer);
   - declares `[Parameter]`s for per-page inputs (`Title`, `ChildContent`, optional `Aside`);
   - `<CascadingValue Value=@this>` wraps its tree so descendants can reach the shell;
   - renders `@ChildContent` in the content zone, and conditionally renders the aside zone only when
     `Aside` is supplied.
3. **Pages wrap their content in the shell:** `<Shell Title="…">…page body…</Shell>`. Routing stays
   a separate concern (your `@page`/route attribute), independent of the shell.

## Pitfalls

- **One shell, parameterized — not several.** Resist per-section shell variants; pass parameters
  (title, aside, flags) instead.
- **Don't name the shell after your routing concept.** If your framework/app uses `Page` or a
  `[Page]` route attribute, naming the shell `Page` collides — give it a distinct name.
- **Don't make the shell a `LayoutComponentBase`** (or register it as the routed layout) — that
  throws away the state/lifecycle benefits that are the whole point.
- **Don't put chrome in the layout or in individual pages.** It lives in the shell only.
- **Guard interop for prerender.** Theme/JS-interop calls in the layout or shell must be gated on
  "is interactive," or they throw during server-side prerender.

## Reference implementation (timewarp-architecture)

Concrete instance of the pattern in this repo:
- **Empty layout:** `components/layouts/MainLayout.razor` — `@inherits LayoutComponentBase`, renders
  `@Body` + `<FluentUIRequiredFeatures/>`, applies the brand ramp via `IThemeService.SetThemeAsync`
  in `OnAfterRenderAsync` guarded by `RendererInfo.IsInteractive`.
- **Shell:** `components/TimeWarpPage.razor` — `@inherits BaseComponent` (the TimeWarp.State base
  component → gives it the state `Id` and render-on-state-change, e.g. the footer activity spinner
  bound to `ActionTrackingState.IsActive`). Renders FluentUI `FluentLayout` zones + brand/search/nav/
  footer/`ModalController`, and `<CascadingValue Value=@this>`s itself. Parameters: `Title`,
  `ChildContent`, `Aside`.
- **Pages:** `@inherits BaseComponent`, wrap content in `<TimeWarpPage Title="…">…</TimeWarpPage>`;
  routing comes from `[Page("/route")]` in the `.razor.cs` (the Moxy `mixins/Page.mixin`). The shell
  is named `TimeWarpPage` (not `Page`) precisely because `[Page]` is the routing concept.
- **Styling of the shell:** see the `blazor-css-strategy` skill — this skill is the *structure*, that
  one is the *styling* (Tier-2 scope-handle `.twe-shell`).
- **Slice boundary:** chrome/shell lives **outside** SliceRoot (e.g. `…Components`); product
  pages and state live in product slice namespaces (`…Features.<Id>`). See skill `slice-isolation`.
