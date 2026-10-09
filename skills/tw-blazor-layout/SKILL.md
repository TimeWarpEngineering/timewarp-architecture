---
name: tw-blazor-layout
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
   layout-root, run-once concerns). The brand theme is not one of them: it is set on the first
   server render (`App.razor`'s `<body data-theme-color … data-theme>`), never applied from
   `OnAfterRenderAsync` — see `tw-blazor-css-strategy` rule 8. Guard anything else that uses JS
   interop so it only runs when interactive (not during server prerender). No header/nav/footer here.
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
- **Guard interop for prerender.** JS-interop calls in the layout or shell must be gated on
  "is interactive," or they throw during server-side prerender. Don't solve the theme this way:
  a theme applied once interactive paints the default theme first and then flashes. Set it on the
  first server render instead (`tw-blazor-css-strategy` rule 8).

## The shell owns the notification region

Every page gets exactly **one** notification region, owned by the shell — not by pages, cards, or
feature components. Both page shells (the full shell and its focused variant) render the
notification-region host directly below the page title/breadcrumb and above the first card. The
host paints a state store that holds the current bars; it is the single place an operation outcome
(success or failure) becomes visible.

- **Lifetime, not per-page markup.** Errors stay visible until dismissed or until the route
  changes — a navigation listener clears the region on the router's location-changed event.
  Success bars auto-dismiss after a fixed interval and are also dismissible by hand. Pages never
  wire this up themselves; it comes free from the shell.
- **Spacing is a token, not a margin.** The region owns the gap between stacked bars and the gap
  before the first card via design tokens in `tokens.css`, applied in the host's own isolated CSS.
  Pages never add top/bottom margin to compensate — if spacing looks wrong, fix the token, not the
  page.
- **Pages never add their own.** A page, card, or feature component that renders its own outcome
  bar duplicates the region and breaks the one-region-per-page rule; report outcomes through the
  shared state store instead (see the `tw-blazor` skill, "Operation outcomes").

## Navigation destinations come from one registry

The nav menu and any other destination surface (a command palette, a sitemap) read **one**
generated list of pages — never a second hand-copied route list.

- **Opt in on the page's route declaration.** A page that is a navigation destination says so
  where its route and policy are declared; the generator lists it (route, URL, title, icon,
  policy) in a per-assembly registry. Pages that need route arguments are not destinations.
- **The menu may keep hand markup, but not hand routes.** Grouping, per-group authorization, and
  feature-flag regions stay authored markup; each link names a page type, and the link component's
  type constraint accepts only registry members — a link to an unlisted page does not compile.
- **Reflection-free.** The registry is a generated array of static member reads, so it is
  AOT/trim safe and needs no assembly scanning at startup.

## Reference implementation (timewarp-architecture)

Concrete instance of the pattern in this repo:
- **Empty layout:** `components/layouts/MainLayout.razor` — `@inherits LayoutComponentBase`, renders
  `@Body` + `<FluentUIRequiredFeatures/>`. It owns the brand color constant; `App.razor` renders it
  as `<body data-theme-color>` so Fluent builds the brand theme at script start, not after the page
  goes interactive (task 290).
- **Shell:** `components/TimeWarpPage.razor` — `@inherits BaseComponent` (the TimeWarp.State base
  component → gives it the state `Id` and render-on-state-change, e.g. the footer activity spinner
  bound to `ActionTrackingState.IsActive`). Renders FluentUI `FluentLayout` zones + brand/search/nav/
  footer/`ModalController`, and `<CascadingValue Value=@this>`s itself. Parameters: `Title`,
  `ChildContent`, `Aside`.
- **Pages:** `@inherits BaseComponent`, wrap content in `<TimeWarpPage Title="…">…</TimeWarpPage>`;
  routing comes from `[Page("/route")]` on the `.razor.cs` partial (`PageSourceGenerator` emits
  `[Route]`, `GetPageUrl`, and route parameters). The shell is named `TimeWarpPage` (not `Page`)
  precisely because `[Page]` is the routing concept.
- **Styling of the shell:** see the `tw-blazor-css-strategy` skill — this skill is the *structure*, that
  one is the *styling* (`TimeWarpPage.razor.css`, anchored on the native `.twe-shell` root).
- **Slice boundary:** chrome/shell lives **outside** SliceRoot (e.g. `…Components`); product
  pages and state live in product slice namespaces (`…Features.<Id>`). See skill `tw-slice-isolation`.
- **Navigation registry:** `[Page("/route", Policy = …, Navigable = true)]` lists a static-route
  page in the generated `PageRegistry.All` (`PageRegistryEntry`: type, route template, URL, title,
  icon, policy) and adds `INavigationDestination`; `components/elements/TimeWarpNavLink.razor`
  constrains `TPage` to it, so every `components/NavMenu.razor` link is a registry entry.
  `Navigable = true` on a parameterized route (or a non-literal value) is **TWE009**.
- **Multi-route (multi-tab) pages:** one `[Page]` takes the primary route first and alias routes
  after it — `[Page("/clients", "/clients/revenue", "/clients/me-close", Policy = …)]`. The
  generator emits one `[Route]` per path. The **primary** route owns `GetPageUrl`, `IStaticRoute`,
  the `PageRegistry` row, and the `Navigable` (TWE009) judgment; aliases are `[Route]`-only, so a
  parameterized alias on a static navigable page is fine. `Policy` covers every route of the page.
  An untyped alias token reuses the type an earlier route gave that name (`/clients/{ClientId:string}` +
  `/clients/{ClientId}/revenue`). Stacked `[Page]`, a non-literal alias, or an alias token typed
  differently from an earlier route is **TWE011**. A route that repeats another route of the same page
  (including a hand-written `[Route]`) is **TWE010**.
- **Notification region:** `components/MessageBars.razor` + `NotificationState`
  (`features/notification/notification-state/`), painted by both shells; TWA0025 keeps outcome
  bars out of pages.
