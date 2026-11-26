# Add a web-only combination to the template-smoke matrix

## Description

`dev template-smoke --matrix` generates the defaults plus each flag off on its own (NoPostgres, NoApi,
NoWeb, NoGrpc, NoYarp). No variant turns several flags off together. The combination the first
instance repo will be generated with (see task 491) is web only: `--api false --grpc false --yarp false`,
postgres on. Nobody builds that output today.

Add that one combination to the matrix so it is generated, built, tested and published every night.
Fix whatever it turns up.

## Propagation

template-only

## Requirements

- A `SmokeWebOnly` variant in `SmokeMatrix`: api, grpc and yarp off; web and postgres on.
- Its `ExcludedFamilies` list covers api, grpc and yarp, so an orphaned aggregator or test project
  from any of the three fails the smoke.
- The variant runs wherever the other matrix entries run: `dev template-smoke --matrix` and the
  nightly `template-smoke` job in `.github/workflows/workflow.yml`.
- `dev template-publish-smoke` (or the publish suites the matrix already runs) covers the variant for
  `aca` and `aca-neon`, the targets a web-only app deploys to.
- Any generation, build, test or publish failure in the variant is fixed in this task, or filed as
  its own task and named in Results when it is large.

## Goal (implementer restatement)

Nobody builds the web-only output (`--api false --grpc false --yarp false`, postgres on) that the
first instance repo will use. Add it to `dev template-smoke --matrix` as `SmokeWebOnly` so the
nightly matrix generates, builds, tests and publish-tests it, and fix what it turns up.

Acceptance:

- `SmokeMatrix` has `SmokeWebOnly` with api, grpc and yarp off and `ExcludedFamilies` api, grpc, yarp.
- An api, grpc or yarp test artifact left in that output fails the smoke. Today the tables only know
  the `web` and `api` families, so the grpc and yarp test artifacts get table entries too.
- The nightly `template-smoke --matrix --app-tests` job runs it with no workflow edit (the job has no
  name filter).
- The generated app's `aspire-tests` (the `aca` and `aca-neon` publish suites) pass for the variant
  under `--app-tests`.
- `dev template-smoke` can run that one variant locally, so this proof does not need the whole matrix.

## Checklist

- [x] Add `SmokeWebOnly` to `tools/dev-cli/services/template-smoke/template-smoke-matrix.cs`.
- [x] Update the header comment there ("defaults plus each flag off on its own").
- [x] Nightly job picks the variant up (matrix list or name filter in the workflow, if it has one).
- [x] `dev template-smoke --matrix` green for the new variant.
- [x] Publish smoke green for the new variant on `aca` and `aca-neon`.

## Review

- Round 1: Done

## Notes

- One combination, not the full power set. We are the consumer; this is the shape our first instance uses.
- Task 373 added the single-flag web, grpc and yarp variants. Task 395 fixed the NoYarp publish failures.
- Task 527 changes what the Azure publish output looks like with yarp off (web-server becomes the
  external app). Whichever of the two lands second updates the expectation. No hard dependency.
- Note: task 071 is already in `kanban/done/`; no edit to `done/071` is needed.
- Review: `review/review-framework.md`, `review/round-1/merged.md`, `review/disposition.md` (round 1, clean).

## Session

- Created: f7eccb81-87c9-4fb4-a0c4-0fb7417d4a2f (2026-11-25)
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 9ed4eca8-118b-4161-a5bf-46b6515adb34, max-turns 500, Done — 2026-11-25T23:09:09Z
- Review oracle: general (grok, model grok-4.7), session 01a214f8-7953-7ee1-929d-42ae433fbde6, effort 2, roster general, max-turns 120, round 1, clean — 2026-11-26
- Review oracle: review by reviewer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a214f8-7953-7ee1-929d-42ae433fbde6, max-turns 120, artifacts general.md, merged.md, disposition.md — 2026-11-25T23:17:22Z
- Implement oracle (resume): the walk stopped at results because the Results headings carried `(required before done)` (fixed in 46b3ee819). No product change since the recorded gates, so they stand — 2026-11-26

## Results

- `SmokeWebOnly` added to `SmokeMatrix` (`--api false --grpc false --yarp false`, web and postgres
  on, `ExcludedFamilies` api, grpc, yarp). Header comment updated.
- The co-located and aggregator tables only knew `web` and `api`, so grpc and yarp exclusions asserted
  nothing. New `FamilyTestArtifacts` table (`template-smoke-test-tables.cs`) plus tier-1
  `AssertFamilyTestArtifacts` (`template-smoke-test-runs.cs`, called from the runner): each flag-owned
  test project or file is absent when any required family is excluded and present otherwise. Entries:
  api-server-integration-tests, api/yarp test-server applications, the grpc superhero tests, the
  yarp-integration-tests project and its grpc-route test. `SmokeNoGrpc` and `SmokeNoYarp` now list
  `grpc` and `yarp` so those variants run the check too.
- New `dev template-smoke --variant <name>` runs one matrix entry (unknown name fails before packing
  and lists the known names). Option and skill text (`co-located-jaribu-runfiles.md`) updated.
- Nightly: `.github/workflows/workflow.yml` runs `template-smoke --matrix --app-tests` with no name
  filter, so it picks up `SmokeWebOnly` with no workflow edit.
- Publish: `dev template-publish-smoke` checks the published packages after a release, not
  `aca`/`aca-neon` output. Those targets are covered by the generated app's `aspire-tests`
  (`aca-publish-tests.cs`, `aca-publish-secrets-tests.cs`, `aca-neon-publish-tests.cs`), which
  `--app-tests` runs per variant. They passed for `SmokeWebOnly` (70/70).
- The web-only output turned up no generation, build, test or publish failure, so there was nothing
  to fix in the template and no follow-up task.

Gates:

- `./bin/dev build`: succeeded, 0 warnings (93s).
- `dotnet run --file tools/dev-cli/dev.cs -- self-install`, then
  `./bin/dev template-smoke --variant SmokeWebOnly --app-tests`: SUCCEEDED. Tier 1 including the new
  family check passed; create-role 6/6 and hello 4/4 standalone; web-jaribu-tests 504/504;
  api-jaribu-tests and timewarp-testing-tests correctly excluded; aspire-tests 70/70,
  web-contracts 48/48, web-domain 57/57, web-infrastructure 101/101, web-server-integration 458/458,
  web-spa-integration 644/644, web-spa-playwright 46/46.
- `FamilyTestArtifacts` vs real output: `dotnet new timewarp-architecture` default, `--api false`,
  `--web false`, `--grpc false`, `--yarp false` and `--postgres false`, and the
  smoke's SmokeWebOnly output, each matched the table's presence and absence for every entry.
- Test files: none added or edited. No test file names the changed types (`TemplateSmokeMatrix`,
  `TemplateSmokeRunner`, `TemplateSmokeTestRuns`, `TemplateSmokeTestTables`, `TemplateSmokeCommand`);
  the template-smoke run above is their gate.
- `./bin/dev lint-skills` (skills/ changed): 11 skills linted, 11 passed.
- `./bin/dev template-smoke --variant Nope`: unknown-variant message, exit 1.
- `ganda repo audit`: passed (tree 71705b933145, all checks pass).

### How to validate

- Smoke: `dotnet run --file tools/dev-cli/dev.cs -- self-install`, then
  `./bin/dev template-smoke --variant SmokeWebOnly --app-tests` (needs Docker).
- Expect: `Flag-owned test artifacts match the excluded families (tier 1).`, the api aggregators
  reported `correctly excluded (--api false)`, `All 7 generated test projects passed.`, and
  `Template smoke SUCCEEDED`.
- Smoke: `./bin/dev template-smoke --variant Nope`.
- Expect: `Unknown --variant 'Nope'. Known: SmokeDefault, …, SmokeWebOnly` and exit 1.

### Review disposition

- Round 1, roster general, effort 2. Counts: bug 0 open / 0 fixed / 0 wontfix; suggestion 0; nit 0. Final open count 0.
- Disposition: clean. No wontfix and no escalation.
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.
- The goal restatement matches the original task. No `## Learnings` misread.
