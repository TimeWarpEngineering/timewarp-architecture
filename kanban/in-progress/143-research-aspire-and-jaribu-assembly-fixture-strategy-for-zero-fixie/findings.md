# Task 143 — Findings: Aspire + Jaribu fixture strategy for zero Fixie

**Date:** 2026-07-31
**Inputs:** three parallel research streams (`research/lifetime-inventory.md`,
`research/aspire-fit.md`, `research/jaribu-fixture-options.md`), all grounded in decompiled
assemblies (TimeWarp.Fixie 3.1.0, Jaribu beta.14), live measurements, and current 13.4.6 docs.
Prior art: task 134 (Aspire survey, §8), tasks 135–136, jaribu#19/#20 (029 upstream).
**Status:** RECOMMENDATION DRAFT — north-star decision is Steve's; see §6.

## 1. The finding that reframes everything

**Fixie's "assembly DI singletons" never existed.** Decompiled TimeWarp.Fixie 3.1.0 builds a
fresh ServiceProvider **per test class** (ConfigureServices re-invoked, provider disposed
between classes). Everything believed to be assembly-shared is class-scoped:

- web-server-integration-tests boots/tears down Web.Server ~14× per run.
- The Aspire-app-as-Fixie-singleton pattern (api + spa conventions) rebuilds the ENTIRE
  distributed app per consuming class — the intended sharing never happens.

Therefore Jaribu's class-scoped `SetupOnce`/`CleanUpOnce` (beta.14) is **lifetime-equivalent
to what Fixie actually delivers today**. Zero-Fixie does not give up assembly sharing,
because there is none. What Fixie actually provides is five *ergonomic* conveniences:
ctor injection, DI-graph-derived host ordering, one assembly-wide override point, structural
disposal, and Scrutor auto-discovery. Each has a small, explicit replacement (§3).

## 2. The repo actually has THREE test frameworks

Fixie (most suites), Jaribu (co-located + aggregators), and **xUnit** (aspire-tests —
template-default, contradicting AGENTS.md's "host-level suites stay Fixie"). Any north star
worth stating should resolve this to ONE framework: Jaribu. "Zero Fixie" should mean
**zero Fixie and zero xUnit**.

## 3. Preferred lifetime model: C + A (explicit fixture module + per-class hooks)

From the five options evaluated (A per-file SetupOnce; B aggregator run-scope hooks;
C explicit shared fixture module; D upstream assembly hooks; E MTP session hooks — the
newly-discovered empty `CreateTestSessionAsync`/`CloseTestSessionAsync` seam):

- **Only C expresses dependency ordering.** Every hook-shaped option (B/D/E) gives a single
  "run once" pair; `StartWeb → StartApi → StartYarp(web, api)` is just code. Fixie's DI graph
  did the ordering implicitly; C makes it explicit — consistent with this repo's
  explicit-over-magical stance (029's own rejected-alternatives said no IClassFixture DI, no
  field-scanning magic).
- **B is illusory:** the MTP loop lives in Jaribu's own code, so run-scope hooks that work
  under `dev test` require upstream changes regardless — B collapses into D, and E is the
  better-shaped D (the seam already exists, empty).
- **Standalone-single-file-first forces the authoring shape anyway:** a runfile must work
  under bare `dotnet run`, so classes call C's idempotent async factory from their own
  `SetupOnce`; any run-scope hook can only ever be an optimization, never the primitive.

**Model:** a `SharedHostGraph`-style static async factory module in `timewarp-testing`
(explicit ordering, idempotent, disposal-aware), consumed from per-class `SetupOnce`/
`CleanUpOnce` (A). Cost parity with Fixie's real behavior on day one (per-class boots — same
as today). **E (MTP session hooks) is filed upstream only when measured aggregator cost
demands run-scope sharing** — with data, not speculatively. Replacements for Fixie's five
conveniences: static factory call (vs ctor injection); explicit factory ordering (vs DI
graph); a shared helper invoked in SetupOnce (vs assembly override point); mandatory
CleanUpOnce discipline — already enforced by convention docs + the exemplars (vs structural
disposal); the existing `[ModuleInitializer]` line (vs Scrutor scan).

## 4. Aspire's role: two-lane model, no wholesale migration

Aspire is already load-bearing in 3 of 4 suite categories. The measured shape:

- **In-proc lane (hand-rolled `WebApplicationHost` + timewarp-testing):** stays for
  mediator/pipeline tests and anything needing in-proc DI substitution. The 13.4.6 docs
  re-confirm verbatim that Aspire testing cannot substitute DI across its process wall.
  Amortized boot ≈ zero; this is the fast lane.
- **Closed-box lane (`Aspire.Hosting.Testing`):** ingress/topology tests, and anything
  needing real process isolation — the OpenAPI suite is the proof that process boundaries fix
  a bug class in-proc hosting structurally cannot (FastEndpoints cross-assembly discovery
  pollution). This is the slow lane: 20–30s per full-graph fixture boot.
- **Known cost taxes to fix:** api-server-integration-tests pays BOTH lanes' boots in one
  assembly today; nothing in-repo uses partial-graph startup (`WithExplicitStart`); the SPA
  suite boots the full graph per class for 11 tests (77s test / 2:13 wall).
- **Confirmed blocker:** `MockAccessTokenProvider` is compile-time (`#if MOCK_AUTHENTICATION`)
  — config/env levers cannot flip it, so the web BFF suite cannot move to the closed-box lane
  unless a product-code change makes mock auth runtime-config-gated. That is a separate,
  deliberate decision — NOT required for zero-Fixie (the BFF suite migrates to Jaribu in the
  in-proc lane regardless).
- SPA nuance: the SPA is not an Aspire resource, so its fakes (IJSRuntime etc.) never hit the
  process wall — its Aspire usage is only "give me an ingress HttpClient". Also: a dead
  competing `SpaTestApplication<,>` path is still registered — delete on migration.

## 5. Zero-Fixie feasibility: remaining true blockers

**None structural.** With §1's finding, the blocker list reduces to build-work, not unknowns:
(a) the C fixture module doesn't exist yet; (b) suite migrations (web BFF is the largest,
~24 files); (c) aspire-tests xUnit→Jaribu (needs Jaribu equivalents of IAsyncLifetime-style
class fixture — SetupOnce covers it — and xunit's assertion swap to Shouldly);
(d) TimeWarp.Fixie retirement docs. Not blockers: assembly lifetime (never existed), vendor
ownership (we own the stack), MTP/CI (task 136 proved it), template safety (task 135 proved it).

## 6. DECISION FOR STEVE (north star)

**Draft recommendation:** adopt **single-framework Jaribu** as the north star (zero Fixie AND
zero xUnit), with the **C+A lifetime model** (explicit shared-fixture module + per-class
hooks; E upstream only when cost data demands) and the **two-lane Aspire role** (in-proc lane
for DI-substitution/pipeline tests; closed-box lane for topology/process-isolation tests; no
wholesale migration; fixed ports stay in the in-proc lane).

Rejected alternatives, with reasons: run-scope-hooks-first (B/D — reachable only via upstream
change, doesn't solve ordering, invisible coupling); Aspire-everywhere (DI wall confirmed;
20–30s boots; MOCK_AUTHENTICATION compile-time blocker); keep-Fixie-for-multi-host (its
assembly sharing is illusory — the one thing it was being kept for).

## 7. Ordered follow-up tasks (create after §6 decision)

1. **Build the shared fixture module** in timewarp-testing (C): explicit Web→Api→Yarp
   ordering, idempotent async factory, disposal contract; document in tw-feature-placement.
2. **Migrate aspire-tests xUnit→Jaribu** (smallest migration; kills the third framework;
   proves SetupOnce as IClassFixture replacement).
3. **Migrate web-server-integration-tests Fixie→Jaribu** using C+A (largest suite; proves the
   model at scale; keeps MockAccessTokenProvider in-proc lane).
4. **Consolidate api-server-integration-tests** (split in-proc vs closed-box classes so one
   assembly stops paying both boots; closed-box classes join the Aspire lane).
5. **SPA tier migration** (delete dead SpaTestApplication path; BaseTest → Jaribu shape;
   evaluate partial-graph/`WithExplicitStart` to cut the full-graph boot).
6. **Upstream (conditional, data-gated):** jaribu MTP session hooks (option E) if aggregator
   wall-clock demands run-scope host sharing.
7. **Product decision (separate):** runtime-config-gated mock auth to unlock closed-box BFF
   testing (optional; not required for zero-Fixie).
8. **Docs:** AGENTS.md testing section rewrite (three-frameworks note → one; migration policy
   update; two-lane Aspire statement); retire TimeWarp.Fixie references when suites land.

## 8. Corrections to prior records

- Task 134's Aspire survey framing ("barely used") is stale — Aspire.Hosting.Testing is now
  load-bearing in three categories.
- AGENTS.md currently says host-level suites "stay Fixie + Shouldly" and is silent on
  aspire-tests being xUnit; superseded by the §6 decision when taken.
