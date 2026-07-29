# Round 1 — grok-verification

**Date:** 2026-07-28
**Role:** Independent re-verification of Kimi K3 findings F-001…F-017 (task 131)
**Review commit:** `2b5dc765` (per review-brief.md; code-identical to HEAD at review time)
**Scope:** full repo at that commit; read-only on product code; write only this file
**Blind rule:** verdicts below written without reading `claude-verification.md`

## Summary

All 17 findings were re-checked against the live tree. **None are wholesale fabrications.**
Most major/blocker claims hold; a few need scope or fact corrections (dead-API details that
are SPA-only, bare-`Features` substrate already documented in Design regions, taxonomy
broader than TWE006 alone). Dominant themes confirmed: platform-package secret/debug noise
(F-001), dead MVC bridge + analyzer jurisdiction (F-002), generator static state and
triplicated discovery (F-003/F-004), no-op public `EndpointType` (F-005), identity and
smoke-harness duplication (F-006/F-007).

**Verdict counts:** confirmed 10 · confirmed-expanded 3 · confirmed-narrowed 4 · corrected 0 · rejected 0

---

## F-001 — Platform package echoes a secret to console

**Primary severity:** blocker
**Verdict:** `confirmed`

**Evidence:**
- `source/foundation/foundation-server/common-server-module.cs:126,130,154` — three
  `Console.WriteLine`s, including `Console.WriteLine($"connectionString: {connectionString}")`
  when an AppConfig connection string is present.
- `ConfigureAzureAppConfig` is invoked from `ConfigureConfiguration` (line 30), which
  web-server and api-server both call (`CommonServerModule.ConfigureConfiguration`).
- Foundation-tree sweep: those three lines are the **only** `Console.WriteLine` hits under
  `source/foundation/`.
- Package ships as `TimeWarp.Foundation.Server` — library code owns neither host logging nor
  a safe place to dump credentials.

**Disposition recommendation:** **accept-fix** (blocker). Delete all three WriteLines;
do not redact-and-keep. No follow-on needed beyond a one-file (or tiny sweep) fix.

---

## F-002 — Dead MVC bridge still ships in the platform package

**Primary severity:** major
**Verdict:** `confirmed-expanded`

**Evidence:**
- `base-endpoint.cs:19` — `BaseEndpoint<TRequest,TResponse> : ControllerBase` still present
  with Purpose/Design describing an MVC bridge; `// TODO: Review this code. Why not inject
  ISender?` at L14 contradicts its own Design region (service-locator intentional for
  ctor-free generated endpoints).
- Product inheritance: **zero** production `: BaseEndpoint` subclasses under `source/`
  (grep). Only live consumers of the type name are the coverage analyzer and **analyzer
  tests** (`tests/analyzers/.../endpoint-coverage-analyzer-tests.cs` still subclasses
  `BaseEndpoint` for TWA0005 fixtures).
- `endpoint-coverage-analyzer.cs:15,73,102` — Design still says “TWA0005 applies to MVC
  BaseEndpoint subclasses only”; Analyze still resolves `BaseEndpoint\`2` and builds
  `mvcEndpoints` for verb mismatch. In product builds that list is always empty.
- `base-fast-endpoint.cs:14` — same stale ISender TODO; Design still says “keep their
  semantics aligned” with the dead MVC twin. **BaseFastEndpoint itself is live** (generated
  endpoints) — delete only the MVC half and the dual-maintenance pact language.
- `common-server-module.cs:59-60` — still configures `Mvc.JsonOptions` alongside
  `ConfigureHttpJsonOptions`. No product MVC endpoints remain; HttpJson path is the live
  FE/minimal-API seam.

**Expansion beyond primary:** deletion must also migrate/update analyzer tests that still
exercise the MVC BaseEndpoint path for TWA0005, or TWA0005 becomes a pure no-op and its
tests need a new fixture strategy (or the diagnostic is retired for product trees).

**Disposition recommendation:** **accept** as follow-on task
(`Delete dead MVC BaseEndpoint bridge and its analyzer jurisdiction`). Verify no
non-API MVC consumer of `JsonOptions` before removing Mvc.JsonOptions wiring.

---

## F-003 — FastEndpoint generator keeps route-conflict state in a static dictionary

**Primary severity:** major
**Verdict:** `confirmed`

**Evidence:**
- `route-registry.cs:17` — `static readonly ConcurrentDictionary<...> RegisteredRoutes`.
- `Reset()` at L39–42 clears the static; Design region admits static is used because
  “incremental-generator source outputs run per symbol with no shared pipeline state.”
- `fast-endpoint-source-generator.cs:47` — `RouteRegistry.Reset()` at `Initialize`;
  L158 — `TryRegisterRoute` during generation.
- Compiler-server / concurrent project builds: any second compilation that calls
  `Initialize` mid-registration of another can clear or interleave entries; two servers
  that legitimately share a route string would false-conflict if both generators run in
  one process. Non-deterministic diagnostics are a real generator defect class.

**Disposition recommendation:** **accept** follow-on
(`Make FastEndpoint route-conflict detection per-compilation`) — collect per batch via
incremental `.Collect()`; delete static `Reset()`.

---

## F-004 — Hosted-route discovery rule is triplicated

**Primary severity:** major
**Verdict:** `confirmed-narrowed`

**Evidence:**
- `EndpointMetadata.FromSymbol` (`endpoint-metadata.cs:55–99`) — outer symbol + nested
  Query/Command + `[ApiRoute]`; auth markers; no `ClientOnlyContract` filter here
  (callers filter by `[ApiEndpoint]`).
- `ingress-route-prefix-generator.cs:248–291` `EnumerateHostedRoutes` — Design (L12–14)
  explicitly says participate rule matches FromSymbol “minus ClientOnlyContract”;
  implements its own walk + `HasClientOnlyContract`.
- `endpoint-coverage-analyzer.cs` — **related but not identical**: walks all types
  (including nested) for `[ApiRoute]` on the type itself, minus ClientOnly; does **not**
  require outer `[ApiEndpoint]`. That is correct for TWA0006 (routed contract ⇒ endpoint)
  but is a third walker, not a pure copy of FromSymbol.
- `GetAllNamespaces` is duplicated between `fast-endpoint-source-generator.cs:180+` and
  `ingress-route-prefix-generator.cs:296+`.

**Narrowing:** “same rule three times” overstates identity of the coverage analyzer path;
shared helpers still pay off for ApiRoute/verb resolution, ClientOnly exclusion, and
namespace walks. Exact “outer ApiEndpoint + nested route” is really two generators + a
sibling coverage walk.

**Disposition recommendation:** **accept** follow-on to extract shared discovery helpers
(symbol → routes, ClientOnly flag, shared GetAllNamespaces). Do not force TWA0006 onto a
strict `[ApiEndpoint]`-only filter without re-thinking coverage semantics.

---

## F-005 — `[ApiEndpoint(EndpointType = …)]` is a public API that silently no-ops

**Primary severity:** major
**Verdict:** `confirmed`

**Evidence:**
- `api-endpoint-attribute.cs:9,17` — Design: “EndpointType optionally overrides the
  generated endpoint's base class”; public `Type? EndpointType { get; set; }`.
- `endpoint-metadata.cs:127–139` — when the named arg is present, body is
  `metadata.CustomEndpointType = null;` unconditionally (no read of TypedConstant).
- `fast-endpoint-source-generator.cs:233` —
  `CustomEndpointType?.FullName ?? "BaseFastEndpoint"` always resolves to BaseFastEndpoint.
- No product usage of `EndpointType =` found under `source/` / `tests/` (only the nulling
  assignment). `System.Type` on the model is the wrong generation-time currency as claimed.

**Disposition recommendation:** **accept** — prefer **delete** the property + dead
extraction/emission path (judo) unless a known consumer is waiting; if implement, bind
`INamedTypeSymbol` display string, not `Type`.

---

## F-006 — Identity handler problem-factories and ceremony preamble duplication

**Primary severity:** major
**Verdict:** `confirmed`

**Evidence:**
- Private static `SharedProblemDetails` factories repeated across handlers:
  `Unauthenticated`, `MalformedPayload`, `ChallengeInvalid`,
  `CredentialAlreadyRegistered`, `Quarantined` in add-passkey, add-agent-key,
  complete-passkey-registration, complete-agent-key-registration,
  complete-passkey-authentication, complete-agent-token-issuance, get-credentials,
  revoke-credential (grep counts of `private static SharedProblemDetails` range 1–6 per
  handler).
- Design regions state intentional mirroring:
  - add-passkey L7: “Order mirrors CompletePasskeyRegistration.Handler exactly…”
  - add-agent-key L8: “Order mirrors CompleteAgentKeyRegistration.Handler exactly…”
- Ceremony ladders (decode → consume challenge → verify → handle-exists → create/attach)
  share structure with documented per-handler differences (principal source, session
  issuance). ~200-line scale claim is plausible from the factory clusters alone.

**Disposition recommendation:** **accept** follow-on
(`Collapse identity handler problem-factory and ceremony-preamble duplication`).
Extract pure problem factories first; ceremony helpers only where ladders truly match —
do not merge distinct handlers.

---

## F-007 — dev-cli smoke commands duplicate ~200 lines and bypass 126-006 SSOT derivation

**Primary severity:** major
**Verdict:** `confirmed`

**Evidence:**
- Identical hand-maintained `ForbiddenRewrittenPackageFragments` arrays:
  `template-smoke-command.cs:85–91` and `template-publish-smoke-command.cs:70–76`
  (`.Analyzers`, `.Generators`, `.Attributes`, `.TypedIds`).
- `SmokePinnedPackageIdFragments` (smoke L69–77) ≡ `PlatformPinIncludeFragments`
  (publish L60–68) — same six fragments, two names.
- `AssertPackageIdsNotRewritten` near-verbatim in both (smoke L710–768, publish L614–657);
  both inline bin/obj skips. Smoke alone also checks generated
  `timewarp-platform-packages.props` content (L743–757); publish does not share that
  helper cleanly. Smoke’s `IsBinObjOrArtifacts` (L640+) is not used by
  `AssertPackageIdsNotRewritten` (which reimplements bin/obj only — misses `artifacts` as
  claimed).
- InstallTemplate filter: smoke L270–272 three
  `.Contains(".Analyzers."|".Generators."|".Attributes.")` hand checks.
- Pre-generate namespace scan derives suffixes from props via `ComposedArchitectureSuffix`
  (`AssertNoUnsafePlatformNamespaceLiterals`, smoke only ~L502+). **Publish-smoke has no
  namespace-literal scan** (no `AssertNoUnsafe` / `SourceNameLiteral` symbols under
  publish-smoke).
- Post-generate rewrite checks still use hand `ForbiddenRewrittenPackageFragments` rather
  than the 126-006 props derivation used for the pre-scan.

**Disposition recommendation:** **accept** follow-on
(`Extract shared template-smoke harness and derive all rewrite-scan suffixes from props SSOT`).
Highest-stakes tooling; drift between smoke and publish is release-risk.

---

## F-008 — Unknown HTTP verbs silently become GET

**Primary severity:** minor
**Verdict:** `confirmed-expanded`

**Evidence:**
- `endpoint-metadata.cs:230–241` — `ConvertHttpVerbToMethodName` switches five verbs;
  `_ => "Get"`.
- Design at L13–14 documents the enum-member-name fix for the related “Value.ToString()
  yields 1 for Post” bug — fail-open default remains on the mapping step.
- **Expansion:** `HttpVerb` already includes `Head` and `Options`
  (`foundation-contracts/base/http-verb.cs:8–14`). Those are not speculative future
  members; if used on `[ApiRoute]` today they silently mount as GET.
- `ResolveHttpVerbName` (L199) also falls back to `"Get"` when enum resolution fails.

**Disposition recommendation:** **accept** (fold into generator hygiene task with
F-003/F-004, or small standalone). Fail with SG diagnostic / generator error on
unrecognized verb; do not default to Get.

---

## F-009 — Generated apps ship with mock authentication compiled in

**Primary severity:** minor
**Verdict:** `confirmed`

**Evidence:**
- `web-spa.csproj:47` — unconditional
  `<DefineConstants>$(DefineConstants);MOCK_AUTHENTICATION;</DefineConstants>` (comment
  above frames it as optional mock B2C, but the define is not commented out).
- `web-spa/program.cs:54–68` — `#if MOCK_AUTHENTICATION` → Mock providers; `#else` →
  `AddMsalAuthentication` / AzureAdB2C bind.
- AzureAd / AzureAdB2C residue: `web-server/appsettings.json`, SPA `wwwroot/appsettings.json`,
  both web integration-test appsettings (grep).
- Tracked task exists:
  `kanban/to-do/104-021-template-feature-flags-slice-placement-and-entra-path-non-default.md`
  — requirements include “Entra non-default.” Finding’s remedy (document mock-default as
  explicit choice; AzureAd residue rides 104-021) matches that ownership.

**Disposition recommendation:** **accept as tracked** by **104-021** (no new task).
Steward may still want a one-line disposition note that mock-default is intentional until
104-021 ships. Optional: strengthen the csproj comment to state “default for template
zero-setup; Entra is non-default by design.”

---

## F-010 — Template chrome fossils: B2C/PWA, stale TODOs, MediatR link

**Primary severity:** minor
**Verdict:** `confirmed-expanded`

**Evidence:**
- `App.razor:41–43` — `<!--#if B2C -->` MSAL script; B2C is **not** a `template.json`
  symbol (symbols are grpc/api/web/yarp/postgres…). Engine strips unknown conditionals.
- `App.razor:53–56` — `<!--#if PWA -->` around already-commented service-worker script;
  “Cramer review” TODO at L53.
- `App.razor:45–48` — Passwordless CDN ESM import + hard-coded
  `apiKey: "timewarp:public:…"` always ships (not behind a flag). **Expansion:** this is
  stronger residue than a dead B2C region — third-party script + tenant public key in
  every generated app’s chrome.
- `HomePage.razor:33` — `https://github.com/jbogard/MediatR` labeled “MediatR for CQRS”;
  stack is TimeWarp.Mediator.
- `web-server/program.cs:147` — typo’d TODO “seesm like could just pass whole config???”;
  L165–166 commented `AddRazorPages` / `AddServerSideBlazor`.

**Disposition recommendation:** **accept** follow-on cleanup. B2C/PWA posture with 104-021;
**separately** confirm 104-016/104-021 cover Passwordless CDN + public key removal (or add
that coverage during disposition). Fix MediatR link immediately in any small chrome pass.

---

## F-011 — template.json excludes postgres by per-file enumeration

**Primary severity:** minor
**Verdict:** `confirmed`

**Evidence:**
- `.template.config/template.json:76–86` — five discrete
  `source/container-apps/web/platform/postgres/*` files +
  `features/identity/ef-principal-store-infrastructure.cs` + tests glob.
- Peer flag excludes use folder globs: `source/container-apps/grpc/**`,
  `source/container-apps/api/**`, `source/container-apps/web/**`,
  `source/container-apps/yarp/**`.
- Live postgres platform tree has exactly those five files today; a sixth would slip into
  `--postgres false` apps until SmokeNoPostgres fails the build.

**Disposition recommendation:** **accept** small fix —
`source/container-apps/web/platform/postgres/**` plus keep the
ef-principal-store identity path as a separate exclude.

---

## F-012 — tests/Directory.Build.props re-detects UseAnalyzerPackages

**Primary severity:** minor
**Verdict:** `confirmed`

**Evidence:**
- Root `Directory.Build.props:18–19` already computes `UseAnalyzerPackages` from source
  tree existence; comments state detection lives at root **because tests need the
  switches**.
- `tests/Directory.Build.props:3–4` imports root DBP via `GetPathOfFileAbove`.
- L40–45 re-runs the same existence detection with a stale “Mirror source/…” comment —
  conditions are `'$(UseAnalyzerPackages)' == ''`, so after root import this PropertyGroup
  is a **dead no-op**.
- L50–59 ItemGroups **are** necessary: analyzer ProjectReference/PackageReference wiring
  from `source/Directory.Build.props` does not apply to the tests tree (different import
  chain). Duplication of ~20 lines of wiring is real; detection block is pure noise.

**Disposition recommendation:** **accept** — delete redundant detection PropertyGroup;
optionally extract shared analyzer-wiring props for source+tests (nice-to-have, not
required for correctness).

---

## F-013 — Grammar analyzer path-scoping dead/redundant branches

**Primary severity:** minor
**Verdict:** `confirmed`

**Evidence:**
- `feature-filename-grammar-analyzer.cs:178` — `collapsed = CollapseDotDot(normalized)`
  before the L191–198 branch. Ternary returns `collapsed` if it starts with
  `../features/`, else `CollapseDotDot(normalized)` — identical values.
- L215–216 — `/{family}/features/` and `/container-apps/{family}/features/` — the latter
  is a substring case of the former for normal repo-rooted paths (redundant arm).
- `feature-filename-grammar.json` functions: only `"handler"` and `"endpoint"` (single
  segment). `TryFindIncompleteMultiSegmentFunction` (L353–416) skips functions without
  `-`, so with today’s registry the loop body never runs useful work.
- Call site at L332 still invokes the multi-segment helper.

**Disposition recommendation:** **accept** cleanup (YAGNI on multi-segment until a
multi-segment function is registered). Low urgency.

---

## F-014 — Diagnostic ID taxonomy drift outside documented TWA surface

**Primary severity:** minor
**Verdict:** `confirmed-expanded`

**Evidence:**
- AGENTS.md enforcement table documents **TWA0001–TWA0019** only.
- Live non-TWA IDs in generators/analyzers package:
  - **TWE001–TWE004** — `diagnostic-descriptors.cs` (ApiEndpoint generation contract)
  - **TWE005** — `page-source-generator.cs`
  - **TWE006** — `typed-id-source-generator.cs` (Error: typed-id shape)
  - **SG001/SG002** — `fast-endpoint-source-generator.cs` (and SG001 also in ingress generator)
  - **SG010/SG011** — typed-id resilience warnings
- AnalyzerReleases.Unshipped.md lists TWE006 / SG010 / SG011; still absent from AGENTS.md.

**Expansion:** primary text highlighted TWE006 + SG001-002/010-011; the full TWE001–005
surface is also undocumented in the contributor table.

**Disposition recommendation:** **accept** docs follow-on — document TWE/SG families in
AGENTS.md (or rationalize TWE convention errors into TWA numbering in a later pass).
Docs-only is enough for now.

---

## F-015 — BaseApiService ↔ TestApiService transport mirror

**Primary severity:** minor
**Verdict:** `confirmed-narrowed`

**Evidence:**
- `base-api-service.cs` and `tests/.../test-api-service.cs` share verb dispatch, route prep
  (GET/DELETE query string), JSON body for POST/PUT/PATCH, 204 and 499 problem mapping,
  seam `JsonSerializerOptions`. TestApiService Design (L6–11) admits it “mirrors” SPA
  transport because tests cannot reference the SPA stack.
- **Narrowing — SPA-only secondary defects:**
  - Swallow-all catch + `// TODO: Log the error`: **BaseApiService L150–152 only**.
    TestApiService L125 filters `JsonException or InvalidOperationException` and has no
    log TODO.
  - Head/Options: BaseApiService `PrepareContent` returns null for Head/Options (L204–205)
    while dispatcher throws `NotImplementedException` (L186–187). TestApiService uses
    `NotSupportedException` for unknown verbs (L92) and does not special-case Head/Options
    in PrepareContent (default `_ => null`).
- Shared-core extraction remains valid maintainability advice; “both sides” bug list needs
  that split.

**Disposition recommendation:** **accept** follow-on for shared transport core when
touched; **fix SPA HandleProblemResponse / verb matrix** opportunistically even without
full extract. Not a release blocker.

---

## F-016 — Shared constants parked in bare `Features` namespace

**Primary severity:** note
**Verdict:** `confirmed-narrowed`

**Evidence:**
- `module-ids-contracts.cs` and `role-ids-contracts.cs` use
  `namespace TimeWarp.Architecture.Features;` (no slice Id).
- **Narrowing on “undocumented and unprincipled”:** both files’ Design regions already
  state the intent — “Lives in the Features substrate (not a product slice) so …
  product slices can reference well-known … ids without cross-slice coupling (TWA0009).”
  So this is a deliberate substrate choice at file level, not an accident.
- Gap that remains: AGENTS.md / placement skill do not describe “bare Features =
  cross-slice constants,” so the next author has no SSOT outside those two Design blocks.
- 104-021 notes already call out `authorization/` role-id constants as “not a slice,
  shared contract data” to rehome — related ownership exists.
- Note: many SPA base types also use bare `Features` under `web-spa/features/base/` —
  different tree (SPA convention); primary finding correctly targets the two contracts
  files under the cohesive product `web/features/` tree.

**Disposition recommendation:** **accept as decision note** — either document bare
`Features` substrate in placement skill/AGENTS.md, or rehome under an explicit shared
home as part of 104-021 / placement work. No urgent code change required for correctness.

---

## F-017 — Demo/scaffold residue cluster

**Primary severity:** note
**Verdict:** `confirmed`

**Evidence (item-by-item):**
| Claim | Check |
|-------|--------|
| api-server `GenericPipelineBehavior` Console.WriteLine on every request | **Yes** — `generic-pipeline-behavior.cs:17,19`; registered in `api-server/program.cs:85` before FluentValidationBehavior. Only non-bootstrap logic file in api-server artifact folder. |
| `web/features/v2/overview.md` empty stub | **Yes** — two-line “add the contracts under this folder”. |
| `tests/.../constants.cs` ExampleConst | **Yes** — `ExampleConst = "Example"` + commented leftover; no other references. |
| aspire `#if web` comment-only block | **Yes** — `program.cs:134–146` is comments only inside `#if web`, adjacent to real `#if web` at L147–152. |
| api/grpc membership.targets “both trees may be absent” vs web | **Yes** — api L15–17 and grpc L15–17 include that paragraph; web membership.targets ends the comment block without it. |
| AGENTS.md “api platform/ empty (no content yet)” | **Yes** — AGENTS.md layout line; `source/container-apps/api/platform` **does not exist** (directory absent, not empty). |

**Disposition recommendation:** **accept** as one small sweep follow-on (or batch with
other chrome cleanup). Silence or relocate GenericPipelineBehavior first if any item is
prioritized (console noise in generated api-server).

---

## Meta

- **Zero rejections:** primary reviewer claims were falsifiable and held under tree
  re-check; expansions/narrowings are precision, not deference.
- **Suggested steward priority order for disposition:**
  1. F-001 (blocker fix now)
  2. F-002, F-005, F-003, F-007, F-004, F-006 (majors → child tasks / packages)
  3. F-008, F-010 (+ Passwordless key), F-011, F-012, F-014 (cheap or docs)
  4. F-009, F-016 (tracked / decide-document)
  5. F-013, F-015, F-017 (batch cleanup)
- **Does not write `disposition.md`** — steward-owned per framework.

---

## Delta vs claude-verification

Read `claude-verification.md` only after the 17 verdicts above were written.
Claude’s summary: confirmed ×7, confirmed-expanded ×8, confirmed-narrowed ×1,
corrected ×1 (F-009); zero rejections. Mine: confirmed ×10, confirmed-expanded ×3,
confirmed-narrowed ×4, corrected ×0, rejected ×0. Both reject nothing; differences
are taxonomy and depth of expansion.

### Material disagreement (I was wrong)

| ID | Grok (blind) | Claude | Resolution after re-check |
|----|--------------|--------|---------------------------|
| **F-009** | `confirmed` — claimed `MOCK_AUTHENTICATION` is **unconditional** | `corrected` — define is inside `Condition="'$(Configuration)' == 'Debug'"` (`web-spa.csproj:37–50`); Release compiles MSAL/`AzureAdB2C` | **Claude is correct.** Line 47 sits in the Debug PropertyGroup; Kimi’s “always defined” and my endorsement of it are false. Release/smoke/`dotnet publish` run the MSAL branch against placeholder config — a Debug↔Release auth flip, worse than “dead weight.” Steward should treat F-009 as **`corrected`**, not my blind `confirmed`. Interim fix Claude proposes (define all configs until 104-021) is sound. |

### Strong agreement (same core)

| ID | Notes |
|----|--------|
| F-002 | Both accept dead MVC bridge; both require analyzer-test cleanup. Claude’s **remedy correction** is sharper: TWA0005 is MVC-only by Design — deleting BaseEndpoint **retires TWA0005** entirely; there is no “keep BaseFastEndpoint path for TWA0005.” My expansion noted test migration; Claude adds AGENTS retirement + no `AddControllers`/`MapControllers` anywhere (unusable, not merely unused). Prefer Claude’s remedy text. |
| F-003 | Both confirm static registry defect. Claude **expands** to IDE incremental self-conflict (endpoints stop generating) — I did not surface that; accept as additional evidence for same fix. |
| F-004 | Both confirm multi-walker drift. Claude finds **live** `[ApiEndpoint]+[ClientOnlyContract]` contradiction and package-boundary sharing (linked source, not cross-package ref). My `confirmed-narrowed` (rules not identical) and Claude’s “different rules are worse” are compatible; prefer Claude’s existing-drift evidence. |
| F-005 | Both confirm silent no-op; Claude finds **docs teach** `EndpointType` (`ApiEndpointSourceGenerator.md:174`) — good expansion; delete still preferred. |
| F-006 | Both confirm; Claude’s factory census slightly higher and security-ordering argument is stronger than pure LOC. |
| F-007 | Both confirm hand-list / dual harness drift; Claude’s ~350 LOC and fourth hand-suffix site are compatible expansions. |
| F-008 | Both confirm fail-open GET; both note dual fallbacks / Head-Options. Taxonomy: I used `confirmed-expanded` (Head/Options already in enum); Claude `confirmed` — same fix. |
| F-011–F-013 | Full agreement on facts and remedies. |
| F-016 | Both `confirmed-narrowed` — Design regions document substrate; AGENTS/placement skill do not. |
| F-017 | Both confirm all six items; Claude’s ILogger-exemplar refinement for GenericPipelineBehavior is better than silence-only. |

### Taxonomy / depth deltas (not factual conflict)

| ID | Grok | Claude | Steward takeaway |
|----|------|--------|------------------|
| **F-001** | `confirmed` — delete WriteLines | `confirmed-expanded` — delete whole `ConfigureAzureAppConfig` + Azure package refs | Claude’s expansion is well-argued (host bootstrap litmus, sole consumer of Azure.Identity / AppConfiguration packages, hard-coded Sentinel/refresh). My verdict under-scoped the **remedy**, not the secret-echo fact. Steward should consider expanded delete under platform hygiene. |
| **F-010** | `confirmed-expanded` (Passwordless CDN + public key) | Same expansion + passwordless-service Console.WriteLine of ApiKey; B2C script missing under F-009 Release path | Aligned; Claude adds SPA Console.WriteLine of secrets (F-001 class) and F-009 cross-link. |
| **F-014** | `confirmed-expanded` (TWE001–006 + SG) | Same + dead TWE001/002/004 never reported; dual SG001; false “authoritative registry” claim | Prefer Claude’s census for disposition. |
| **F-015** | `confirmed-narrowed` (SPA-only secondary bugs) | `confirmed-expanded` (catch already forked = mirror drift proven) | Same facts: test side has the better catch. Narrow vs expand labels the same fork differently. |

### Items Claude found that my pass under-emphasized

1. F-009 Debug/Release auth flip (critical — see above).
2. F-001 delete App Config module + Azure deps, not just WriteLines.
3. F-003 IDE phantom self-conflicts.
4. F-004 live ClientOnly × ApiEndpoint drift.
5. F-005 published how-to advertising dead API.
6. F-014 unused TWE001/002/004 and dual SG001.
7. F-010 passwordless-service `Console.WriteLine` of ApiKey.

### Net for steward disposition

- **No finding should be rejected** (both reviewers).
- **Must re-label F-009** using Claude’s `corrected` mechanics before disposition.
- Prefer **Claude’s expanded remedies** for F-001, F-002 (TWA0005 retirement), F-003, F-004 when writing child tasks.
- Theme grouping both reviewers propose is compatible: platform hygiene · generator hardening · identity/transport de-dup · smoke SSOT · chrome/residue (+ F-009 interim).
