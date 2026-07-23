# Task 107 — General review, round 1

Reviewer: general (claude). Commit under review: `52ff73e2`.
Verification: read all 12 changed files + traced real web/api contract shapes and markers;
ran `dotnet fixie tests/analyzers/timewarp-architecture-sourcegenerator-tests` (49 passed).
Did not run the standalone yarp project or the aspire-tests (trusted the commit's claim; see G3).

## Findings

### G1 — Generator has a silent-empty failure mode (the drift class it exists to prevent)
- sev: MEDIUM (robustness / hardening)
- status: open
- file: source/analyzers/timewarp-architecture-analyzers/generators/ingress-route-prefix-generator.cs:107-124, 332-333
- description: The source set is selected by matching `assembly.Name` against
  `IngressWebContractAssemblies` (`web-contracts`). If that name ever fails to match a referenced
  assembly — a typo in the csproj property, an added `<AssemblyName>` override (web-spa already does
  exactly this: assembly `Web.Spa`, not `web-spa`; see the repo memory note), or a template rename in
  a generated app — then web-contracts falls into the `isForeignContracts` branch
  (`name.Contains("contracts")` is still true), `webRoutes` is empty, and the generator emits
  `WebServerApiRoutePrefixes.All = ImmutableArray<string>.Empty` **with no diagnostic**. The ingress
  then silently loses every `/api` carve-out and reintroduces the exact 104-003 drift (every web
  `/api/*` path falls to the Api.Server catch-all → 404). The generator deliberately treats
  "enabled + empty" as valid (for flag combos with no participating contract), so it cannot itself
  tell a legitimate empty from a misconfigured one. In this repo the aspire-tests would catch it, but
  a generated app has no such guard — and the whole point of the task is that the pit of success does
  the work.
- fix: Emit a diagnostic when `EnableIngressRouteGeneration=true` and a name listed in
  `IngressWebContractAssemblies` matches **no** referenced assembly (unambiguous misconfiguration —
  distinct from a genuinely empty source assembly). Verified today the assembly name IS `web-contracts`
  (no `<AssemblyName>` override in web-contracts.csproj), so this is hardening, not a live bug.

### G2 — ClientOnlyContract exclusion is checked on the wrong class vs. real contracts; test masks it
- sev: LOW
- status: open
- file: ingress-route-prefix-generator.cs:230-235; tests/.../ingress-route-prefix-generator-tests.cs:93-100
- description: The generator excludes `[ClientOnlyContract]` by inspecting the **outer** `[ApiEndpoint]`
  wrapper type. But every real client-only contract places `[ClientOnlyContract]` on the **nested**
  request class and carries **no** `[ApiEndpoint]` at all: `GetCurrentUser`
  (get-current-user-contracts.cs — no `[ApiEndpoint]`, `[ClientOnlyContract]` on `Query`),
  `GetSignInToken` (same), `CreateTodoItem`/`UpdateTodoItem` (same). So in reality these are excluded
  by the *missing-`[ApiEndpoint]`* filter (line 223-228), and the outer `[ClientOnlyContract]` check
  (230-235) never fires for any real contract. The unit test models `GetCurrentUser` as
  `[ApiEndpoint]` + `[ClientOnlyContract]` **on the outer class** — a shape no real contract uses and
  that is contradictory under the repo convention (`[ApiEndpoint]` XOR `[ClientOnlyContract]`, TWA0006).
  Net effect: the ClientOnlyContract branch is effectively dead for real inputs; a hypothetical
  `[ApiEndpoint]`-outer + `[ClientOnlyContract]`-nested contract would be wrongly *included*.
- fix: Either drop the redundant outer check and rely on the `[ApiEndpoint]` filter + TWA0006, or also
  scan the nested request class; and change the test sample to the real shape (no `[ApiEndpoint]`,
  `[ClientOnlyContract]` on the nested `Query`) so it exercises the path that actually runs. Output is
  correct today regardless (GetCurrentUser is excluded either way), hence LOW.

### G3 — Standalone yarp route-merge is build-verified only (verdict requested)
- sev: MEDIUM (coverage gap) — verdict: acceptable to ship, file a follow-up
- status: open
- file: source/container-apps/yarp/program.cs:67-91; source/container-apps/yarp/appsettings.Development.json
- description: The standalone gateway adds the generated routes via
  `reverseProxy.LoadFromMemory(generatedWebRoutes, Array.Empty<ClusterConfig>())` alongside
  `LoadFromConfig(...)`, relying on YARP merging two `IProxyConfigProvider`s and an in-memory route
  resolving the config-defined `Web.Server` cluster cross-provider. The Design region states this was
  spiked manually ("verified: a memory route resolves a cross-provider cluster"), but **no automated
  test exercises the standalone yarp project** — the aspire-tests drive the AppHost YARP, a different
  code path. A regression (YARP version change dropping multi-provider merge, or the empty-clusters
  interaction) would ship green. Route precedence (literal `/api/identity/{**catch-all}` beating
  `/api/{**catch-all}`) and the https→http Development cluster change are both sound and consistent
  with the AppHost path; the only untested piece is the cross-provider merge itself.
- fix: Ship as-is (the AppHost is the dogfooded/verified public chain, task 112), but file a follow-up
  task for a standalone-yarp runtime smoke (boot the yarp project against a stub Web.Server, assert an
  `/api/identity/*` path routes to it rather than the Api.Server catch-all).

### G4 — Casing-dependent output when one segment appears in two cases
- sev: LOW (theoretical)
- status: open
- file: ingress-route-prefix-generator.cs:127-156
- description: `seen` dedupes `OrdinalIgnoreCase` while `prefixes` sorts `Ordinal`; if two contracts
  declared the same top-level segment in different cases (e.g. `api/Roles` and `api/roles`), which
  casing is emitted depends on referenced-assembly / member enumeration order (first-seen wins).
  Deterministic within a build; only matters if such a collision ever exists (none today).
- fix: None required; note only.

## Clean statements (verified, no issue)

- **Collapse edge cases — clean.** Trailing slashes handled (`Trim('/')`), double slashes handled
  (`Split(..., RemoveEmptyEntries)` → `api//x` = `api/x`), parameterized-second-segment and bare-`api`
  correctly routed to TWA0018. Query fragments do not occur in real `[ApiRoute]` templates (path-only);
  purely theoretical.
- **TWA0017 shadow direction — clean.** Exact foreign match detected via `Equals(prefix)`; deeper
  foreign routes (`api/weatherforecast/daily` under prefix `api/weatherforecast`) detected via
  `StartsWith(prefix + "/")`. `api/...` web routes never falsely trip the reserved-prefix check
  (`segments[0]` is `api`, reserved list is `grpc`). Real build has no collision: web prefixes are
  {api/Hello, api/Roles, api/Users, api/identity}, api-contracts declares only `api/weatherforecast`.
- **grpc participation — clean/answered.** The AppHost does **not** reference grpc-contracts; grpc is
  guarded solely by `IngressReservedPathPrefixes=grpc` (reserved-prefix check on web routes), which is
  the correct tool since grpc routes use the `grpc/` prefix, not `api/`. foundation-contracts and
  grpc-contracts carry no `[ApiEndpoint]`, so the foreign/collision set is exactly api-contracts — no
  spurious TWA0017. `dotnet build` 0/0 (per commit) is consistent with this.
- **Behavior delta /api/Roles — clean & safe.** All five role endpoints (Get/GetRoles/Create/Update/
  Delete) are `[ApiEndpoint]` + `[EndpointAuthorize(Policy="identity-session-authenticated")]` on
  Web.Server. Unauthenticated → 401; Api.Server hosts no `/api/Roles`, so pre-fix they 404'd through
  the ingress. Routing them to Web.Server is the intended fix (they were unreachable-by-accident, not
  blocked-by-design); the smoke fact's 401-not-404 assertion is the correct guard.
- **Behavior delta /api/GetCurrentUser removal — clean & safe.** GetCurrentUser has no `[ApiEndpoint]`
  and no server-side handler/endpoint (grep of web-server/ and api/ = none); its only consumer is the
  SPA client mock-mode (`using static GetCurrentUser` in web-spa authorization-state). Dropping its
  ingress carve-out cannot break a server fetch — there was never one. `Analytics/TrackEvent` is
  `[ApiEndpoint]` + `[EndpointAllowAnonymous]` but non-`api`; correctly skipped (served by the Web.Server
  catch-all), not an ingress carve-out.
- **Template shapes — clean.** `<!--#if (web) -->` guards the generation PropertyGroup, the
  CompilerVisibleProperty ItemGroup, the web-contracts ProjectReference, and the generator attach in
  both aspire-app-host.csproj and yarp.csproj; api-contracts guarded `#if (api)`;
  IngressReservedPathPrefixes + its CompilerVisibleProperty guarded `#if (grpc)`. Package-mode attach
  is correct dual-mode (`ProjectReference OutputItemType=Analyzer` when `UseAnalyzerPackages != true`,
  `PackageReference $(TwArchitectureGeneratorsPackageId) PrivateAssets=all` otherwise).
  `ExcludeAssets=runtime`/`Private=false` on the contract refs (metadata-only, not a runtime dep of the
  orchestrator) follows the foundation-contracts precedent.
- **TWA0010 — clean.** yarp/program.cs newly introduces `#if web`; yarp.csproj newly adds
  `$(DefineConstants);web` inside the same `<!--#if (web) -->` block, so the flag is defined wherever
  the C# directive is active and compiled out together in `--web false`. AppHost already defines
  `api;web;grpc;yarp;postgres`.
- **TWA0008 hygiene — clean.** No template-conditional tokens leaked into new comments/strings.
- **Global-namespace emission + always-emit — clean.** Emits `WebServerApiRoutePrefixes` in the global
  namespace (task 115 sourceName-rewrite lesson) and always `AddSource` when enabled, with
  `ImmutableArray<string>.Empty` for the no-route case, so the AppHost/yarp `foreach` compiles in every
  flag combo (covered by `Should_Emit_Empty_All_When_Enabled_With_No_Participating_Contracts`).
- **Incrementality/determinism — clean (acceptable).** `CompilationProvider.Combine(options)` →
  `RegisterSourceOutput`; re-runs the referenced-assembly scan each compile (not finely cached), same
  design as the sibling FastEndpointSourceGenerator. No `DateTime`/random; output sorted `Ordinal`, so
  deterministic (modulo G4's casing edge). Generator wraps `Execute` in try/catch (CA1031) so it can't
  break the compilation.
- **Tests assert what they claim — clean.** The 9 generator cases cover collapse+Ordinal ordering,
  identity 5→1 collapse, client-only exclusion, non-api skip, disabled gate, empty-emit, TWA0017
  (foreign shadow AND reserved grpc), and TWA0018 (count==2 + empty output). 49/49 passed locally.
  Smoke facts (ingress-smoke-tests.cs) assert real backend reach via body content, and 401-not-404 /
  200 for the two drift shapes. (Caveat: see G2 — the client-only test sample doesn't match real
  contract shape.)

## Summary

- Blockers: 0
- Medium: 2 (G1 silent-empty failure mode; G3 standalone-yarp coverage gap — ship + follow-up)
- Low: 2 (G2 client-only check vs. real shape / test fidelity; G4 casing determinism edge)
- Clean areas confirmed: generator collapse, TWA0017/0018 semantics, grpc handling, both behavior
  deltas, template/flag shapes, dual-mode attach, TWA0008/0010 hygiene, global-namespace + always-emit,
  determinism, tests.
- Recommendation: no blocker to merge. Address G1 (a "named source assembly not found" diagnostic) as
  cheap hardening that closes the one silent-drift path the design still leaves open; file the G3
  follow-up; G2/G4 are polish.
