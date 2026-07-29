# Spike Jaribu co-located integration tests

## Description

Prove that TimeWarp.Jaribu can serve as this repo's test framework for **co-located tests**
(test runfiles living inside `features/` slices next to the code they test) before committing
to the convention repo-wide. The direction is decided: Jaribu is the destination; Fixie remains
only in not-yet-migrated projects. This spike de-risks the two hardest claims on one or two
real files each — it does NOT migrate the existing test suite.

**Testing philosophy (constraint, not preference):** this repo prefers integration tests over
unit tests. "Don't mock your friends" — never mock code we control; mock only externalities we
don't control (third-party services, clocks, external APIs). A spike outcome that only proves
host-free unit-style tests is a FAILURE for this task: the integration-test case is the primary
case. Playwright e2e tests are a third tier that stays; the spike should place Jaribu relative
to them, not replace them.

Origin: brainstorm on co-locating tests next to source (2026-07-29 session). Key insight from
that discussion: Jaribu runfiles declare their own dependencies (`#:project`), making isolation
a per-file structural fact and collapsing the per-layer test-csproj routing question — the
axis-1 grammar would need only a flat `tests` suffix carved out of the membership guard.

## Requirements

1. **Contracts round-trip proof (easy case):** one Jaribu runfile co-located in a web slice
   (e.g. `features/counter/`), grammar-conformant name (`<name>-tests.cs` shape), referencing
   `web-contracts` via `#:project`, running standalone (`./file.cs`) — physically host-free.
2. **Integration-test proof (primary case):** one Jaribu runfile that spins a real server host
   and exercises an endpoint end-to-end through the mediator pipeline (happy path + validation
   rejection), honoring the fixed-port / serialized-execution constraint. Must evaluate whether
   the existing integration-test infrastructure's **flexible test DI container** abilities
   (service overrides for mocking externalities only) carry over to a runfile, and document
   what carries, what needs porting into a shared helper, and what Jaribu itself lacks.
3. **Aspire testing research:** survey current Aspire testing support (e.g.
   `Aspire.Hosting.Testing` / `DistributedApplicationTestingBuilder` and whatever is newest as
   of the spike) and assess whether it supersedes or complements the hand-rolled test-host
   approach for integration tests. We may be behind on what Aspire now offers — check latest.
4. **Multi-mode / IDE proof:** the same runfiles discovered via a `JARIBU_MULTI` project so
   `dotnet test` / IDE test explorers see them. If MTP discovery has rough edges, file the gap
   upstream in Jaribu (fix upstream, never work around — house rule).
5. **Membership-guard carve-out sketch:** identify (not necessarily implement) what the
   `feature-filename-grammar.json` registry and `feature-membership.targets` guard need so a
   `-tests.cs` file under `features/` is accepted but globbed by no layer project. Note
   interactions: TWA0004 `#region Purpose` after the shebang line, TWA0008/template
   conditionals now applying to test files, TWA0009 namespaces
   (`…Features.<Id>.<Sut>_.<Action>_Given_` nesting), TW0001 kebab naming vs Jaribu's
   `{sut}.{action}.cs` convention (grammar wins inside feature trees).
6. **Findings report:** write spike findings to this folder (`findings.md`) covering: what
   worked, upstream Jaribu gaps filed, Aspire recommendation, proposed test-tier map
   (co-located Jaribu integration/contract tests → `tests/` host-level tests → Playwright
   e2e), and the follow-up task list for actual adoption (grammar change, `dev test` glob
   update, migration order — integration tests under `tests/` migrate last or never).

Out of scope: migrating existing Fixie projects; changing `dev test`; landing the grammar
change. Spike artifacts may live on a branch that never merges — findings.md is the deliverable
of record.

## Checklist

- [x] Contracts round-trip runfile co-located in a slice, runs standalone (`create-role-tests.cs`, 5/5; branch `spike/134-jaribu-co-located-integration-tests`)
- [x] Integration-test runfile: real host, endpoint happy path + validation rejection (`get-weather-forecasts-tests.cs`, 2/2 on :7255, real FastEndpoints + mediator)
- [x] Test DI container flexibility evaluated; carry/port/gap documented (timewarp-testing host classes carry unchanged; only Fixie DI role replaced by manual `new`; Jaribu lacks class-scoped fixture lifetime)
- [x] Aspire testing features surveyed (latest); recommendation written (`aspire-testing-survey.md` — complement, not supersede; no in-process DI overrides in Aspire testing)
- [x] JARIBU_MULTI project: `dotnet test` / IDE discovery of the same files (`tests/container-apps/jaribu-spike-tests/`, 7/7 in 2.6s; needed `TestingPlatformDotnetTestSupport` + `global.json` runner opt-in)
- [x] Upstream Jaribu gaps filed: timewarp-jaribu#19 (class-scoped fixture lifetime), timewarp-jaribu#20 (MTP dotnet-test docs + sdk-pin landmine)
- [x] Membership-guard + analyzer interaction sketch written (proven as real diff: `Exclude="**/*-tests.cs"` in both web+api `feature-membership.targets`; `dev build` 0/0; TWA0004/CA1707 etc. confirmed firing on runfiles under `source/`)
- [x] findings.md written with adoption follow-up task list
- [x] Kanban mutations committed (each phase committed on dev as it landed)

## Notes

- "Don't mock your friends": mock ONLY externalities we don't control. Test through real
  layers we own. Unit-testing code we control behind mocks is considered wasted effort here.
- Existing integration tests (`tests/container-apps/**-integration-tests`) predate this and
  have battle-tested test-DI-container flexibility — treat them as the bar to clear, not
  legacy to discard.
- Sequencing rationale: framework decision precedes co-location convention (avoids building
  per-layer test-csproj routing that Jaribu obsoletes). Co-location grammar work is a
  follow-up task gated on this spike's findings.

- **Plan (2026-07-29):** full implementation plan in `plan.md` (this folder). Spec correction
  from planning: requirement 1's "e.g. `features/counter/`" example was stale — counter is
  client-only SPA state with no wire contract; requirement 1 targets
  `web/features/admin/roles/create-role/` and requirement 2 targets
  `api/features/weather-forecast/get-weather-forecasts/` (anonymous endpoint, existing Fixie
  suite to duplicate). Strategic post-spike questions for Steve are listed at the end of
  `plan.md`; none block the spike.

## Results

**Verdict: spike SUCCEEDS** — Jaribu co-located tests are viable for both the contracts case
and the primary real-host integration case. Full report: `findings.md` (deliverable of record).

- **Spike branch:** `spike/134-jaribu-co-located-integration-tests` (3 commits off dev;
  evidence artifact, not intended to merge as-is).
- **Proofs (all independently re-verified in review):** co-located contracts runfile 5/5
  standalone; co-located integration runfile 2/2 against the real api host on :7255 (real
  FastEndpoints + mediator, happy path + validation rejection); solution build 0/0 with both
  `-tests.cs` files present (carve-out in web+api feature-membership.targets); JARIBU_MULTI
  aggregator 7/7 via `dotnet test`.
- **Key evidence:** Directory.Build.props/analyzer chain DOES apply to file-based apps
  (virtual csproj + upward walk); .NET 10 runfiles default `PublishAot=true` (breaks
  reflection JSON — needs `#:property PublishAot=false`); timewarp-testing host classes carry
  over unchanged, only Fixie's DI role replaced by manual instantiation.
- **Confirmed adoption blockers:** (M1) `#if !JARIBU_MULTI` is not template-safe — dotnet-new
  strips the directives and generated apps fail CS8802; (M2) `dev test`'s
  `dotnet test <csproj-path>` invocation fails on MTP projects on .NET 10.
- **Aspire (req 3):** complements, does not supersede — Aspire testing is closed-box
  (separate processes, no DI substitution), so the hand-rolled host + configureServicesDelegate
  stays for endpoint tests; Aspire.Hosting.Testing only for a possible future multi-resource
  tier. Survey: `aspire-testing-survey.md`.
- **Upstream filed:** timewarp-jaribu#19 (class-scoped fixture lifetime),
  timewarp-jaribu#20 (MTP dotnet-test docs, sdk-pin landmine, invocation forms).
- **Review (Phase 4b):** 1 round, effort 1 (general reviewer), all implementation claims
  confirmed; final counts — bug 2 wontfix, suggestion 1 wontfix, 0 open; disposition:
  **accepted-exceptions** (all three findings are spike-branch-only defects recorded as
  adoption evidence; rationale in `review/round-1/merged.md` and `review/disposition.md`).
- **Open strategic decisions (Steve), in dependency order — findings.md §8:** carve-out
  mechanism (exclude-glob vs registered-unrouted `tests` grammar suffix); `dev test`
  discovery shape for co-located tests; whether/when to add an Aspire multi-resource tier.
- **Follow-up tasks** to create after those decisions: findings.md §9 (adoption convention +
  template-safety fix, `dev test` MTP support, upstream follow-through, migration policy,
  conditional Aspire tier).

## Session

- Created: c6f1a13b-487f-4085-bf61-ba4761e8579e (2026-07-29)
- Plan + orchestration: c6f1a13b-487f-4085-bf61-ba4761e8579e (2026-07-29)
- Implementation: subagent build-134 (isolated worktree, spike branch), 2026-07-29
- Review round 1: subagent review-134-r1 (isolated worktree, detached), 2026-07-29
