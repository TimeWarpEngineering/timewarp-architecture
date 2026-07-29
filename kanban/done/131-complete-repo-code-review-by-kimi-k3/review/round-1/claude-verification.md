# Round 1 — claude-verification (review of the review)

**Date:** 2026-07-28
**Reviewer:** Claude Fable 5 (independent verification of Kimi K3's F-001…F-017)
**Scope reviewed:** each finding re-verified against the tree at `dev` HEAD (code-identical
to review commit `2b5dc765`); read-only on product code.

Verdicts: `confirmed` | `confirmed-expanded` | `confirmed-narrowed` | `corrected`
(core concern stands, stated mechanics wrong) | `rejected`.
Findings are verified one at a time with the steward in session; entries below fill in
as each is completed. Steward decisions land in `../../disposition.md`, not here.

## Summary

_(written after all 17 verifications complete)_

---

## F-001 — blocker — AppConfig connection string echoed to console

**Verdict: confirmed-expanded**

**Kimi's claim checks out.** All three `Console.WriteLine`s exist as cited
(`source/foundation/foundation-server/common-server-module.cs:126,130,154`); :130 prints the
Azure App Config connection string — a credential — to stdout, and the file ships in the
`TimeWarp.Foundation.Server` package. Severity `blocker` is fair for a platform package:
consumers' log aggregation would capture the secret. Foundation-tree sweep confirms these
are the only three `Console.WriteLine`s in `source/foundation/`.

**But the remedy is under-scoped.** The WriteLines are symptoms; the host method
`ConfigureAzureAppConfig` is the disease:

1. **Sole consumer of two heavy Azure dependencies.** `Azure.Identity` and
   `Microsoft.Azure.AppConfiguration.AspNetCore` are referenced by `foundation-server`
   *only* for this method — nothing else in `source/` or `tests/` uses either. Every
   generated app restores Azure.Identity (large, frequently CVE-flagged dependency chain)
   whether or not it ever touches Azure.
2. **Opinions a library cannot defend:** hard-coded `"Sentinel"` refresh key, 5-minute
   refresh interval, `UseFeatureFlags()` unconditionally, Key Vault via
   `DefaultAzureCredential`, and `optional: false` — a present-but-unreachable connection
   string crashes startup.
3. **Placement litmus (repo's own rule):** configuration-source composition is host
   bootstrap, not shared platform plumbing. An app wanting App Config adds ~3 lines in its
   own `program.cs`.
4. **Hand-test heritage:** the `TestValue` probe (:153-154) exists only to print a config
   value; `ConfigureConfiguration` carries a stray double semicolon (:30). Same legacy-Azure
   residue class as F-009's dead MSAL path.

Ownership note: this is **not** covered by task 104-021 — that owns Entra *auth* posture;
App Config is a config-source concern, decidable under 131.

**Recommendation:** accept, expanded — delete `ConfigureAzureAppConfig` entirely
(method, both Azure package references, the `; ;`), leaving `ConfigureConfiguration` an
empty (or removed) hook, rather than only deleting the WriteLines. The secret-echo blocker
falls out for free and the foundation package sheds its only Azure dependency. If a
documented "add Azure App Config back" recipe is wanted, it is a how-to doc, not library code.


---

## F-002 — major — Dead MVC BaseEndpoint bridge in platform package

**Verdict: confirmed (with one remedy correction)**

**All four factual claims verified:**

1. **`BaseEndpoint<TRequest,TResponse>` is dead.** Repo-wide grep: the only references are
   the class itself, the coverage analyzer's `GetTypeByMetadataName("…BaseEndpoint`2")`
   lookup, comments, and analyzer tests that declare their *own* stub `BaseEndpoint` (they
   don't reference foundation's). Stronger than Kimi stated: **no `AddControllers`/
   `MapControllers` exists anywhere in `source/`** — MVC is never wired into any host, so a
   `ControllerBase`-derived endpoint could not be routed even if someone subclassed it. The
   bridge isn't just unused; it's unusable in every host the template generates.
2. **`Mvc.JsonOptions` configuration is a no-op** (`common-server-module.cs:59-60`): with no
   MVC services registered, `Configure<Microsoft.AspNetCore.Mvc.JsonOptions>` has no
   consumer. FastEndpoints and the OpenAPI document use their own serializer paths;
   `ConfigureHttpJsonOptions` covers minimal-API `WriteAsJsonAsync` (which
   `BaseFastEndpoint.HandleAsync` uses — that one stays).
3. **Stale TODOs confirmed** in both bridge files (`// TODO: Review this code. Why not
   inject ISender?`) — each file's own Design region already answers it (ctor-free so
   source-generated subclasses need no plumbing).
4. **Dual-maintenance pact confirmed**: both Design regions instruct "keep their semantics
   aligned" across a live half and a dead half.

**Remedy correction:** Kimi's parenthetical "(TWA0005 keeps the BaseFastEndpoint-generated
path)" is wrong. The analyzer's own Design region states TWA0005 (verb mismatch) applies to
**MVC subclasses only** — FastEndpoints are generated *from* the contract's verb, so drift
is impossible and there is no FE path for TWA0005 to keep (FE verbs are set in `Configure()`
code, not `[HttpX]` attributes, so the check couldn't even be ported). Deleting the MVC
branch therefore **retires TWA0005 entirely**; TWA0006 (missing-endpoint coverage) is what
survives, with its server-project scope gate keying on `BaseFastEndpoint` alone.

**Additional cleanup the finding missed:**
- `endpoint-coverage-analyzer-tests.cs` — the MVC/TWA0005 test scenarios (stub
  `BaseEndpoint`, `GetWidgetEndpoint : BaseEndpoint<…>` cases) must be removed/rewritten.
- AGENTS.md TWA table — mark TWA0005 retired (reserve the ID; never reuse).
- Design-region reconciliation: `base-fast-endpoint.cs` "MVC counterpart… keep aligned"
  line; `common-server-module.cs` Design mention of MVC JsonOptions;
  `web-server/program.cs:15` comment mentioning "no MVC BaseEndpoint".

**Breaking-change note:** removing a public type from `TimeWarp.Foundation.Server` is
breaking for package consumers — but no generated app uses it, packages are pre-adoption,
and the steward's stated goal is a solid repo *before* utilization. Now is the cheapest
moment this deletion will ever have.

**Recommendation:** accept, with the corrected remedy: delete `base-endpoint.cs`,
retire TWA0005 (keep TWA0006 keyed on `BaseFastEndpoint`), delete both TODOs, delete the
`Mvc.JsonOptions` block, update analyzer tests, AGENTS.md table, and the three Design/
comment reconciliations.


---

## F-003 — major — Static cross-compilation RouteRegistry in FastEndpoint generator

**Verdict: confirmed-expanded** (defect is worse than reported)

**Kimi's claims verified:** `RouteRegistry` (`validation/route-registry.cs:17`) is a static
`ConcurrentDictionary` in the generator assembly — one instance per compiler-server process,
shared by every compilation that runs the generator. `Reset()` is called in `Initialize`
(`fast-endpoint-source-generator.cs:47`). Concurrent project builds (web-server + api-server
in one solution build) interleave: one compilation's `Reset()` can clear another's
mid-registration, and cross-project routes can false-conflict. Confirmed as written.

**Expansion — the IDE incremental case is broken outright, not just non-deterministic:**
for `IIncrementalGenerator`, `Initialize` runs once per generator-driver creation, but
`RegisterSourceOutput` callbacks re-run on every pipeline recompute. The pipeline combines
`CompilationProvider` (changes on every keystroke) through `SelectMany`, so in a long-lived
IDE session every recompute re-registers the same routes into the never-again-cleared
static: each endpoint finds *itself* in the dictionary, reports a phantom TWE003
route-conflict, and — per the "first registration wins, conflicting endpoint not
generated" policy — **silently stops being generated** until the compiler process restarts.
Command-line builds mask this (fresh driver per csc invocation → Reset per compilation);
any IDE user editing a server project hits vanishing endpoints.

**Secondary observations:**
- Piping `Compilation`/`INamedTypeSymbol` through the incremental pipeline defeats caching
  (symbols are not equatable across compilations) — full regeneration per keystroke. The
  fix Kimi proposes (extract equatable metadata, `.Collect()`, detect duplicates in-batch)
  cures this too.
- "First registration wins" makes *which* endpoint survives a conflict order-dependent;
  per-compilation batch detection should report all parties deterministically.
- The registry's own Design region argues the static is needed because "source outputs run
  per symbol with no shared pipeline state" — false: `.Collect()` is exactly the shared
  per-compilation state mechanism; the Design region rationalizes the defect.
- TWE003 feeds the F-014 taxonomy finding (undocumented ID prefix).

**Recommendation:** accept — Kimi's remedy is exactly right (delete the static
registry; collect equatable `(route, verb, className)` metadata via `.Collect()`; report
duplicates per compilation; `Reset()` disappears). Fold F-008 (unknown-verb → GET) into the
same generator-hardening task; both live in the same extraction/emission path.


---

## F-004 — major — Hosted-route discovery rule triplicated

**Verdict: confirmed** (with two refinements: the drift is already real, and the remedy
needs a different sharing mechanism)

**Triplication confirmed**, but the three walkers are not identical copies — they are
three overlapping-but-different rules, which is worse:

1. **FastEndpoint generator** (`fast-endpoint-source-generator.cs:120-125` +
   `EndpointMetadata.FromSymbol`): outer `[ApiEndpoint]` (symbol-equality match) → nested
   `Query`/`Command` → `[ApiRoute]` (simple-name match). **No `[ClientOnlyContract]` check
   at all.**
2. **Ingress generator** (`EnumerateHostedRoutes`, :248-291): outer `[ApiEndpoint]` →
   nested `Query`/`Command` → minus `[ClientOnlyContract]` checked on **both** the outer
   type and the nested request ("either placement means not hosted").
3. **Coverage analyzer** (TWA0006): any type carrying `[ApiRoute]` directly (no
   `[ApiEndpoint]` requirement — intentional: its job is catching the forgot-`[ApiEndpoint]`
   case) → minus `[ClientOnlyContract]` on **the `[ApiRoute]` carrier only**.

**Existing drift instance (new evidence for Kimi's thesis):** a contract carrying both
`[ApiEndpoint]` and `[ClientOnlyContract]` today gets a **generated endpoint** (walker 1
doesn't check client-only), **no ingress carve-out** (walker 2 excludes it), and **no
TWA0006** (walker 3 excludes it) — a hosted endpoint the ingress can't reach and no
diagnostic flags the contradiction. The `[ClientOnlyContract]` placement asymmetry
(either-position vs carrier-only) is a second live disagreement. These are exactly the
silent desyncs the finding predicts; they already exist.

**Remedy refinement — sharing mechanism matters:** the coverage analyzer ships in
`TimeWarp.Architecture.Analyzers` while both generators ship in
`TimeWarp.Architecture.Generators` — separate packages loaded independently into the
compiler process. A cross-package assembly dependency between analyzer DLLs is fragile
(loader conflicts); the shared `HostedRouteDiscovery` should be a **linked shared-source
file** compiled into both projects (same pattern as the grammar-registry SSOT), not a
package reference. It must be flag-parameterized because the rule differences are partly
intentional (walker 3's broader net is by design).

**Also confirmed:** `GetAllNamespaces` is verbatim-duplicated in both generators; the
ingress Design region states the cross-walker invariant as prose ("participate rule matches
EndpointMetadata.FromSymbol … minus ClientOnlyContract") — convention-by-comment, and the
prose is *already inaccurate* regarding the client-only placement difference.

**Recommendation:** accept, with the refinements: shared discovery helper as linked shared
source with explicit flags; add a TWA diagnostic (or fold into TWA0014's contradiction
family) for `[ApiEndpoint]` + `[ClientOnlyContract]` on one contract; delete duplicate
`GetAllNamespaces`. Natural home: same generator-hardening task as F-003/F-008.

---

## F-005 — major — `[ApiEndpoint(EndpointType = …)]` silently no-ops

**Verdict: confirmed-expanded** (the docs actively advertise the broken API)

**Kimi's mechanics verified exactly:** `ApiEndpointAttribute.EndpointType` is a public
`Type?` property whose Design region documents it as a base-class override
(`api-endpoint-attribute.cs:9,17`). The extraction (`endpoint-metadata.cs:131-140`) finds
the named argument and then executes `metadata.CustomEndpointType = null` — the property is
already null, so the code is a literal no-op that *runs only when the user set the value*.
Emission (`fast-endpoint-source-generator.cs:233`) therefore always falls through to
`BaseFastEndpoint`. Kimi's currency point also holds: `System.Type` cannot be materialized
from a `TypedConstant` at generation time; a correct implementation would carry the
`INamedTypeSymbol` display string.

**Expansion:** no contract in the repo sets `EndpointType` (repo-wide grep: zero setters) —
but `documentation/developer/reference/ApiEndpointSourceGenerator.md:174` **teaches it** as
Customization step 1 (`[ApiEndpoint(EndpointType = typeof(MinimalApiEndpoint<,>))]`,
referencing a `MinimalApiEndpoint` type that doesn't exist in the repo). So the shipped
story is: public API + published how-to → silent nothing. Worse than Kimi stated.

**On implement-vs-delete:** delete. (a) Zero consumers in-repo, none waiting; (b) a real
implementation is not one line — a custom base must match the `<TRequest, TResponse>`
generic shape and interact coherently with auth emission, empty-request binders, and
serialization, none of which is specified; (c) YAGNI + this repo's no-speculative-machinery
posture (same reasoning as F-013's multi-segment branch). Scope: the attribute property,
its Design-region line, `EndpointMetadata.CustomEndpointType` + extraction block, the
`?.FullName ??` fallback at emission, and the doc's Customization step 1. Breaking-change
calculus identical to F-002 (public Attributes package, pre-adoption — cheapest now).

**Recommendation:** accept — delete the property and all dead plumbing; fix the reference
doc. Fold into the generator-hardening cluster (F-003/F-004/F-008) since the same files are
touched.

---

## F-006 — major — Identity handler problem-factory / ceremony duplication

**Verdict: confirmed** (counts slightly undercounted; the duplication is worse)

**Factory census (repo grep, exact):** `MalformedPayload` ×6, `ChallengeInvalid` ×6 (Kimi
said ×4), `Unauthenticated` ×4, `CredentialAlreadyRegistered` ×4 (Kimi said ×3),
`VerificationFailed` ×4 (2 WebAuthn-typed + 2 AgentKey-typed), `Quarantined` ×2,
`InvalidPublicKey` ×2 — across 10 of the 14 identity operation handlers. Spot-diff of
`add-passkey` vs `complete-passkey-registration`: their four shared factories are
**byte-identical**.

**Ceremony mirror confirmed:** the ladder RP-select → decode triple → challenge consume →
verify → duplicate-handle check is an exact ~35-line copy between the two passkey
registration handlers, with the differences exactly as their Design regions describe
(auth-guard-first + attach vs mint + session issuance). The `add-passkey` Design region
opens with "Order mirrors CompletePasskeyRegistration.Handler exactly" — consistency by
comment, as charged.

**A point Kimi under-sold — this is a safety issue, not just line count:** the ladder
ordering is *replay-safety-critical* by the handlers' own documentation (challenge burned
before verify; host checked before challenge burn; auth before everything). That invariant
currently exists as N parallel copies whose agreement is maintained by prose. Extracting
the ceremony preamble into one helper doesn't merely delete ~200 lines — it moves a
security-critical ordering from convention-by-comment (the drift class this repo keeps
paying for) into a single enforced code path. That is the same argument that justified the
FastEndpoint generator itself.

**Remedy refinements:**
- Shared `IdentityProblems` factories must parameterize the genuinely-varying wording
  (e.g. `ChallengeInvalid` registration vs authentication detail text) rather than flatten
  it — the variants are intentional, per-ceremony copy.
- Ceremony-preamble helper per family (passkey-registration, agent-key, passkey-auth),
  returning `OneOf<VerifiedResult, SharedProblemDetails>`; handlers keep their own
  auth-guard placement and post-verify actions. Agree with Kimi: do **not** merge handlers.
- Design regions must move with the code: ordering rationale relocates to the helper's
  Design region; handler regions slim to their genuine differences (agent-context-regions
  maintenance rule).
- All extraction stays inside the identity slice (application layer) — no TWA0009 surface.

**Recommendation:** accept, with the refinements above.

---

## F-008 — minor — Unknown HTTP verbs silently become GET

**Verdict: confirmed** (verified during F-003/F-005 reads; two fail-open sites, not one)

`ConvertHttpVerbToMethodName` (`endpoint-metadata.cs:230-241`) identity-switches five verbs
and defaults `_ => "Get"` — exactly the fail-open class the enum-member-name fix documented
ten lines above it was built to kill. **Additionally** `ResolveHttpVerbName` (:199) has its
own `?? "Get"` fallback, so there are two stacked fail-open defaults on the same path. If
`HttpVerb` grows (Head/Options — which `PrepareContent` in F-015 already anticipates
client-side), endpoints silently mount as GET: a write operation could become a
cache-safe, CSRF-exempt GET — a security posture change, not just a 405.

Note: the switch is also nearly pointless — four of five arms are identity mappings; its
only real function is the fail-open default. Deleting it in favor of a validate-or-diagnose
step is strictly simpler.

**Recommendation:** accept — replace both fallbacks with an SG diagnostic (or throw into
the generator's catch) on unrecognized verbs; fold into the generator-hardening cluster
(F-003/F-004/F-005).

---

## F-009 — minor — Mock authentication compiled in; MSAL path dead

**Verdict: corrected** — the core concern (unexplained dual auth stack) stands, but the
stated mechanics are wrong, and the true behavior is worse than the finding describes.

**Kimi's claim "MOCK_AUTHENTICATION is always defined" is FALSE.** The define sits inside
`<PropertyGroup Condition="'$(Configuration)' == 'Debug'">` (`web-spa.csproj:35,47`). It is
**Debug-only**. Consequently "the MSAL/AzureAd branch is dead unless manually revived" is
also false — the `#else` branch is **live in every Release build**:

- `Microsoft.Authentication.WebAssembly.Msal` is unconditionally referenced (:63) and
  `AccountClaimsPrincipalFactoryWithRoles` exists, so Release compiles the
  `AddMsalAuthentication` + `AzureAdB2C` path.
- `dev template-smoke` builds generated apps with `Release` configuration
  (`template-smoke-command.cs:222,250,376`) — so the smoke-validated build of every
  generated app is the **MSAL** one, while every developer's inner loop (Debug) runs mock
  auth.
- Net effect: **a generated app's authentication stack silently flips between Debug and
  Release.** `dotnet publish` (Release) ships MSAL redirect auth bound to placeholder
  `AzureAdB2C` appsettings the developer never configured — an app that "worked" all
  through development breaks (or half-authenticates against a nonexistent tenant) on first
  publish. That is a sharper trap than the "dead weight" Kimi described.
- The adjacent csproj comment is a fossil contradicting the code: "Uncomment the following
  line if you want to Mock B2C" — above a line that is permanently uncommented.

**Tracked-by check:** task 104-021 exists
(`kanban/to-do/104-021-template-feature-flags-slice-placement-and-entra-path-non-default.md`)
and legitimately owns the Entra/auth posture decision. Agree with routing the *posture*
there — but the Debug/Release auth-flip is a today-defect worth an interim fix under 131's
disposition: make `MOCK_AUTHENTICATION` unconditional (all configurations) so Debug and
Release agree, until 104-021 replaces the posture wholesale. One-line change, removes the
publish trap, changes nothing about the eventual 104-021 decision.

**Recommendation:** re-characterize per above; record the corrected mechanics on 104-021;
accept the one-line interim fix (define in all configurations) + delete the contradictory
comment. AzureAd appsettings residue rides with 104-021 as Kimi proposed.

---

## F-007 — major — dev-cli smoke commands duplicate ~200 lines, bypass 126-006 SSOT

**Verdict: confirmed-expanded** (verified via delegated file comparison; every numbered
claim TRUE, several understated)

1. `ForbiddenRewrittenPackageFragments` **byte-identical** in both files (S:85-91, P:70-76;
   four fragments incl. `.TypedIds`). ✔
2. The props-derived suffix mechanism (126-006 regex over
   `msbuild/timewarp-platform-packages.props`) is used **only** by template-smoke's
   pre-generate scan (S:502-553, wired S:176). The post-generate gates in both files use
   the hand list — and the InstallTemplate nupkg filter (S:270-272) spells the same
   suffixes by hand a **fourth** time. A new platform suffix property is auto-covered by
   exactly one of four checks. ✔ (worse than stated)
3. `AssertPackageIdsNotRewritten` verbatim-duplicated (S:714-741 ≡ P:618-644). Correction
   to Kimi: the inline bin/obj skip is in **both** copies, and both miss not only
   `artifacts` but also case-insensitivity (the unused-there `IsBinObjOrArtifacts` helper
   S:640-648 handles both). ✔
4. `SmokePinnedPackageIdFragments` ≡ `PlatformPinIncludeFragments` — same six entries, same
   order, two names (S:69-77, P:60-68). ✔
5. `SmokeOneAsync` skeleton duplicated (S:310-388 / P:397-486); the find-solution → restore
   → build tail is verbatim apart from two env-var calls. ✔
6. Publish-smoke has **no namespace-literal scan and no `.cs`-inclusive rewrite check at
   all** — its only rewrite check is MSBuild/JSON-scoped. The publish gate genuinely can
   pass what the smoke gate would fail. ✔

**Beyond Kimi's list:** `FindRepoRoot` verbatim-duplicated; SmokeMatrix + driver loop
duplicated; the `PackageVersion` regex + platform-fragment predicate duplicated between
`RewriteCpmPinsToSmokeVersion` (S) and `TryEvaluatePlatformPins` (P); NuGet.config writers
share a skeleton. The real duplication is closer to ~350 lines than ~200.

**Recommendation:** accept — one shared smoke-harness file (assert helpers + 126-006
suffix derivation + generate/restore/build skeleton) consumed by both commands; all four
suffix-list sites derive from the props SSOT; port the namespace-literal scan to
publish-smoke; use `IsBinObjOrArtifacts` everywhere. Fold F-012's MSBuild wiring dedup
here only if convenient — they're separate files, same theme.

---

## F-010 — minor — Template chrome fossils

**Verdict: confirmed-expanded** (all cited fossils real; the same block hides a bigger one
Kimi walked past)

**Cited items verified:** `<!--#if B2C -->` (App.razor:41) and `<!--#if PWA -->` (:54)
reference symbols absent from template.json (grep: zero hits) — the engine strips both
regions from generated output; two "Cramer review" TODOs; HomePage.razor:33 links
`jbogard/MediatR` (wrong project — repo uses TimeWarp.Mediator); web-server program.cs
carries the "seesm" TODO (:147) and commented-out `AddRazorPages`/`AddServerSideBlazor`
(:165-166). All confirmed.

**Cross-link to F-009 (corrected):** because the B2C region is stripped, generated apps
never receive the MSAL `AuthenticationService.js` script — so the Release-config MSAL
branch (live per my F-009 correction) is *doubly* broken in generated output: wrong config
AND missing script.

**Adjacent discovery — Passwordless.dev residue cluster (same App.razor block):**
- App.razor:46-49 loads the Bitwarden Passwordless client **from a third-party CDN** with a
  **hardcoded TimeWarp tenant public API key**
  (`timewarp:public:b00cd…`), shipped into every generated app — which will phone
  TimeWarp's passwordless.dev tenant. The key also ships in web-server and web-spa
  appsettings.
- `passwordless-service.cs:25-26` `Console.WriteLine`s the options (ApiUrl, ApiKey) —
  the F-001 defect class, SPA edition.
- Retirement is **tracked** (104-016 + 104-021; the `get-sign-in-token` contract's Design
  region documents the legacy status, and its `[ClientOnlyContract]` reasoning is sound) —
  so per the dedup rule this stays a tracked note, not a new major. But the disposition
  should verify 104-016/104-021 explicitly cover: removing the CDN script + tenant key
  from template output, and the appsettings/global-usings/package-ref sweep. If they
  don't, extend them.

**Recommendation:** accept the fossil deletions as written (B2C/PWA blocks ride with
104-021 as Kimi proposed); fix the MediatR link to TimeWarp.Mediator; add the
tenant-key/CDN coverage check to the 104-016/104-021 specs during disposition.

---

## F-011 — minor — template.json postgres excludes enumerate files instead of glob

**Verdict: confirmed** (exactly as written)

`.template.config/template.json` `(!postgres)` block enumerates the five
`platform/postgres/*.cs` files individually; the folder contains exactly those five today,
so a sixth file silently ships into `--postgres false` apps until SmokeNoPostgres catches
the build break (and only if it breaks the build — a self-contained file wouldn't).
Every other flag excludes by folder glob. The `ef-principal-store-infrastructure.cs` entry
must stay (it lives in `features/identity/`), as must the `web-infrastructure-tests/**`
glob.

**Recommendation:** accept as written — replace the five file entries with
`source/container-apps/web/platform/postgres/**`. One-line change; batch with F-017's
sweep.

---

## F-012 — minor — tests/Directory.Build.props redundant analyzer detection

**Verdict: confirmed** (plus one duplication Kimi didn't list)

Root `Directory.Build.props:18-19` computes `UseAnalyzerPackages` unconditionally, and its
own comment states detection lives at root precisely "because tests/ projects need the
switches too." `tests/Directory.Build.props:42-45` re-runs identical existence detection —
provably dead (the `== ''` guards can never be true after root runs) — under a "Mirror
source/Directory.Build.props" comment that is stale twice over (detection is not in
source/DBP, and no mirroring is needed). The wiring ItemGroups (tests :50-59 ≡ source
:36-47) and the TWA0010 `AdditionalFiles` block (tests :61-64 ≡ source :50-54 — Kimi
missed this one) are near-verbatim duplicates.

**Recommendation:** accept — delete the dead detection block, and extract the shared
wiring + AdditionalFiles into one `msbuild/*.props` imported by both source/ and tests/
props (per the no-dual-maintenance standard; "keep the duplication" is a churn argument).
Theme home: gate/tooling dedup with F-007.

---

## F-013 — minor — Grammar analyzer dead/redundant branches

**Verdict: confirmed** (all three claims verified against the code)

1. `collapsed` is assigned `CollapseDotDot(normalized)` at
   `feature-filename-grammar-analyzer.cs:178`; the ternary at :195-197 returns `collapsed`
   in one arm and `CollapseDotDot(normalized)` in the other — provably identical. ✔
2. :215-216 — any path containing `/container-apps/{family}/features/` contains
   `/{family}/features/` as a substring; the second `Contains` is dead. ✔
3. `TryFindIncompleteMultiSegmentFunction` (:353-416, ~65 lines) opens with
   `if (!function.Contains('-')) continue;` and the registry
   (`feature-filename-grammar.json`) contains only `handler` and `endpoint` — the loop
   body is unreachable with the current SSOT. ✔

On the delete-vs-register question: no multi-segment function candidate exists or is
planned; the machinery is untestable against the real registry (tests consume generated
constants). Delete per YAGNI — git preserves it, and re-adding rides the normal
registry-edit ⇒ full-rebuild flow.

**Recommendation:** accept as written (~80 lines removed net).

---

## F-014 — minor — Diagnostic ID taxonomy drift

**Verdict: confirmed-expanded** (population is larger and messier than reported)

Full census (grep over declared IDs): TWA0001–0019 (documented in AGENTS.md), **TWE001–006**
(Kimi listed only TWE006), and SG001/SG002/SG010/SG011 — with **SG001 declared twice**
(independent descriptor instances in the FastEndpoint and ingress generators).

Additional structural problems Kimi missed:

- `diagnostics/diagnostic-descriptors.cs` calls itself the "Authoritative registry of the
  TWE diagnostic ID range… Centralized so IDs stay unique and stable" — but declares only
  TWE001–004; TWE005 lives in `page-source-generator.cs:33` and TWE006 in
  `typed-id-source-generator.cs:42`. The registry's authority claim is false today; nothing
  prevents an ID collision it exists to prevent.
- **TWE001, TWE002, TWE004 are declared and never reported** — no `ReportDiagnostic`
  references anywhere. The registry's Design region permits reserve-first, but three of
  four resident IDs being unwired (while the contract shapes they describe *are* real
  conventions) is enforcement drift, not reservation.
- TWE003's only reporter is `RouteRegistry` — the type F-003 deletes; its replacement must
  keep the ID.

**Recommendation:** accept, expanded: document TWE/SG in the AGENTS.md table (or renumber
TWE into TWA — steward's call; TWE are all build-Errors enforcing conventions, so they are
TWA-shaped); consolidate all TWE declarations into the registry or drop its authority
claim; wire-or-delete TWE001/002/004; dedupe SG001. Executes naturally inside the
generator-hardening cluster (F-003/004/005/008).

---

## F-015 — minor — BaseApiService ↔ TestApiService transport mirror

**Verdict: confirmed-expanded** (the predicted drift has already happened)

Mirror confirmed: verb dispatch, 204/499 specials, route/content prep are duplicated, and
`TestApiService`'s Design region says outright it "mirrors that transport's semantics"
because timewarp-testing must compile in every flag combination and cannot reference the
web-feature-owned SPA stack. Both implement the same `IApiService` interface.

**Drift evidence (new):** `HandleProblemResponse` has already forked. The test copy
(`test-api-service.cs:125-127`) catches only `JsonException or InvalidOperationException`
with an honest comment ("Body was not RFC 7807 JSON — synthesize…"); the SPA original
(`base-api-service.cs:150-152`) still catches bare `System.Exception` under `// TODO: Log
the error`. Someone fixed the mirror's copy and not the original — the exact failure mode
mirrors produce. Verb-matrix asymmetry also confirmed: SPA `PrepareContent` handles
Head/Options while its dispatcher throws `NotImplementedException` for them (test side
throws `NotSupportedException` — different exception types for the same condition).

**Recommendation:** accept — extract the shared transport core into a foundation-layer
client assembly (the seam types `IApiService`/`IApiRequest`/`SharedProblemDetails`/
`ContractSerializationDefaults` already live in foundation, so the home exists); both sides
compose it with their own token acquisition. Backport the narrow catch to the SPA side
immediately even if extraction waits. Align verb handling (`NotSupportedException` in both;
drop dead Head/Options arms in `PrepareContent`/`PrepareRoute` or support them for real —
decide once, with F-008's verb posture).

---

## F-016 — note — Shared constants in bare `Features` namespace

**Verdict: confirmed-narrowed** (principled and documented in-file; Kimi's "undocumented
and unprincipled" is half wrong)

Both files carry near-identical Design-region rationale: "Lives in the Features substrate
(not a product slice) so … other product slices can reference well-known ids without
cross-slice coupling (TWA0009)." So the placement is a deliberate, documented shared-kernel
tier — at the file level. What's genuinely missing is **discoverability**: neither the
feature-placement skill nor AGENTS.md mentions a "Features substrate" tier, so the next
shared constant has no rule to find (exactly Kimi's practical concern, minus the
"unprincipled" charge).

**Recommendation:** accept the narrowed remedy — document the substrate tier (name, litmus,
the two existing examples) in the feature-placement skill + AGENTS.md layout notes. No file
moves; no new namespace (grab-bag namespaces are explicitly against repo policy).

---

## F-017 — note — Demo/scaffold residue cluster

**Verdict: confirmed** (all six items verified; one remedy refinement)

- `generic-pipeline-behavior.cs` — confirmed, but note its Design region *documents* the
  Console.WriteLines as intentional exemplar ("Intentionally does nothing beyond console
  writes"). Refinement: don't just silence it — rewrite the exemplar around `ILogger`,
  which is what a generated app should actually copy (Console.WriteLine in server code is
  the F-001 defect class being taught as a pattern). Its placement (only non-bootstrap
  logic file in an artifact folder) violates the repo's own placement rule — move under
  `api/platform/` or `features/` per the litmus.
- `v2/overview.md` — confirmed two-line stub. Delete; fold the sentence into a how-to.
- `ExampleConst` + commented leftover — confirmed (`tests/common/timewarp-testing/constants.cs:9-10`).
- Aspire comment-only `#if web` block adjacent to a real one — confirmed
  (`aspire-app-host/program.cs`); merge the pair.
- `feature-membership.targets` comment drift — confirmed by grep: the "may be absent"
  paragraph exists in api and grpc, absent in web (0/1/1).
- AGENTS.md "api `platform/` is empty (no content yet)" — confirmed wrong: the directory
  does not exist (`ls: cannot access`). Fix the wording (or create the empty tree if the
  membership targets expect it — check `ApiPlatformTreeRoot` globbing tolerates absence,
  which the api targets' own comment says it does).

**Recommendation:** accept as one batchable sweep with the F-011 one-liner.

---

## Summary

All 17 findings verified against the tree; none rejected outright. Verdict distribution:
**confirmed** ×7 (F-002*, F-006, F-011, F-012, F-013, F-015†, F-017), **confirmed-expanded**
×8 (F-001, F-003, F-005, F-007, F-008, F-010, F-014, F-015†), **confirmed-narrowed** ×1
(F-016), **corrected** ×1 (F-009). (*F-002 carries a remedy correction: TWA0005 retires
entirely — there is no FastEndpoint path for it to keep. †F-015 confirmed with expansion.)

**Material corrections to Kimi's report:**
1. **F-009 mechanics are wrong**: `MOCK_AUTHENTICATION` is Debug-only, not unconditional —
   Release builds (what template-smoke validates and `dotnet publish` produces) compile the
   live MSAL/B2C branch against placeholder config, and (per F-010) without its script tag.
   The auth stack silently flips between Debug and Release. Interim one-line fix proposed.
2. **F-002's remedy parenthetical is wrong**: deleting the MVC bridge retires TWA0005
   entirely; TWA0006 is what survives.
3. **F-003 is worse than reported**: the static registry also breaks IDE incremental
   builds outright (phantom self-conflicts; endpoints silently stop generating).
4. **F-004's drift is not hypothetical**: `[ApiEndpoint]`+`[ClientOnlyContract]` today
   produces a hosted endpoint with no ingress route and no diagnostic; the three walkers
   already disagree.

**Verification-discovered items not in Kimi's report:**
- Passwordless.dev residue (F-010 adjacent): third-party CDN script + hardcoded TimeWarp
  tenant key shipping in template output; Console.WriteLine of options in the SPA service.
  Tracked by 104-016/104-021 — disposition should confirm those specs cover the key/CDN
  removal.
- TWE001/002/004 declared but never reported; TWE registry's centralization claim false;
  SG001 declared twice (F-014 expansion).

**Suggested theme grouping for follow-on tasks** (per task 131's one-task-per-theme rule):
A. Platform-package hygiene — F-001 + F-002 (foundation-server deletions, TWA0005
   retirement).
B. Generator/analyzer hardening — F-003 + F-004 + F-005 + F-008 + F-014 (one tree, one
   rebuild discipline).
C. Exemplar de-dup — F-006 (identity) and F-015 (transport core) — possibly two tasks;
   different layers.
D. Gate/tooling SSOT — F-007 + F-012.
E. Chrome/residue sweep — F-010 + F-011 + F-013 + F-016 + F-017 (+ F-009 interim
   one-liner; posture rides 104-021).
