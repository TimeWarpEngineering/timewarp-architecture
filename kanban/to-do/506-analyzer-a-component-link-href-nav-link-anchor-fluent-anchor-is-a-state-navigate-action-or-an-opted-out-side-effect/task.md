# Analyzer: a component link (Href, NavLink, anchor, FluentAnchor) is a State navigate action or an opted-out side effect

## Description

The rule since task 260: every SPA user interaction dispatches a TimeWarp.State action. TWA0026
(`component-side-effect-analyzer.cs`, task 265) enforces it for method calls only:
`NavigationManager.NavigateTo`, `IJSRuntime`, `IApiService`, `HttpClient`, browser storage. It
never looks at markup. An `Href="…"` in a `.razor` file compiles to an `AddComponentParameter`
call with a string name, which the analyzer ignores. So `FluentAnchorButton`, `FluentAnchor`,
`FluentLink`, `FluentButton Href`, `NavLink`, and raw `<a href>` all build clean, and
`RolesListPage.razor` line 31 ships a New Role button that is a plain link (task 207, 2026-09-02,
before the analyzer existed). 22 `.razor` files under web-spa carry an `Href`, `<a href`, or
`NavLink` today.

`skills/tw-blazor/SKILL.md` line 360 also says "Navigation is `FluentAnchor`", which contradicts
line 45 ("navigation — `RouteState.ChangeRoute(...)` for an in-app route"). The skill and the
analyzer must say the same thing.

Found 2026-11-24 while validating 490: Ask AI could not find a create-role entry point because New
Role is not an action.

## Requirements

- New diagnostic **TWA0050** in `timewarp-architecture-convention-analyzers` (next free id per the
  `AnalyzerReleases.Unshipped.md` header; update that header). Severity error. It reports, inside
  any `ComponentBase`-derived type's generated Razor code, a component parameter or HTML attribute
  named `Href`, `href`, or `To` whose value is not null, and any `NavLink` or `<a>` element with an
  `href`. External links (`http://`, `https://`, `mailto:`, `tel:`) with `target="_blank"` or
  `rel="external"` are not reported. Message names the fix: dispatch `RouteState.ChangeRoute` (or
  the page's navigate action) from a State action.
- Opt-out is the existing `[DirectComponentSideEffect("reason")]` on the component, reported by
  TWA0027 when the reason is empty. `NavMenu`, breadcrumbs, and the footer may opt out with a
  reason if an implementer judges a real anchor is required for accessibility or middle-click.
  Record the decision in that component's Design region.
- Convert the offenders. `RolesListPage` New Role becomes a `FluentButton` whose click dispatches
  a State action that navigates to `RolePage` (so it also appears in the action catalog for Ask
  AI). Do the same for every other hit the analyzer reports, or opt the component out with a
  reason. Zero TWA0050 on `dev build`.
- Fix `skills/tw-blazor/SKILL.md` line 360 and add TWA0050 to its analyzer table beside TWA0026.
- Analyzer tests: positive for `FluentAnchorButton Href`, `FluentAnchor Href`, `NavLink href`,
  `<a href>`, `FluentButton Href`; negative for an external `https://` link with
  `target="_blank"`, a component with `[DirectComponentSideEffect]`, and a `FluentButton` with
  `OnClick` only.

## Checklist

- [ ] TWA0050 analyzer + tests; `AnalyzerReleases.Unshipped.md` and its next-free-id header updated
- [ ] All web-spa hits converted to State actions or opted out with a reason in a Design region
- [ ] `RolesListPage` New Role is a catalogued navigate action; Ask AI can reach it
- [ ] `skills/tw-blazor/SKILL.md` reconciled; `dev lint-skills` green
- [ ] Screenshot of `/Admin/Roles` with the converted button in `evidence/`
- [ ] `dev build` 0/0, `ganda repo audit`, analyzer tests, web-spa integration tests green

## Session

- Created: 2026-11-24. Investigation: TWA0026 exists and is unsuppressed; the gap is markup
  links, never specced (tasks 260, 265).
