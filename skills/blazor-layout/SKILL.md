---
name: blazor-layout
description: How this repo structures Blazor page layout — the "empty MainLayout + TimeWarpPage shell" (COPIC) pattern. The app chrome (header/nav/content/aside/footer) lives in ONE page-component shell, TimeWarpPage, that every page wraps its content in; MainLayout stays empty. Use when adding a page, changing app chrome/navigation, touching MainLayout/TimeWarpPage, or wondering where layout lives.
---

# Blazor Layout (empty MainLayout + TimeWarpPage shell)

The app chrome lives in **one** component — `components/TimeWarpPage.razor` — and every page
wraps its content in it. `MainLayout` is intentionally almost empty. This is the same pattern used
in the **COPIC** project (`TimeWarpEngineering/copic`): an empty `MainLayout` plus a page-component
shell, rather than chrome in the layout.

## The rule

1. **`MainLayout` owns nothing but `@Body`.** `components/layouts/MainLayout.razor` is
   `@inherits LayoutComponentBase`, renders `@Body` + `<FluentUIRequiredFeatures />`, and does
   only things that genuinely belong to the layout root (e.g. applying the FluentUI brand ramp via
   `IThemeService.SetThemeAsync` in `OnAfterRenderAsync` guarded by `RendererInfo.IsInteractive`).
   **No header, nav, footer, or page chrome here.**
2. **`TimeWarpPage` is the single app shell.** `components/TimeWarpPage.razor` renders the
   `FluentLayout` zones (Header / Navigation / Content / Aside / Footer), the brand, search, nav
   menu, footer, and `ModalController`. It `@inherits BaseComponent` and `<CascadingValue Value=@this>`s
   itself so descendants can reach it. There is exactly **one** shell — never a second one.
3. **Every page wraps its content in `<TimeWarpPage>`.** A page renders its body *inside* the shell;
   it never re-declares chrome.

## Why a page-component shell instead of putting chrome in MainLayout

`MainLayout : LayoutComponentBase` can't be what we need:
- It doesn't get a TimeWarp.State `Id`, the state render-trigger wiring, or a clean per-navigation
  lifecycle. `TimeWarpPage : BaseComponent` (the state dev base component) **does** — it has the
  public `Id` (used as the CSS scope handle, see the `blazor-css-strategy` skill) and re-renders on state
  changes (e.g. the footer activity spinner bound to `ActionTrackingState.IsActive`).
- Layouts persist across navigations; a page-shell gives per-page lifecycle and lets each page set
  its own `Title` / `Aside` via parameters.

So the chrome lives in a component that behaves like a page and is cascaded to its children.

## Two different "Page" concepts — don't conflate them

| Concept | What it is | Where |
|---|---|---|
| **`TimeWarpPage`** | the **visual shell** (chrome) pages wrap content in | `components/TimeWarpPage.razor` |
| **`[Page("/route")]`** | the **routing** attribute (Moxy `mixins/Page.mixin`) that generates `[Route]`, `GetPageUrl()`, `Policy`, and route params | a page's `.razor.cs` |

This is why the shell is named **`TimeWarpPage`, not `Page`** — `Page`/`[Page]` is already the
routing concept. They're orthogonal: `[Page]` makes a component routable; `TimeWarpPage` gives it chrome.

## Recipe — add a new page

`pages/SettingsPage.razor`:
```razor
@namespace TimeWarp.Architecture.Pages
@inherits BaseComponent

@code {
  public static string Title => "Settings";
  public static Icon? NavIcon => new Icons.Regular.Size20.Question();
}

<TimeWarpPage Title="Settings">
  <Card>…page content…</Card>
</TimeWarpPage>
```
`pages/SettingsPage.razor.cs` (routing via the Page mixin):
```csharp
[Page("/Settings")]
public partial class SettingsPage;
```
- `@inherits BaseComponent` — gives the page its state `Id` / render triggers.
- `static Title` / `NavIcon` — consumed by the nav menu.
- `<TimeWarpPage Title="…">` — the shell; the title renders in the content header.
- Need a right rail? pass the `Aside` render fragment:
  `<TimeWarpPage Title="…"><Aside>…</Aside>…body…</TimeWarpPage>` (the Aside zone only renders when provided).

## Don'ts

- **Don't** add chrome (header/nav/footer/toolbars) to `MainLayout` or to individual pages — it
  belongs in `TimeWarpPage`.
- **Don't** create a second/alternate shell component. One shell, parameterized. (The old
  multi-shell setup — SitePage/SidebarPage/StackedPage + the splitter TimeWarpPage — was deleted;
  don't reintroduce it.)
- **Don't** make `TimeWarpPage` inherit `LayoutComponentBase` or move it under `layouts/` — it must
  stay a `BaseComponent` page-shell to keep the state `Id` / render-trigger behavior.
- **Don't** call JS-interop/theme APIs during server prerender — guard with
  `RendererInfo.IsInteractive` (as MainLayout does for `SetThemeAsync`).

## Related

- `blazor-css-strategy` skill — how `TimeWarpPage` is styled (Tier-2 scope-handle `.twe-shell`,
  isolation vs co-located `<style>`). This skill is the *structure*; that one is the *styling*.
