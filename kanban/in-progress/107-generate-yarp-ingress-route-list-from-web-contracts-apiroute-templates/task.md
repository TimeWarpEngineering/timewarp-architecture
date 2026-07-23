# Generate YARP ingress route list from web-contracts ApiRoute templates

## Description

The AppHost's YARP ingress config (`source/container-apps/aspire/aspire-app-host/program.cs`)
hand-maintains the list of `/api` paths owned by Web.Server; everything else under `/api` falls
into the Api.Server catch-all. Nothing forces that list to track web-contracts — a missed entry
sends the path to Api.Server, which 404s with a bare body the SPA renders as a generic
"Unhandled Error". This is exactly the agreement-by-memory failure class the repo organizes
against, and it bit for real in 104-003: the new `/api/identity/*` passkey endpoints were
unreachable through the ingress (found only by human smoke test; integration tests hit web-server
directly and stayed green). Hotfix was fe722050; this task replaces the hand-list with generation
so the pit of success does the work.

Precedent: TWA0007 solved the same class of drift in the same file (Aspire resource names must be
`ServiceNames` constants). The contract attributes are the single source of truth — `[ApiRoute]`
templates on web-contracts operations already declare every Web.Server-owned path.

## Requirements

- Web.Server-owned ingress routes derive from web-contracts `[ApiRoute]` templates automatically —
  adding a contract with a new top-level `/api` segment must make the ingress route appear with no
  AppHost edit.
- Collapse per-operation routes to distinct top-level path prefixes (e.g. `api/identity/...` ×5 →
  one `/api/identity/{**catch-all}` route) so the YARP config stays small and precedence over the
  Api.Server catch-all keeps working.
- Only web-contracts routes participate (not api-contracts — those belong to the Api.Server
  catch-all). Respect template feature flags: the generated list is consumed inside the existing
  `#if web` block; no new template-conditional regions.
- Mechanism (decide during planning; both have repo precedent):
  - **Option A — source generator (lean recommendation):** attach `TimeWarp.Architecture.Generators`
    to the AppHost and emit a `WebServerApiRoutePrefixes` constants class by scanning the referenced
    web-contracts assembly for `[ApiRoute]` (the TypedId EF pass already does cross-assembly
    metadata scanning); AppHost loops the constants into `yarpConfiguration.AddRoute(...)`.
    Requires the AppHost to reference web-contracts (verify acceptable — it is dev-time
    orchestration, not a service).
  - **Option B — TWA analyzer only:** keep the hand list but add a diagnostic "web-contracts
    declares top-level /api segment X with no matching AddRoute" so the build breaks on drift
    (TWA0007 sibling). Weaker (still hand-edited) but zero runtime/reference changes.
  - Generation (A) preferred per the prefer-analyzers/sourcegen directive — make the right thing
    automatic, not merely checked; fall back to B only if the AppHost→contracts reference proves
    problematic.
- Collision guard: fail the build (diagnostic) if a web-contracts top-level prefix would shadow or
  be shadowed ambiguously by another server's route space.
- Remove the hand-maintained lines and the "hand-maintained, MUST gain a line" warning added to the
  AppHost Design region in fe722050; replace with a description of the generated mechanism.
- Tests: generator unit tests (prefix collapsing, web-contracts-only filtering, deterministic
  ordering); an assertion that the 104-003 regression shape (contract route with no ingress route)
  is impossible or build-breaking.

## Checklist

- [ ] Decide mechanism (A generator / B analyzer) during planning; record rationale
- [ ] Implement generation (or diagnostic) + AppHost consumption
- [ ] Prefix collapsing + web-contracts-only filtering + collision guard
- [ ] Remove hand-list; reconcile AppHost Design region
- [ ] Tests incl. the 104-003 regression shape
- [ ] Verify ingress smoke: /api/identity reachable via ingress with zero hand edits

## Notes

- Origin: 104-003 human smoke test finding (see that task's Results and commit fe722050).
- The generated prefixes intentionally cover only routing ownership — RP/origin config for
  WebAuthn at non-localhost ingress hosts is a separate, documented concern (104-003 Results).

## Session

- Created: 2026-07-20

### Implementation plan (Phase 2, 2026-07-23)

**Decisive finding: the hand list has ALREADY drifted twice more** — `/api/Roles` (5 hosted
admin endpoints) currently falls to the Api.Server catch-all through the ingress (104-003 bug
class, live), and `/api/GetCurrentUser` is stale ([ClientOnlyContract]). Mechanism: **Option A,
pure generation** — the hand list ceases to exist.

Design: `IngressRoutePrefixGenerator` (IIncrementalGenerator) in the Generators project
(timewarp-architecture-analyzers — corrected ground truth), gated by compiler-visible props
(EnableIngressRouteGeneration, IngressWebContractAssemblies=web-contracts,
IngressReservedPathPrefixes=grpc). Scans referenced assemblies for [ApiEndpoint]+[ApiRoute]
minus [ClientOnlyContract] (simple-name matching per existing scanners); collapses to top-level
`api/<segment>` prefixes (dedupe, Ordinal sort); emits `WebServerApiRoutePrefixes.All`
(ImmutableArray<string>) in the **GLOBAL namespace** — sourceName rewriting cannot touch
generator output but would break a hardcoded namespace (115 lesson). Empty-but-present emission
when enabled → consumption compiles in every flag combo. Expected current output: api/Hello,
api/Roles, api/Users, api/identity (+Roles fixed, −GetCurrentUser — both deltas ship LOUDLY in
Results/commit).

Diagnostics: **TWA0017** (web prefix shadows another server's route space — concrete case:
api/weatherforecast; also reserved-prefix grpc) + **TWA0018** (non-derivable prefix: bare api or
parameterized second segment). Release notes + AGENTS.md rows + csproj Description ranges.

AppHost: <!--#if(web)--> guarded props + web-contracts ProjectReference
(IsAspireProjectResource=false Private=false ExcludeAssets=runtime — foundation-contracts
precedent) + api-contracts ref (collision set only) + dual-mode Generators attach; program.cs
hand list → foreach over All with `webServerHttp` cluster AND
.WithTransformUseOriginalHostHeader(true) preserved; Design region rewritten (hand-maintained
paragraphs deleted, signin-token note condensed).

Standalone yarp: INCLUDED (review fold-in; live deployment gap) — same generator +
LoadFromMemory route merge onto the config-defined Web.Server cluster (cross-provider merge
SPIKED FIRST; fallback in-memory cluster); check https+original-Host cert-mismatch shape (the
AppHost 502 template) — likely move Development web cluster to http.

Tests: generator unit tests in sourcegenerator-tests (harness extended for multi-assembly +
build props; 8 cases incl. the named 104-003 regression shape); 117 smoke extension (compile-time
prefix-coverage fact, GET /api/identity/session → 200 through ingress, optional /api/Roles →
401-not-404); template both ways + UseAnalyzerPackages=true forced.

Risks ordered: yarp config-merge spike; Generators attach activates TWA0001 on AppHost/yarp;
ExcludeAssets hygiene; loud behavior deltas; standalone https hop. Order: spike → generator+tests
→ AppHost → smoke → standalone → docs/template-matrix.

- Plan: 2026-07-23 (plan agent; two live drift instances found during planning)
