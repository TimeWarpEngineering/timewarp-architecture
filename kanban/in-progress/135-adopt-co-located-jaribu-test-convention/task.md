# Adopt co-located Jaribu test convention

## Description

Land the co-located Jaribu test convention on dev for real, using the evidence and blockers
from spike task 134 (`kanban/done/134-spike-jaribu-co-located-integration-tests/findings.md` —
read it first; the spike branch `spike/134-jaribu-co-located-integration-tests` holds worked
proof files to port, not merge).

**Decided (Steve, 2026-07-29):** the membership-guard carve-out is a **registered-unrouted
`tests` suffix** in `feature-filename-grammar.json` — NOT the spike's exclude-glob. Test files
under `features/`/`platform/` stay matched-and-validated by `ValidateFeatureFileMembership`
(so orphaned/misnamed files still get the teaching error) but are routed into no layer
project. Grammar remains the single vocabulary for the trees.

Testing philosophy constraints carry over from 134: integration-over-unit, "don't mock your
friends" (mock only externalities via the timewarp-testing `configureServicesDelegate` hook),
framework docs stay in the `tw-jaribu` skill.

## Requirements

1. **Grammar registration:** add `tests` to
   `source/analyzers/timewarp-architecture-convention-analyzers/feature-filename-grammar.json`
   as a recognized suffix with NO layer-project routing (no Compile glob emitted for it in any
   family's `feature-filename-grammar.g.props`). Applies to all grammar families (web, api,
   grpc). Define TWA0015/TWA0016 semantics for it (a `-tests.cs` file is a valid archetype;
   function-segment pairing rules must not false-positive on it). Registry edit ⇒ full
   rebuild (AGENTS.md). Remove the spike's `Exclude="**/*-tests.cs"` approach — the guard
   must ACCEPT `-tests.cs` via the grammar, not skip it.
2. **Template safety (spike blocker M1):** the `#if !JARIBU_MULTI` switch in co-located
   runfiles must survive `dotnet new` generation. Candidates (implementer picks, proves via
   regression gate): `cnd:noEmit` comment-marker escape around the directive pair;
   template.json exclusion of `-tests.cs` from conditional processing; or a
   template-recognized symbol. Extend **`dev template-smoke`** to assert generated apps'
   co-located tests still compile and run (this is the permanent regression gate; TWA0008/0010
   family hazard: non-template `#if` symbols in template content).
3. **Runfile preamble convention:** standardize and document the co-located runfile header:
   shebang → `#:project`/`#:package`/`#:property PublishAot=false` (spike evidence: .NET 10
   file-based apps default `PublishAot=true`, breaking `ContractSerializationDefaults`
   reflection JSON) → `#region Purpose` → JARIBU_MULTI switch. Decide the analyzer-noise
   story for `-tests.cs` under `source/`: per-file pragmas (spike form) vs a scoped NoWarn
   for the suffix — prefer whatever keeps production files in the same folders fully analyzed.
4. **Port the spike proof files onto dev** as the first real co-located tests (template-safe
   form, per this task's conventions):
   `source/container-apps/web/features/admin/roles/create-role/create-role-tests.cs` and
   `source/container-apps/api/features/weather-forecast/get-weather-forecasts/get-weather-forecasts-tests.cs`.
   Both must pass standalone. Keep the timewarp-testing manual-instantiation pattern; adopt
   class-scoped lifetime from timewarp-jaribu#19 if it has shipped by then (check), else keep
   the documented Lazy-static workaround.
5. **Aggregator sequencing (spike blocker M2):** do NOT commit a JARIBU_MULTI aggregator
   under `tests/` while `dev test` still uses the `dotnet test <csproj-path>` invocation —
   it breaks `dev test` on .NET 10 MTP projects. The aggregator + `dev test` MTP support land
   together in the follow-up `dev test` task (create it when its discovery-shape decision is
   made — 134 findings §8 Q2). Until then co-located tests are standalone-run only; note this
   explicitly in the docs delivered by requirement 6.
6. **Documentation:** update AGENTS.md (layout + enforcement sections), the
   `tw-feature-placement` skill (grammar table), `documentation/developer/standards/`
   file-naming/testing pages as applicable, and the `tw-jaribu` skill if the repo-specific
   preamble belongs there. State the migration policy: new tests are co-located Jaribu from
   adoption; existing Fixie projects migrate opportunistically slice-by-slice; `tests/`
   host-level integration suites migrate last or never; Playwright e2e unchanged.
7. **Gates:** `dev build` 0/0; `dev template-smoke` green (including the new regression
   coverage); both runfiles pass standalone; `ganda repo audit` clean.

Out of scope: `dev test` changes and the aggregator commit (own task after findings §8 Q2);
Aspire multi-resource tier (findings §8 Q3); migrating any existing Fixie suite.

## Checklist

- [x] `tests` registered in grammar SSOT as recognized-unrouted (`"unroutedLayers"`); g.props
      regenerated ×3 families; no exclude-glob used; guard integrity proven by negative probe
- [x] TWA0015/0016 semantics for `-tests.cs` defined + analyzer tests (102/0; zero analyzer
      code changes needed; registry-sync test split routed/unrouted)
- [x] Template-safety mechanism: `cnd:noEmit` escape (matches web-spa precedent); M1 failure
      reproduced without escape, survival proven with it
- [x] `dev template-smoke` two-tier regression coverage (directive survival + standalone
      `dotnet run` of generated test files); failure path proven by fault injection
- [x] Runfile preamble convention decided and applied — NOTE plan correction: bare
      `#:property NoWarn=…` REPLACES the property; canonical form is `NoWarn=$(NoWarn);…`
- [x] Both spike proof files ported onto dev (100755), passing standalone (5/5, 2/2)
- [x] timewarp-jaribu#19 checked: still open → documented Lazy-static host workaround kept
- [x] AGENTS.md + repo-local tw-feature-placement skill + file-naming.md updated; migration
      policy + standalone-only-until-136 + enforcement-surface caveat stated; cross-repo
      tw-jaribu skill pointer deferred as follow-up
- [ ] Gates: `dev build` 0/0 ✓, template-smoke green ✓, analyzer+sourcegen tests ✓ —
      `ganda repo audit` NOT clean: `kebab-path-names` fails on ~83 PRE-EXISTING paths on
      dev itself (none from this diff; verified both on the branch and on dev). Blocks the
      tw-pr gate; needs Steve's decision (exceptions vs rename task vs waive).
- [x] Kanban mutations committed
- [ ] PR opened + merged per tw-pr (pending: audit-debt decision + `dev check-version`
      version/pin bump per task-124 policy — template content changed)

## Notes

- Origin: task 134 findings §§4–5, 8–9. Strategic decision Q1 resolved by Steve (registered-
  unrouted suffix); Q2 (`dev test` discovery shape) and Q3 (Aspire tier) remain open and are
  deliberately fenced out of this task.
- Spike branch is reference material; port files fresh (template-safe), don't merge it.

- **Plan (2026-07-29):** full implementation plan in `plan.md`. Four decided mechanisms:
  `"unroutedLayers": ["tests"]` in the grammar JSON (python generator, six touch points);
  `tests` as layer-only → TWA0015 fires on `-handler-tests.cs` for free; `cnd:noEmit` escape
  for the JARIBU_MULTI switch (in-repo precedent: web-spa/program.cs); standardized
  `#:property NoWarn=…` preamble directive (TWA0004 never suppressed). timewarp-jaribu#19
  still open → Lazy-static host stays. Global tw-jaribu skill edit deliberately skipped
  (external repo) — pointer recorded as follow-up.

## Results

**Implementation complete and review-clean; merge pending two human gate decisions.**

- **Branch:** `Claude/2026-07-29/adopt-co-located-jaribu-tests` (off dev), 5 commits:
  938a5fe6 grammar/generator/analyzer-tests · c83e088e ported test files · b316c972
  template-smoke · f33e78a7 docs · 20646757 review fixes.
- **What landed:** `"unroutedLayers": ["tests"]` in the grammar SSOT (guard accepts, no
  Compile glob, no Project= metadata); TWA0015 covers `-handler/-endpoint-tests.cs` with zero
  analyzer code changes; two co-located Jaribu runfiles on dev with the canonical preamble
  (`PublishAot=false`, `NoWarn=$(NoWarn);…`, Purpose regions, cnd:noEmit-escaped JARIBU_MULTI
  switch); template-smoke two-tier regression (both matrix entries green, 5/5 + 2/2 in
  generated apps); docs in AGENTS.md, skills/tw-feature-placement (canonical preamble
  section), file-naming.md, incl. migration policy and the per-file enforcement-surface
  caveat (build-time coverage returns with task-136 aggregators).
- **Gates:** `dev build --clean` 0/0; analyzers-tests 102/0; sourcegenerator-tests 59/0;
  both runfiles standalone pass; `dev template-smoke` SUCCEEDED (both tiers, both entries).
  `ganda repo audit`: no new violations from this diff; `kebab-path-names` fails on ~83
  pre-existing paths on dev itself — blocks tw-pr gate, decision pending.
- **Review (Phase 4b):** 2 rounds, effort 1. Round 1 confirmed all claims incl. negative
  probes and fault-injected tier-2 failure path; 3 findings (2 suggestion, 1 nit) → all
  fixed in 20646757, verified round 2. Disposition: **clean** (0 open, 0 wontfix).
  Artifacts: `review/review-framework.md`, `review/round-{1,2}/`, `review/disposition.md`.
- **Plan corrections discovered:** `#:property NoWarn` is NOT additive (needs `$(NoWarn);`
  prefix); cnd:noEmit marker comments are themselves stripped from generated output; Jaribu
  TerminalSink emits ANSI unconditionally (harness strips before parsing). Environment
  footgun (not committed): stale `2.0.0-smoke` NuGet cache shadows fresh template packs.
- **Follow-ups:** task 136 (aggregators + dev test MTP — restores build-time TWA0015/16
  coverage for tests files); cross-repo tw-jaribu skill pointer update; optional layer-casing
  near-miss diagnostics (accepted as-is, consistent with existing layers); pre-existing
  kebab-path-names audit debt (needs its own decision/task).

## Session

- Created: c6f1a13b-487f-4085-bf61-ba4761e8579e (2026-07-29)
- Plan + orchestration: c6f1a13b-487f-4085-bf61-ba4761e8579e (2026-07-29)
- Implementation + fixes: subagent build-135 (isolated worktree), 2026-07-29
- Review round 1: subagent review-135-r1 (isolated worktree, detached), 2026-07-29
- Review round 2: orchestrator verification, 2026-07-29
