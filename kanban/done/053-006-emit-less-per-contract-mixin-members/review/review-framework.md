# Review framework — task 053-006

**Date:** 2026-09-11
**Host task:** kanban/in-progress/053-006-emit-less-per-contract-mixin-members/
**Diff scope:** branch `task/053-006-emit-less-per-contract-mixin-members` vs `origin/master` (product commit `79f0ec11`, Results `97803e89`)
**Plan / brief:** Shrink per-contract mixin emit. Static `[ApiRoute]` templates emit `GetRoute() => RouteTemplate` (no `FormattableString` interpolation). Parameterized templates emit `GetRoute(...)` plus a parameterless `GetRoute()` that forwards so the format string is not duplicated. `[AuthApiRequest]` always emits `UserId`; `GetAuthQueryParameters` only when the same type is `IQueryStringRouteProvider` or also has `[OpenDataQueryParameters]`. Generated `guid` / `datetime` members use `global::System.Guid` / `global::System.DateTime`. Do not collapse the two auth forms. Out of scope: route parser (053-003), public FQN attributes (053-004), SyntaxProvider incrementality (053-005).
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** Review oracle Grok session `01a090a1-3a9b-7040-8fb5-72582626315e` (2026-09-11)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
