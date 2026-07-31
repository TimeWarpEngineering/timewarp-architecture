# Round 1 — general
**Date:** 2026-07-31
**Scope reviewed:** commit 52dda114 / task 136 plan D1–D8

## Summary

Task 136 lands cleanly against plan D1–D8. MTP projects are detected only via a project-local
`global.json` containing `Microsoft.Testing.Platform`, then invoked as bare
`dotnet test -c Release` with cwd = project directory; Fixie stays on
`DotNet.Test().WithProject(...).WithWorkingDirectory(RepoRoot)`. Serialized `foreach` over
`tests/**/*.csproj` is unchanged (fixed ports 7000 / 7255 / 8443).

Aggregators match D4/D5: `JARIBU_MULTI`, `TestingPlatformDotnetTestSupport`,
`TimeWarp.Jaribu.TestingPlatform` + Shouldly, Compile+Link globs under
`$(SourceDirectory)container-apps/{web,api}/{features,platform}/**/*-tests.cs`, ProjectReferences
aligned with the two exemplar `#:project` directives (web → web-contracts; api → api-contracts +
timewarp-testing). Per-aggregator `global.json` mirrors root SDK `10.0.301` +
`test.runner: Microsoft.Testing.Platform`; root `global.json` has no runner (Fixie-safe). CPM pin
`TimeWarp.Jaribu.TestingPlatform` 1.0.0-beta.14 matches Jaribu beta.14. Package props
(`IsTestingPlatformApplication`, `OutputType=Exe`, `Features=FileBasedProgram`) flow from the
NuGet package — no explicit import fallback needed today.

Template safety (D6): no `template.json` change; existing `(!api)` / `(!web)` excludes already
cover `tests/container-apps/{api,web}/**`. Aggregators are not in `.slnx` (D8). Template-smoke
tier 3 (`AssertJaribuFamilyAggregatorsAsync`) runs bare MTP `dotnet test` serially for web (5)
and api (2) after tier 2. AGENTS.md and `tw-feature-placement` correctly describe aggregators as
the CI/`dev test` enforcement surface for the registered-unrouted `tests` layer.

Re-verified against the tree: 5 web test methods / 2 api test methods (SetupOnce/CleanUpOnce on
api :7255); only those two co-located `*-tests.cs` files exist under the globs. No correctness
bugs found in MTP detection, serialization, ProjectReferences, or template flag coverage.

Residual noted by implementer (full `dev template-smoke` / Phase 5 gates) is process, not a
diff defect.

## Issues

### Issue 1 — Severity: suggestion
- File: `skills/tw-feature-placement/SKILL.md:308-309` (and `tools/dev-cli/services/template-smoke-harness.cs:565-569`)
- Description: Adding a co-located runfile is documented as requiring an update to the family
  aggregator's `ProjectReference` list when new `#:project` deps appear. Tier 3 also hardcodes
  expected MTP succeeded counts (`web` → 5, `api` → 2) in `JaribuFamilyAggregators`. A new
  test method (or a new runfile with more methods) that builds and passes under `dev test`
  still fails `dev template-smoke` until those literals are bumped — and the skill checklist
  never mentions that second maintenance step. Counts themselves are correct for the two
  exemplars today (create-role: 5 methods; weather: 2 methods excluding SetupOnce/CleanUpOnce).
- Suggestion: Extend the runfile preamble / aggregator note in `tw-feature-placement` to require
  updating `TemplateSmokeHarness.JaribuFamilyAggregators` expected counts (and, if a new family
  gains runfiles, a new aggregator + matrix entry) whenever co-located test method totals change.
- Status: open

### Issue 2 — Severity: suggestion
- File: `tools/dev-cli/services/template-smoke-harness.cs:593-598,754-766`
- Description: `TryParseMtpSummary` only accepts line-anchored multi-line MTP host summaries
  (`^\s*total:\s*(\d+)\s*$` / `^\s*succeeded:\s*(\d+)\s*$`). The SDK also commonly emits a
  single-line form (`Test summary: total: N, failed: …, succeeded: N, …`) that does not match.
  Fail-closed behavior is correct (no silent green on parse miss), and the implementer reported
  bare `dotnet test` web 5/5 and api 2/2 with the multi-line form. Plan residual already flagged
  parse instability; if a future SDK/MTP release emits only the compact line (or changes labels),
  tier 3 becomes a false red while exit code 0.
- Suggestion: Prefer last-match of a looser pattern that also accepts the compact
  `Test summary: total: N, … succeeded: N` form, or fall back to exit-code-only when parse fails
  but still log a warning. Keep hard expected counts so discovery regressions stay visible.
- Status: open

### Issue 3 — Severity: nit
- File: `tests/container-apps/{web,api}/*-jaribu-tests/global.json` vs root `global.json`
- Description: Plan risk (SDK pin drift) is real: two project-local `global.json` files duplicate
  root `sdk.version` `10.0.301`. Nothing in AGENTS.md / `dev check-version` (or similar) reminds
  agents to bump the aggregator pins with the root SDK. Not a defect today — pins match.
- Suggestion: One-line note under Build/run/test or the aggregators blurb in AGENTS.md:
  "aggregator `global.json` SDK pins must mirror root on SDK bumps."
- Status: open
