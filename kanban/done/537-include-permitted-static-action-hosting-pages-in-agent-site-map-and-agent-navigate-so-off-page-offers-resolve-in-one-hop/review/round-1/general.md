# Round 1 — general
**Date:** 2026-11-28
**Scope reviewed:** branch `task/537-include-permitted-static-action-hosting-pages-in` vs `master`
(e87497bca fix, 58e3abd13 Amuru pin bump, plus kitchen commits). Product files:
`page-emitter.cs`, `page-source-generator.cs`, `RolePage.razor.cs`, `agent-site-map.cs`,
`agent-navigate.cs`, `agent-ask-instructions.cs`, `catalog-agent-tool-set.cs`,
`skills/tw-blazor/SKILL.md`, `Directory.Packages.props`, and the generator and
navigation-destinations tests.

## Summary

The generator now gives each `PageRegistry.ActionScopes` row the page's `Policy` and `Title`
(read from the page's generated or declared static members) and its `[Page]` Description as init
properties. `AgentSiteMap.DestinationsAsync` appends each static action route that is not already
a menu or deep-link row, after `NavigationDestinations.IsAuthorizedAsync` (the same check the menu
uses). Because `navigate` reads the same list, its enum and `FindAsync` accept the route in one hop.
NavMenu and the palette still read `NavigationDestinations`, so the nav rail does not change. No
route literal was added under `features/application/agent/`. Risk is low.

The implementation matches the original task and the `## Goal (implementer restatement)`. I found
no misread.

Checks made by reading code:

- Every `[Page]` class gets a generated `public static string Policy`, and `Title` is a static
  member on the page (`RolePage.Title => "New Role"`). The emitted `{ Policy = X.Policy, Title =
  X.Title, ... }` initializer therefore compiles for every action page. The generator test pins
  the emitted text.
- Of today's action pages, `/Admin/Roles/New` is the only static route that is not a menu row.
  `/Admin/Roles/{RoleId:Guid}` and `/Feedback/{FeedbackItemId:Guid}` are parameterized and
  skipped. The rest are menu pages and are de-duplicated through `taken`.
- Authorization reads the `[Page]` Policy (`AdminRolesManage`), which matches the page's
  `[Authorize]` attribute.
- Tests: the admin test checks every non-null `OffPageRoute` against the site map, the navigate
  enum, `FindAsync`, and `InvokeAsync`. It pins `Prefill.Role.CreateRole` → `/Admin/Roles/New` and
  keeps the Ask instructions more than 1,000 characters under 7,000. The negative test covers the
  anonymous principal and the plain user. The per-principal fixture now expects the action routes
  between the menu rows and the deep links, and adds unauthorized action routes to the refused set.
- The `TimeWarp.Amuru` 3.0.0-beta.11 bump was forced by the audit's `package-pin-currency`
  check. Under the audit-fix policy it is intended and needs no finding.

## Issues

### Issue 1 — Severity: nit
- File: kanban/in-progress/537-…/task.md (`### How to validate` → Gates)
- Description: The source generator and web-spa code that ships in the template changed, so the
  template output changed. The recorded gates list `dev build`, the targeted tests, the audit, and
  `lint-skills`, but not `dev template-smoke`. AGENTS.md lists it for template-output changes.
- Suggestion: Rely on CI. The PR CI gate runs the default-variant template-smoke. The change
  touches no `#if` region and no template symbol, so `--matrix` is not needed.
- Status: open
