# Findings — task 131 full-repo code review (Kimi K3)

Ordered by the `/code-review` priority: structural regressions → missed judo → spaghetti → boundaries → size → modularity → legibility. Every finding answers "delete/reframe what?".

Severity counts: **1 blocker · 6 major · 8 minor · 2 note**

---

## F-001 — blocker — Platform package echoes a secret to console

**Area:** foundation-server (ships in `TimeWarp.Foundation.Server` NuGet)
**Path:** `source/foundation/foundation-server/common-server-module.cs:126,130,154`

**Wrong:** `ConfigureAzureAppConfig` does `Console.WriteLine($"connectionString: {connectionString}")` — printing the Azure App Config connection string (a credential) to stdout whenever one is configured. Flanking debug WriteLines ("No AppConfig ConnectionString", "App Config value TestValue") add noise. This ships in the published platform package — every consumer's logs capture a secret.

**Why it matters:** secret leakage into logs is a security defect, not a style nit; library code must not Console.Write at all (hosts own logging — web-server already wires Serilog).

**Remedy:** delete all three WriteLines (do not "redact and keep" — the module needs no console output). Sweep the foundation tree for other `Console.WriteLine` while there.

**Follow-on:** `Stop echoing AppConfig connection string from CommonServerModule`

---

## F-002 — major — Dead MVC bridge still ships in the platform package (plus its analyzer jurisdiction and stale TODOs)

**Area:** foundation-server + convention-analyzers
**Path:** `source/foundation/foundation-server/base/base-endpoint.cs:19`; `source/analyzers/timewarp-architecture-convention-analyzers/endpoint-coverage-analyzer.cs:15,73,102`; `source/foundation/foundation-server/base/base-fast-endpoint.cs:14`; `source/foundation/foundation-server/common-server-module.cs:59`

**Wrong:** `BaseEndpoint<TRequest,TResponse> : ControllerBase` has zero product references (post-109, confirmed: only the analyzer's metadata-name match and a comment reference it). Yet it ships in `TimeWarp.Foundation.Server`; the endpoint-coverage analyzer keeps a live MVC jurisdiction for it (TWA0005 "applies to MVC BaseEndpoint subclasses only"); both bridge files carry `// TODO: Review this code. Why not inject ISender?` that their own Design regions already answer (ctor-free for generated endpoints); `CommonServerModule` still configures `Mvc.JsonOptions` whose only consumer was the MVC bridge. The Design regions on both bridges even say "keep their semantics aligned" — a dual-maintenance pact for a dead half.

**Why it matters:** dead abstraction + supporting analyzer surface + misleading TODOs ship to every generated app as "the pattern."

**Remedy:** delete `base-endpoint.cs`, the MVC branch of the coverage analyzer (TWA0005 keeps the BaseFastEndpoint-generated path), the stale TODOs, and the `Mvc.JsonOptions` configuration (HttpJsonOptions covers the live path — verify no MVC consumer remains first).

**Follow-on:** `Delete dead MVC BaseEndpoint bridge and its analyzer jurisdiction`

---

## F-003 — major — FastEndpoint generator keeps route-conflict state in a static cross-compilation dictionary

**Area:** generators
**Path:** `source/analyzers/timewarp-architecture-analyzers/validation/route-registry.cs:17,39-42`; used at `generators/fast-endpoint-source-generator.cs:47,158`

**Wrong:** `RouteRegistry` is a static `ConcurrentDictionary` with a `Reset()` called at generator `Initialize`. In the compiler-server process all compilations share that static: web-server and api-server both run this generator in one solution build, and concurrent project builds interleave — one compilation can `Reset()` mid-registration of another, or false-conflict on another project's routes (two different servers may legitimately host the same route). Stale entries from a prior compilation in the same process can also false-conflict on rebuild.

**Why it matters:** diagnostics that depend on build concurrency are non-deterministic; global mutable state in a generator is the classic "magic shared state" defect.

**Remedy:** delete the static registry. Collect `(route, verb, className)` in the incremental pipeline (`.Collect()`) and report duplicates within that per-compilation batch — deterministic, incremental-friendly, and `Reset()` disappears.

**Follow-on:** `Make FastEndpoint route-conflict detection per-compilation`

---

## F-004 — major — Hosted-route discovery rule is triplicated, kept consistent by a comment

**Area:** generators + convention-analyzers
**Path:** `source/analyzers/timewarp-architecture-analyzers/models/endpoint-metadata.cs:55-99`; `source/analyzers/timewarp-architecture-analyzers/generators/ingress-route-prefix-generator.cs:248-291` (`EnumerateHostedRoutes`); `source/analyzers/timewarp-architecture-convention-analyzers/endpoint-coverage-analyzer.cs`

**Wrong:** the participation rule "outer `[ApiEndpoint]` + nested Query/Command `[ApiRoute]` − `[ClientOnlyContract]`" is implemented three times (FastEndpoint metadata extraction, ingress route enumeration, endpoint-coverage analyzer). The ingress generator's Design region states the invariant as prose: "participate rule matches EndpointMetadata.FromSymbol … minus ClientOnlyContract" — convention-by-comment, the drift class this repo keeps paying for (104-003, 115). `GetAllNamespaces` is also copy-pasted in both generators.

**Why it matters:** a fourth consumer (or a rule change) must edit three Roslyn walkers in lockstep; one missed edit silently desyncs which contracts get endpoints vs ingress routes vs coverage.

**Remedy:** one shared `HostedRouteDiscovery` helper in the analyzers package (symbol-in, routes-out, with flags for client-only exclusion) consumed by all three; delete the duplicate `GetAllNamespaces`.

**Follow-on:** `Extract shared hosted-route discovery for generators/analyzers`

---

## F-005 — major — `[ApiEndpoint(EndpointType = …)]` is a public API that silently no-ops

**Area:** attributes package (public) + generator
**Path:** `source/analyzers/timewarp-architecture-attributes/api-endpoint-attribute.cs:17`; `source/analyzers/timewarp-architecture-analyzers/models/endpoint-metadata.cs:53,128-140`; `source/analyzers/timewarp-architecture-analyzers/generators/fast-endpoint-source-generator.cs:233`

**Wrong:** the shipped attribute documents "EndpointType optionally overrides the generated endpoint's base class," but the extraction code sets `metadata.CustomEndpointType = null` **unconditionally — including when the named argument is present** — so the generator's `CustomEndpointType?.FullName ?? "BaseFastEndpoint"` always emits `BaseFastEndpoint`. (`System.Type` is also the wrong currency for a generator model: a referenced-assembly `Type` cannot be materialized at generation time; the symbol's display string is what's needed.)

**Why it matters:** a documented public platform API that silently ignores its input is worse than no API — contract authors will set it and get no error, no warning, and no effect.

**Remedy (pick one):** (a) implement: read the `TypedConstant`'s `INamedTypeSymbol`, emit its fully-qualified display string as the base type; or (b) delete the property from the attribute and the dead extraction/emission path. Deletion is the judo move unless a consumer is waiting on it.

**Follow-on:** `Implement or delete ApiEndpointAttribute.EndpointType`

---

## F-006 — major — Identity handler problem-factories and ceremony preamble duplicated across the exemplar slice

**Area:** web features/identity (the template's flagship slice — the style every generated app copies)
**Path:** e.g. `source/container-apps/web/features/identity/add-passkey/add-passkey-handler-application.cs:136-169`; same 4–6 factories in `add-agent-key`, `revoke-credential`, `get-credentials`, `complete-passkey-registration`, `complete-agent-key-registration`, `complete-passkey-authentication`, `complete-agent-token-issuance` handlers

**Wrong:** private static `SharedProblemDetails` factories are copy-pasted per handler — `Unauthenticated` ×4, `ChallengeInvalid` ×4 (registration wording), `MalformedPayload` ×6, `CredentialAlreadyRegistered` ×3, full agent-key/passkey helper sets ×2, `Quarantined` ×2. The ceremony ladder (decode triple → challenge consume → verify → handle-exists → create/attach) is near-identical between `add-passkey` and `complete-passkey-registration` (~25 lines) and `add-agent-key` ↔ `complete-agent-key-registration`; the Design regions themselves say "Order mirrors CompletePasskeyRegistration.Handler exactly" — consistency by comment.

**Why it matters:** ~200 duplicated lines in the slice that defines "how we write handlers" for every generated app; a wording fix must be made in N places.

**Remedy:** one `IdentityProblems` statics type in the identity application layer (problems are pure data — trivially shareable); extract the decode/consume/verify preamble into a small helper per ceremony family where the ladder truly matches. Keep the per-handler differences (principal source, session issuance) — do not merge handlers.

**Follow-on:** `Collapse identity handler problem-factory and ceremony-preamble duplication`

---

## F-007 — major — dev-cli smoke commands duplicate ~200 lines and bypass the 126-006 SSOT derivation

**Area:** tools/dev-cli
**Path:** `tools/dev-cli/endpoints/template-smoke-command.cs:85-91,270-272,391-427,710-768`; `tools/dev-cli/endpoints/template-publish-smoke-command.cs:60-76,397-489,583-657`

**Wrong:** (1) `ForbiddenRewrittenPackageFragments` is hand-maintained identically in BOTH files — the exact drift class 126-006 eliminated for the pre-generate scan by deriving suffixes from `msbuild/timewarp-platform-packages.props`; the two post-generate checks and the `InstallTemplate` nupkg filter (three `.Contains(".Analyzers.")` lines) still use hand lists, so a new platform suffix property auto-covers the pre-scan but silently misses the post-generate gates. (2) `AssertPackageIdsNotRewritten` (~50 lines) is near-verbatim in both files, including an inline bin/obj skip that duplicates template-smoke's own `IsBinObjOrArtifacts` helper (and misses `artifacts`). (3) `SmokePinnedPackageIdFragments` ≡ `PlatformPinIncludeFragments` — same list, two names. (4) `SmokeOneAsync` skeleton (generate → asserts → find solution → restore → build) duplicated. (5) publish-smoke has **no** namespace-literal scan at all.

**Why it matters:** the release gates are the repo's highest-stakes tooling; drift between them means the publish gate can pass what the smoke gate would fail.

**Remedy:** one shared smoke-harness file (assert helpers + suffix derivation reused from 126-006's regex + generate/build skeleton) consumed by both commands; derive all suffix lists from the props SSOT.

**Follow-on:** `Extract shared template-smoke harness and derive all rewrite-scan suffixes from props SSOT`

---

## F-008 — minor — Unknown HTTP verbs silently become GET in generated endpoints

**Path:** `source/analyzers/timewarp-architecture-analyzers/models/endpoint-metadata.cs:230-241`

**Wrong:** `ConvertHttpVerbToMethodName` identity-switches the five known verbs and falls through to `"Get"` for anything else — the same fail-open default the enum-member-name fix (documented 10 lines above it) was built to kill. If `HttpVerb` grows (Head, Options), endpoints silently mount as GET.

**Remedy:** report an SG diagnostic (or throw into the generator's SG001 catch) on an unrecognized verb instead of guessing GET.

**Follow-on:** fold into F-003/F-004 generator task, or `Fail fast on unknown HttpVerb in endpoint metadata`

---

## F-009 — minor — Generated apps ship with mock authentication compiled in; MSAL/AzureAd path is dead unless manually revived

**Path:** `source/container-apps/web/projects/web-spa/web-spa.csproj:47` (unconditional `MOCK_AUTHENTICATION`); `source/container-apps/web/projects/web-spa/program.cs:54-68`; `AzureAd` appsettings sections (web-server + both web integration-test projects)

**Wrong:** `MOCK_AUTHENTICATION` is always defined, so every generated app builds the `MockAuthenticationStateProvider` branch; the `AddMsalAuthentication`/`AzureAdB2C` branch and AzureAd config sections are dead weight in new apps. Zero-setup is plausibly intentional (passkey identity is the real story), but today the template ships a dead auth stack alongside the live one with no in-template explanation.

**Remedy:** tracked — **104-021** ("flags + Entra non-default") owns the Entra posture. This finding asks only that the disposition make the mock-default an explicit documented choice (csproj comment exists; the AzureAd appsettings residue should go with 104-021).

**Tracked by:** #104-021

---

## F-010 — minor — Template chrome fossils: B2C/PWA comment-conditionals, stale TODOs, and a MediatR marketing link

**Path:** `source/container-apps/web/projects/web-server/components/App.razor:41-43,45,53-56`; `source/container-apps/web/projects/web-spa/features/application/pages/HomePage.razor:33`; `source/container-apps/web/projects/web-server/program.cs:147,165-166`

**Wrong:** (1) `<!--#if B2C -->` / `<!--#if PWA -->` reference symbols absent from template.json — the engine strips the regions, so the MSAL-script exemplar never ships and the PWA block is doubly dead (commented content inside a stripped region); two "Cramer review" TODOs record a decision never made. (2) HomePage.razor tells every generated app's users the CQRS stack is `jbogard/MediatR` — wrong project (TimeWarp.Mediator fork), wrong link. (3) web-server program.cs carries a typo'd stale TODO ("seesm like could just pass whole config???") and commented-out `AddRazorPages`/`AddServerSideBlazor` lines.

**Remedy:** delete the fossils and dead comments; point the HomePage link at TimeWarp.Mediator (or drop the line); the B2C/PWA posture decision rides with 104-021 (F-009).

**Follow-on:** `Remove B2C/PWA fossils and stale links from template chrome`

---

## F-011 — minor — template.json excludes postgres by per-file enumeration instead of a folder glob

**Path:** `.template.config/template.json:76-86`

**Wrong:** five individual `platform/postgres/*` files are enumerated (plus ef-principal-store, plus a test glob). Every other flag excludes by folder glob (`source/container-apps/grpc/**` etc.). A sixth postgres platform file would silently ship into `--postgres false` apps — caught by the SmokeNoPostgres build, but the enumeration style is needlessly fragile.

**Remedy:** replace the five file entries with `source/container-apps/web/platform/postgres/**`; keep the ef-principal-store entry (it lives in features/identity).

**Follow-on:** `Glob the postgres platform tree in template.json excludes`

---

## F-012 — minor — tests/Directory.Build.props re-detects `UseAnalyzerPackages` and duplicates analyzer wiring

**Path:** `tests/Directory.Build.props:40-59` vs `Directory.Build.props:18-19` and `source/Directory.Build.props:36-47`

**Wrong:** the root props (post-124) already computes `UseAnalyzerPackages`; tests/ re-runs the same existence detection (dead no-op with a stale "Mirror" comment) and duplicates the project-ref/package-ref ItemGroup from source/DBP (~20 lines).

**Remedy:** delete the redundant detection block (keep the wiring — source/DBP genuinely doesn't reach the tests tree — or extract the shared wiring into one imported `msbuild/*.props` consumed by both).

**Follow-on:** fold into F-007 tooling task, or `Remove redundant analyzer detection from tests/Directory.Build.props`

---

## F-013 — minor — Grammar analyzer path-scoping has dead/redundant branches and speculative machinery

**Path:** `source/analyzers/timewarp-architecture-convention-analyzers/feature-filename-grammar-analyzer.cs:191-220,353-416`

**Wrong:** (1) L191-198 ternary's arms are identical (`collapsed` is by definition `CollapseDotDot(normalized)`). (2) L215-216's `/container-apps/{family}/features/` check is subsumed by `/{family}/features/` (substring). (3) `TryFindIncompleteMultiSegmentFunction` (~60 lines) guards multi-segment registered functions — the registry (`feature-filename-grammar.json`) contains only single-segment `handler`/`endpoint`, so the loop body is unreachable today.

**Remedy:** collapse the redundant conditions (~15 lines); delete the multi-segment machinery until a multi-segment function is actually registered (YAGNI), or register one and test it.

**Follow-on:** `Simplify grammar analyzer path scoping and drop speculative multi-segment branch`

---

## F-014 — minor — Diagnostic ID taxonomy drift: TWE006/SG001-002/SG010-011 live outside the documented TWA surface

**Path:** `source/analyzers/timewarp-architecture-analyzers/generators/typed-id-source-generator.cs:41-63`; `fast-endpoint-source-generator.cs:50-69`; `AGENTS.md` TWA table

**Wrong:** three ID prefixes coexist — TWA0001–0019 (documented in AGENTS.md), TWE006 (typed-id shape, an **Error** that enforces a convention but isn't in the table), and SG001/SG002/SG010/SG011 (generator logs). A contributor reading the enforcement table can't discover TWE006 or the SG IDs.

**Remedy:** document the TWE/SG IDs in the AGENTS.md table (or rationalize TWE006 into TWA numbering — it is a convention error, not a generator log).

**Follow-on:** `Document TWE/SG diagnostic IDs in AGENTS.md`

---

## F-015 — minor — BaseApiService ↔ TestApiService transport mirror (+ swallow-all catch, verb-matrix asymmetry)

**Path:** `source/container-apps/web/projects/web-spa/services/api-services/base-api-service.cs:140-161,186-188,192-228`; `tests/common/timewarp-testing/web-api-test-service/test-api-service.cs`

**Wrong:** ~43 shared lines (verb dispatch, problem mapping incl. the 204/499 specials, route/content prep); TestApiService's own Design comment says it "mirrors" BaseApiService because tests can't reference the SPA stack. Also: `HandleProblemResponse` swallows all exceptions with `// TODO: Log the error`; `PrepareContent` handles Head/Options while the dispatcher throws `NotImplementedException` for them.

**Remedy:** extract a shared transport core (HttpClient + seam options + token-acquisition delegate) both sides compose; delete the TODO by logging or by honest comment; make the verb matrix consistent (support via `HttpMethod` or throw `NotSupportedException` in both places).

**Follow-on:** `Share the API transport core between SPA and test host`

---

## F-016 — note — Shared constants parked in the bare `TimeWarp.Architecture.Features` namespace

**Path:** `source/container-apps/web/features/admin/modules/module-ids-contracts.cs`; `source/container-apps/web/features/authorization/role-ids-contracts.cs`

**Wrong:** two cross-slice constant files sit at the root Features namespace with no slice Id — the only product files outside a `Features.<Id>` home (soft TWA0009 shape; the analyzer tolerates them). Plausibly a deliberate shared-kernel choice, but it's undocumented and unprincipled: the next shared constant has no rule to follow.

**Remedy:** make it a decision, not an accident: either document "cross-slice constants live at bare `Features`" in the placement skill/AGENTS.md, or move them into an explicit shared contracts home.

**Follow-on:** `Decide and document the home for cross-slice shared constants`

---

## F-017 — note — Demo/scaffold residue cluster (small, batchable)

**Path:**
- `source/container-apps/api/projects/api-server/generic-pipeline-behavior.cs:17,19` — placeholder behavior `Console.WriteLine`s on every request; lives in the api-server artifact folder (only non-bootstrap logic file there); asymmetric (web-server has no such exemplar)
- `source/container-apps/web/features/v2/overview.md` — empty v2 slice stub, two-line doc ("add the contracts under this folder")
- `tests/common/timewarp-testing/constants.cs` — dead `ExampleConst` placeholder + commented leftover
- `source/container-apps/aspire/projects/aspire-app-host/program.cs:134-146` — comment-only `#if web` block (empty directive pair) adjacent to a real one
- `source/container-apps/{web,api,grpc}/msbuild/feature-membership.targets` — api/grpc carry the "both trees may be absent" comment paragraph; web lacks it (generated-sibling drift)
- `AGENTS.md` — says api `platform/` is "empty (no content yet)"; the directory is actually absent

**Remedy:** one small cleanup pass: make GenericPipelineBehavior silent (or move under features/ as an exemplar), delete the v2 stub (fold the sentence into a how-to), delete ExampleConst, merge the aspire `#if web` blocks, sync the membership.targets comment, fix the AGENTS.md word.

**Follow-on:** `Sweep demo/scaffold residue (pipeline placeholder, v2 stub, dead constants, comment drift)`

---

## Theme summary

- **A. Platform-package hygiene** (F-001, F-002): what ships in TimeWarp.Foundation.* is the template's real API surface — secrets out, dead MVC out.
- **B. Generator engineering** (F-003, F-004, F-005, F-008, F-014): per-compilation state, one discovery rule, honest public API, fail-closed defaults, documented IDs.
- **C. Template-exemplar duplication** (F-006, F-015): the identity slice and API transport are the patterns every generated app copies — de-dup there first.
- **D. Gate/tooling duplication** (F-007, F-011, F-012): the release gates must share one harness and one SSOT-derived scan set.
- **E. Convention-by-comment residuals** (F-009, F-010, F-013, F-016, F-017): fossils and undocumented defaults — decide, document, or delete.

## Approval-bar argument (per /code-review skill)

The repo does **not** merit an unqualified approval: F-001 is a presumptive blocker, and F-002/F-005 are exactly the "unnecessary abstraction / misleading contract" classes the bar names. It equally does not merit wholesale restructuring: convention enforcement is green across the board (§Verification in review-brief.md), file sizes are disciplined (0 files ≥1k), and the worst duplication is concentrated in two fixable clusters (identity handlers, dev-cli smoke). Conditional approval with the blocker/major findings dispositioned.
