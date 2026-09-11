# Emit less per-contract mixin members

## Parent

053

## Description

Mixin emit is larger than the skill needs:

- Parameterless `GetRoute()` always interpolates
  `FormattableString.Invariant($"api/Roles")` even for static templates.
- Parameterized + parameterless overloads duplicate the template string.
- `[AuthApiRequest]` always emits **private** `GetAuthQueryParameters()` —
  only list queries that implement `IQueryStringRouteProvider` compose it
  (`GetRoles`, `ListPrincipals`). POST / GET-by-id should keep using
  **manual** `: IAuthApiRequest` + `UserId` (skill: two forms).
- Property types use bare `Guid` / `DateTime` (implicit usings), not
  `global::`.

Shrink emission. Do not change route parsing (**053-003**).

## Requirements

- Static templates: `GetRoute()` returns `RouteTemplate` (no interpolation).
- Parameterized `GetRoute(...)` only when the template has `{params}`.
- Emit `GetAuthQueryParameters` only when the same partial is (or should
  be) an `IQueryStringRouteProvider` / also has `[OpenDataQueryParameters]`.
  Do **not** force every `[AuthApiRequest]` onto query-string form.
- Use `global::System.Guid` (and peers) in generated members.
- Existing hosted contracts still compile; GetRoles still composes auth +
  OData query helpers.

### Not in scope

- Parser (**053-003**), FQN attributes (**053-004**), SyntaxProvider
  incrementality (**053-005**)

## Checklist

- [x] Static GetRoute is a const return
- [x] Auth query helper only on query-string contracts
- [x] `global::` types in generated members
- [x] Roles list + create still generate correctly
- [x] Results + How to validate
- [x] Review disposition (clean, 1 round)

## Session

- Created: 2417840 (2026-09-09)
- Cockpit: timewarp-flow Grok `01a03d38-9611-7620-aae5-848e15dafa94`
- Implementer: Grok session `01a09097-530c-7930-9ae2-e4969eb17d93` (2026-09-11)
- Review oracle: Grok session `01a090a1-3a9b-7040-8fb5-72582626315e` (2026-09-11)

## Notes

- File: `source/foundation/foundation-contracts-generators/contracts-mixin-generator.cs`
- Skill: `[AuthApiRequest]` vs manual `IAuthApiRequest` are two forms;
  do not collapse them.
- Review kitchen: `review/review-framework.md`, `review/round-1/`,
  `review/disposition.md`.

## Results

Mixin emit is smaller without collapsing the two auth forms or changing
route parsing.

**What**

- Static `[ApiRoute]` templates emit `GetRoute() => RouteTemplate` (no
  `FormattableString` interpolation).
- Parameterized templates emit `GetRoute(...)` plus a parameterless
  `GetRoute()` that forwards to it, so the format string is not duplicated.
- `[AuthApiRequest]` always emits `UserId`. `GetAuthQueryParameters` only
  when the same type is `IQueryStringRouteProvider` or also has
  `[OpenDataQueryParameters]`. Merge upgrades the auth body when OpenData
  lands on a later pipeline.
- Generated `guid` / `datetime` members use `global::System.Guid` /
  `global::System.DateTime`.

**Files**

- `source/foundation/foundation-contracts-generators/contracts-mixin-generator.cs`
- `tests/analyzers/timewarp-architecture-sourcegenerator-tests/contracts-mixin-generator-tests.cs`
- `skills/tw-web-api-contracts/SKILL.md`
- `documentation/developer/how-to-guides/web-api-contracts/how-to-write-bff-api-contracts.md`

**Decisions**

- Manual `: IAuthApiRequest` + `UserId` stays the POST / GET-by-id form
  (`CreateRole`, `GetRole`). The attribute form is not forced onto
  query-string composition.
- Hosted list queries that already compose the helper keep it:
  `GetRoles` (auth + OpenData), `ListPrincipals` and `GetCredentials`
  (`IQueryStringRouteProvider`).

**Tests**

- `ContractsMixinGenerator_Tests`: 17 passed (15 prior + 2 new).
- Sourcegenerator suite: 76 passed (74 prior + 2).
- Generator project: 0 Warning(s) 0 Error(s).
- `web-contracts` and `api-contracts`: 0 Warning(s) 0 Error(s).

### How to validate

**Smoke**

```bash
cd tests/analyzers/timewarp-architecture-sourcegenerator-tests
dotnet test -c Release -- --filter-class ContractsMixinGenerator
dotnet test -c Release -- --filter-method Should_Emit_Static_GetRoute_As_RouteTemplate_Return
dotnet test -c Release -- --filter-method Should_Emit_GetAuthQueryParameters_Only_For_Query_String_Contracts
```

From the repo root:

```bash
dotnet build source/container-apps/web/projects/web-contracts/web-contracts.csproj -c Release \
  -p:EmitCompilerGeneratedFiles=true -p:CompilerGeneratedFilesOutputPath=obj/generated --no-incremental
```

Inspect:

- `obj/generated/foundation-contracts-generators/TimeWarp.Foundation.Contracts.Generators.ContractsMixinGenerator/TimeWarp.Architecture.Features.Admin.Roles.GetRoles.Query.g.cs`
- `…/CreateRole.Command.g.cs`
- `…/GetRole.Query.g.cs`

**Expect**

- Mixin class: 17 passed (0 failed), including:
  - `Should_Emit_Static_GetRoute_As_RouteTemplate_Return` — `GetRoute() => RouteTemplate;` and no `FormattableString.Invariant` on `api/Roles`
  - `Should_Emit_GetAuthQueryParameters_Only_For_Query_String_Contracts` — `[AuthApiRequest]` alone has `UserId` and no helper; Auth+OpenData and Auth+`IQueryStringRouteProvider` emit `GetAuthQueryParameters()`
  - Existing parameterized cases emit `GetRoute() => GetRoute(...)` and `global::System.Guid` / `global::System.DateTime`
- GetRoles generated source: `GetRoute() => RouteTemplate;`, `GetAuthQueryParameters()`, `GetOpenDataQueryParameters()`
- CreateRole generated source: `GetRoute() => RouteTemplate;` and no `IAuthApiRequest` / `GetAuthQueryParameters` (manual form stays in the hand-written partial)
- GetRole generated source: `GetRoute(global::System.Guid RoleId)` plus `GetRoute() => GetRoute(RoleId);`

**Automated gate**

```bash
cd tests/analyzers/timewarp-architecture-sourcegenerator-tests && dotnet test -c Release
# expect: 76 passed (74 prior + 2 emit-shrink tests)

dotnet build source/foundation/foundation-contracts-generators/foundation-contracts-generators.csproj -c Release
# expect: Build succeeded. 0 Warning(s) 0 Error(s)

dotnet build source/container-apps/web/projects/web-contracts/web-contracts.csproj -c Release
dotnet build source/container-apps/api/projects/api-contracts/api-contracts.csproj -c Release
# expect: Build succeeded. 0 Warning(s) 0 Error(s)
```

**Not in scope:** route parser (053-003), public FQN attributes (053-004),
SyntaxProvider incrementality (053-005), `dev template-smoke`.

**Review disposition:** clean (0 open). Effort 1, general only. 1 round.

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

- No findings. Static `GetRoute() => RouteTemplate`, parameterized forwarder, auth helper gated to query-string contracts, and `global::` Guid/DateTime match the brief. Hosted GetRoles / ListPrincipals / GetCredentials keep the helper; CreateRole and GetRole stay on the manual form.
- Paths: `review/review-framework.md`, `review/round-1/merged.md`, `review/disposition.md`. No wontfix; no escalation.
