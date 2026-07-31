# Round 2 — independent verification (post-hoc, human-requested)
**Date:** 2026-07-31
**Reviewer:** independent agent (Claude orchestrator session), isolated worktree
**Scope:** origin/dev..dev — all 9 unpushed commits (136 main, 140 aspire ports, 141 docs,
c0edcd83 SetupOnce adoption), i.e. wider than round-1's single-commit scope

## Verification (all independently reproduced)

- `dev build` 0/0 (fresh self-installed dev-cli); aggregators confirmed absent from solution build
- FULL `dev test`: 18 projects green incl. api-jaribu-tests 2/2 + web-jaribu-tests 5/5 via the
  new MTP path; 3m20s wall
- `dev template-smoke` SUCCEEDED: both matrices, tier 3 aggregators (web 5/5, api 2/2) in each
- `ganda repo audit` 23/23 PASS
- MTP detection (test-command.cs): keys off project-local global.json containing literal
  "Microsoft.Testing.Platform" (ordinal substring); Fixie path byte-identical to pre-diff;
  failure propagation verified (non-zero → allPassed=false → ExitCode=1); no current
  false-positive risk (no other tests/ project has a local global.json)
- Aggregator csprojs: JARIBU_MULTI + TestingPlatformDotnetTestSupport, features/ AND platform/
  globs, Link metadata, ProjectReferences match exemplars, CPM pins TimeWarp.Jaribu[.TestingPlatform]
  = beta.14 = latest on nuget.org (no backward-pin), global.json mirrors root SDK pin
- Template flags: template.json (!api)/(!web) excludes structurally cover the aggregator dirs
  (no edit needed); NOTE SmokeMatrix has no flag-off entry — see Issue 1
- SetupOnce/CleanUpOnce (c0edcd83): Lazy workaround fully removed, host disposed in CleanUpOnce,
  zero stale copies repo-wide; exercised live via dev test + smoke
- Task 140: +100 port offsets, service/ingress ports and ServiceNames untouched (TWA0007 safe),
  no collision with 7000/7255/8443; rationale documented
- Task 141: kanban-markdown-only, confirmed
- AGENTS.md / tw-feature-placement SKILL.md diffs accurate incl. honest grpc deferral
- Version note: source <Version> still 2.0.0-beta.10 (shipped via PR #293); this batch ships new
  template content ⇒ needs beta.11 + pins bump in the ship commit (task-124 policy)

## Round-1 audit

Round-1 (Grok, effort 1) scoped only commit 52dda114; its three findings' "fixed" claims all
verified real in code (SKILL.md maintenance bullet, TryParseMtpSummary dual-form regex,
AGENTS.md sdk-pin note). Blind spots: post-fix commit dab0f5d8 got no re-review round, and
tasks 140/141/c0edcd83 were outside its remit — this round is their first independent look.
Within its scope, round-1 was sound.

## Issues

### R2-1 — Severity: suggestion — Status: open
- File: tools/dev-cli/endpoints/template-smoke-command.cs (SmokeMatrix)
- SmokeMatrix has no --api/--web-off entry, so the template-flag exclusion path (for the new
  aggregators AND every pre-existing flagged test project) is never generated+built by smoke/CI —
  only statically verified. Pre-existing gap; 136 didn't introduce it, didn't close it.
- Suggestion: add a flag-off matrix entry (e.g. SmokeNoApi) so orphaned-project regressions
  surface automatically.

### R2-2 — Severity: nit — Status: open
- File: tools/dev-cli/endpoints/test-command.cs (IsMicrosoftTestingPlatformProject)
- Detection keys solely off the local global.json string; the csproj's
  TestingPlatformDotnetTestSupport property is unlinked. A future aggregator missing the
  global.json silently falls to the VSTest path and fails at dev-test time.
- Suggestion: one-line authoring callout in tw-feature-placement's aggregator note: the
  global.json test.runner value is what dev test keys off — mandatory per aggregator.

## Verdict

**Zero bugs. Ship-worthy.** Both open items are pre-existing-class coverage/doc gaps, deferred
to human disposition (fix inline vs follow-up task). Version bump required at ship time.
