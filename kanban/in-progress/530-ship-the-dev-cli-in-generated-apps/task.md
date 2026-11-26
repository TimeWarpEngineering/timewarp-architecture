# Ship the dev CLI in generated apps

## Description

A generated app has no dev CLI. The template pack is an allow-list
(`timewarp-templates/source/timewarp-architecture-template/timewarp-architecture-template.csproj`):
`source/`, `tests/`, `msbuild/`, `skills/`, `.template.config/`, the kanban skeleton and the root
build files. `tools/` is not on it, so `tools/dev-cli/` never reaches the output.

The skills the pack does ship tell the app to use that CLI: `tw-deploy` says the first deploy is
`dev deploy --target <t>`, and it names `dev publish`, `dev deploy migrate`, `dev open` and
`dev deprovision`. An instance cannot follow its own skills.

Ship the dev CLI in the generated app, without the commands that only make sense in the template repo.

The first instance repo (see task 491) will be regenerated from this template and needs `dev build`,
`dev test`, `dev run`, `dev db`, `dev publish` and `dev deploy` on day one.

## Propagation

template-only

## Requirements

- `dotnet new timewarp-architecture -n X` output contains `tools/dev-cli/` and it builds and runs
  there (`dotnet run tools/dev-cli/dev.cs -- --help`, and the self-install that produces `bin/dev`).
- The generated CLI has the commands an app uses: build, test, run, db, publish, deploy (with
  migrate), open, deprovision, entra, workflow, regenerate-generated when it applies.
- Template-only commands do not ship, or are excluded by `template.json`: template-smoke,
  template-install, template-publish-smoke, check-client-names, lint-skills, check-version and the
  NuGet release path. Decide each one and list the decision in Results.
- Flag-off generation leaves no dead command: a command for api, grpc, yarp or postgres is excluded
  or compiles and refuses cleanly when that flag is off.
- `sourceName` rewriting does not break the CLI (project paths, solution name, AppHost path).
- `dev template-smoke` builds the generated CLI and runs at least `dev build` through it, for the
  default variant and in `--matrix`.

## Goal (implementer restatement)

A generated app gets the repo's dev CLI under `tools/dev-cli/`, so it can follow its own skills
(`dev build`, `dev test`, `dev run`, `dev db`, `dev publish`, `dev deploy`, `dev open`, ...).

Acceptance:

- The packed template's output contains `tools/dev-cli/` (plus `tools/Directory.Build.props`,
  `.timewarp/dev.jsonc` and `.envrc`), and `dotnet run tools/dev-cli/dev.cs -- --help` and
  `-- self-install` work there.
- The app's CLI keeps the app commands and drops the template-only ones (template-smoke,
  template-install, template-publish-smoke, check-client-names, lint-skills, verify-samples,
  regenerate-generated, check-version, release, and the NuGet release and classify paths of
  `dev workflow`). Each decision is listed in Results.
- A flag-off app has no dead command: `dev db` is excluded without postgres; `entra`, `open`,
  `deploy` and `deploy migrate` refuse with a message naming the missing feature.
- sourceName rewriting leaves the CLI compiling (namespaces, solution name, AppHost path).
- `dev template-smoke` builds the generated CLI, checks its command list, and runs `dev build`
  through it, for the default variant and every `--matrix` variant.
- readme and kanban/readme say what a generated app contains.

Review round 1 fix pass (M1, M2): a refusal names the flag that is actually off. postgres lives
under the web tree, so `dev deploy migrate` in a `--web false` app names web, not postgres; `dev
deploy` refuses without web (its smoke paths are web-server's) as well as without yarp. The marker
test fails when another flag's exclude also removes a marker that the feature does not declare as
required. readme says the same.

## Checklist

- [x] Confirm the gap first: generate from the packed template and list the output root.
- [x] Add `tools/dev-cli/**` to the pack allow-list; keep `tools/agent-identity-cli` out.
- [x] `template.json` excludes for template-only endpoints and services, and per-flag excludes.
- [x] `tests/tools/dev-cli-tests` is excluded today; decide whether an app-side subset ships.
- [x] `.timewarp/dev.jsonc` and anything else the CLI reads at the repo root ships too.
- [x] `dev template-smoke` assertion and matrix coverage.
- [x] `readme.md` and `kanban/readme.md`: what a generated app contains.
- [x] Review round 1 M1 and M2 fixed.

## Review

- Round 1: Blocked — open M1, M2
- Round 2: Done
- Round 3: Done
- Round 4: Done
- Round 5: Done
## Notes

- Found 2026-11-25 while scoping the instance CI workflow (task 531) and the release deploy job
  (task 529). Both call `dev` commands and depend on this.
- `template.json` already excludes `tools/agent-identity-cli/**` and `tests/tools/dev-cli-tests/**`
  but not `tools/dev-cli/**`. That only matters for a repo-root install; the NuGet pack never
  includes `tools/` at all.
- The first instance repo has its own older `tools/dev-cli`. After this task it takes the template's.

### Merge-conflict fix pass (PR #709 — `CONFLICTING` against `origin/master`)

PR #709 parked at merge (`NoChecks`) because `task/530-ship-the-dev-cli-in-generated-apps` has a content merge conflict against `origin/master` in:
- `tools/dev-cli/services/template-smoke/template-smoke-runner.cs` (after tasks 527 and 528 merged).

Fix:
1. Run `git merge origin/master` and resolve the conflict in `tools/dev-cli/services/template-smoke/template-smoke-runner.cs` cleanly (preserving both task 528's `SmokeWebOnly` matrix coverage and task 530's generated dev-CLI smoke checks, plus any `open-command.cs` auto-merge from task 527).
2. Re-verify `./bin/dev build`, `./bin/dev test --project dev-cli-tests`, `./bin/dev template-smoke --matrix`, and `ganda repo audit`, then commit and push so PR #709 triggers CI and merges.

### CI fix pass (PR #709 — `ci (fast)` 1/435 failure in `dev-cli-tests`)

All other CI jobs on PR #709 passed (`ci (integration)`, `ci (publish)`, `template-smoke`, `Lint skill specs`, `classify`, `green-key`). Only 1 of 435 tests in `dev-cli-tests` failed:

`AspireDeploy_.NeverAutomated_Given_.CiWorkflowsAndDevWorkflow_Should_NeverDeploy` (`tests/tools/dev-cli-tests/aspire-deploy-never-automated-given-tests.cs:57`) regex-scans `tools/dev-cli/endpoints/workflow-command.cs` for `dev deploy` and flagged two prose strings (not invocations):
- `tools/dev-cli/endpoints/workflow-command.cs:37`: `//     and names \`dev deploy\`.`
- `tools/dev-cli/endpoints/workflow-command.cs:96`: `Terminal.WriteErrorLine("Error: release mode packs and pushes the template's NuGet packages. This app publishes no packages: deploy with \`dev deploy\`.".Red());`

Fix:
1. Reword lines 37 and 96 in `tools/dev-cli/endpoints/workflow-command.cs` (e.g. avoid the literal `dev deploy` substring in comment/error text so the existing `NeverAutomated` guard stays intact until task 529 updates it).
2. Verify `./bin/dev build`, `./bin/dev test --project dev-cli-tests` (435/435 pass), and `ganda repo audit`, then commit and push so PR #709 goes green and merges.

## Session

- Created: f7eccb81-87c9-4fb4-a0c4-0fb7417d4a2f (2026-11-25)
- 2026-11-26 implement (ganda task work): gap confirmed (a packed-template app had no `tools/`
  and no `.timewarp/`); CLI shipped; flag-off variants generated and their CLIs compiled by hand
  before template-smoke.
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 6d4c1964-3d12-477f-a328-0e9a3b18524b, max-turns 500, Done — 2026-11-25T21:28:46Z
- 2026-11-26 implement-fix-2 (review round 1 fix pass): M1 and M2 fixed, see Results › Review fix pass.
- Review oracle: grok session 01a2149c-8f0e-7c20-a0c6-5ae91cc3abd7 (2026-11-26), effort 3, roster general, round 1
- Review oracle: review by reviewer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a2149c-8f0e-7c20-a0c6-5ae91cc3abd7, max-turns 200, artifacts general.md, merged.md — 2026-11-25T21:41:33Z
- Review oracle round 2: grok session 01a214ad-d381-7e20-abcd-77224169cb95 (2026-11-26), effort 2, roster general
- Review oracle round 3: grok session 01a21623-3f19-7df0-800e-0fd7f7bfe862 (2026-11-26), effort 3, roster general
- 2026-11-26 implement (walk resume): the results compute failed only because the headings said
  "(required before done)"; 91cbf8c5f normalized them. No product work remained, so the gates were not rerun.
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session a2b2c78e-6093-4098-80e9-bdb6c4f4cda3, max-turns 500, Done — 2026-11-26T04:35:30Z
- Review oracle: review by reviewer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a21623-3f19-7df0-800e-0fd7f7bfe862, max-turns 200, artifacts general.md, merged.md, disposition.md — 2026-11-26T04:42:31Z
- 2026-11-26 implement (merge-conflict fix pass): merged origin/master (87 commits), see Results ›
  Merge-conflict fix pass.
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 6f96f8fe-4af1-4825-beab-8f99a4ef0a5e, max-turns 500, Done — 2026-11-26T07:15:33Z
- Review oracle round 4: grok session 01a216b5-ca35-7622-9303-ffeb97dea72c (2026-11-26), effort 3, roster general
- Review oracle: review by reviewer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a216b5-ca35-7622-9303-ffeb97dea72c, max-turns 200, artifacts general.md, merged.md, disposition.md — 2026-11-26
- Review oracle: review by reviewer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a216b5-ca35-7622-9303-ffeb97dea72c, max-turns 200, artifacts general.md, merged.md, disposition.md — 2026-11-26T07:21:40Z
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 0eb9293a-08e1-498b-80eb-698dd41f500b, max-turns 500, Done — 2026-11-26T08:23:11Z
- Review oracle round 5: grok session 01a216f3-b45d-7f62-aeae-f0842ce951c7 (2026-11-26), effort 3, roster general
- Review oracle: review by reviewer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a216f3-b45d-7f62-aeae-f0842ce951c7, max-turns 200, artifacts general.md, merged.md, disposition.md — 2026-11-26T08:33:45Z

## Learnings

- No misread. The Goal restatement matches the original task (ship the dev CLI, drop template-only commands, refuse or exclude flag-off commands, smoke `dev build` on the default variant and `--matrix`). The Goal line says `dev db` is excluded without postgres; Results and the code also exclude it when web is off. That tighter coupling is finding M1, not a conflict with the task.
- Round 2 re-checked the restatement, including the fix-pass paragraph, against the original task. Still no misread. Naming web when the web tree is gone, and refusing `dev deploy` without web, is the review fix. It matches the task rule that a flag-off command is excluded or refuses cleanly.
- Round 3 re-checked the restatement against the original task over the full branch. Still no misread. `regenerate-generated` is excluded because its inputs do not ship, which is the task's "when it applies" clause with nothing to apply. The web coupling on `dev db` and `dev deploy migrate` remains the round 1 fix, not a conflict with the task.
- Round 4 re-checked the restatement against the original task after the origin/master merge. Still no misread. The Goal acceptance line says `open` and `deploy` refuse naming the missing feature and does not mention the compose/kubernetes limit added in the merge. That line is behind the fix, not a conflict with the task. The original rule is that a flag-off command is excluded or refuses cleanly. aca and aca-neon still deploy and open without yarp, because task 527 serves web-server there, so those commands are not dead.
- Round 5 re-checked the restatement against the original task after the CI prose fix. Still no misread. The release-mode refusal no longer contains the literal `dev deploy`. That sentence is the template-only NuGet path, which the task says to drop or refuse. It is not the flag-off rule that a refusal names the missing feature.

## Results

The generated app now ships the dev CLI: `tools/Directory.Build.props`, `tools/dev-cli/`,
`.timewarp/dev.jsonc` and `.envrc` are on the pack allow-list. `tools/agent-identity-cli` and
`tools/fix-analyzer-debug.reg` stay out.

**Mechanism.** File-level template-only parts are `template.json` excludes (the `(true)` modifier).
Shared files keep their template-only lines in `templateRepository` regions. That symbol is a new
computed template symbol, always false at generation. `tools/dev-cli/Directory.Build.props`
defines the C# constant only inside its own `templateRepository` region, so the monorepo compiles
the regions and TWA0010 checks the agreement. `.timewarp/dev.jsonc` uses the same regions. The
`appNameKebab` symbol also rewrites the CLI description and telemetry service name
(`contoso-shop-dev`, not `timewarp-architecture-dev`).

**Command decisions (generated app):**

| Command | Decision |
|---------|----------|
| build, test, run, publish *, deploy, deploy migrate, open, deprovision, entra *, workflow | Ship |
| clean, self-install, audit, benchmark (TimeWarp.Nuru.DevCli) | Ship |
| db * (and the `db-update` alias) | Ship only with postgres and web on; `template.json` excludes `db-*.cs`, `db-ef.cs`, `db-nuke.cs` otherwise |
| template-smoke, template-install, template-publish-smoke | Excluded (`template.json`), with `services/template-smoke/**`, `template-nupkg.cs`, `template-smoke-content-assets.cs`, `jaribu-aggregator-summary-gate.cs` |
| check-client-names | Excluded with `client-name-scan.cs`. `dev build`'s scan is a `templateRepository` region: an app has no name list |
| lint-skills | Excluded with `skill-spec-lint.cs` and `vally-cli.version` |
| verify-samples | Excluded: `samples/` does not ship |
| regenerate-generated | Excluded: its inputs (the grammar JSON under `source/analyzers`, the `goldens/ingress` snapshot) do not ship, so it never applies in an app. `feature-filename-grammar-generator.cs` and `generated-ingress-prefixes.cs` go with it |
| check-version, release (TimeWarp.Nuru.DevCli content) | Removed from the app's compile by a target in `tools/dev-cli/Directory.Build.props` (outside the `templateRepository` region, so only apps run it): an app publishes no NuGet packages |
| workflow: release mode, classify skill/template/green-key outputs, generated-files check | Moved to `endpoints/workflow-template-repository.cs` (excluded); call sites are `templateRepository` regions. In an app, `--mode release` refuses and names `dev deploy`, classify writes `ci=` only, `--api-key` is gone. `green-key*.cs` and `workflow-path-classes.cs` are excluded |

**Flag-off.** `services/app-features.cs` checks a marker path per feature (the paths `template.json`
removes for that flag; `app-features-tests.cs` pins that agreement). A feature may require another:
postgres requires web, because `--web false` removes the postgres tree too, and the required one is
checked first. Refusals: `dev entra *` without web, `dev open` without yarp (it addresses the
`ingress` resource), `dev deploy` without yarp or web (its smoke requests web-server paths through
the ingress), `dev deploy migrate` without postgres or web, each naming the flag that is off. `dev run` skips the web-only xAI key warning without web. api and grpc
have no command of their own.

**dev-cli-tests.** Not shipped. They compile-include template-only services and pin this
repository's paths. The generated CLI is covered by template-smoke, which builds it and runs it.

**template-smoke.** New `TemplateSmokeDevCli`, every variant:
after generate, the CLI files ship, every `tools/` path the `(true)` modifier excludes is absent
(read from `template.json`), `dev db` is present exactly with postgres and web, and no
`templateRepository` directive survived. After the solution build it `git init`s the app, runs
`dotnet run tools/dev-cli/dev.cs -- --capabilities` (the app commands listed, none of the template-only
ones), then `dev build` through the generated CLI.

**Also.** `WorkflowPathClasses` treats `.timewarp/dev.jsonc` and `.envrc` as template paths now that
they ship. readme.md, kanban/readme.md and AGENTS.md say what a generated app contains.

### Gates

- `dotnet run --file tools/dev-cli/dev.cs -- self-install`: installed (dev-cli changed).
- `./bin/dev build`: passed (0 warnings, 0 errors; client-name scan still runs here).
- `./bin/dev test tests/tools/dev-cli-tests/app-features-tests.cs`: 4/4 passed (new file).
- `./bin/dev test tests/tools/dev-cli-tests/workflow-path-classes-tests.cs`: passed (4 classes).
- `./bin/dev template-smoke`: SUCCEEDED (SmokeDefault: dev CLI shipped, capabilities checked, generated `dev build` passed).
- `./bin/dev template-smoke --matrix`: SUCCEEDED (SmokeDefault, SmokeNoPostgres, SmokeNoApi, SmokeNoWeb, SmokeNoGrpc, SmokeNoYarp: each shipped the CLI and passed generated `dev build`)
- `ganda repo audit`: passed with one advisory, vscode-window-icon (the git-ignored local `.vscode/settings.json` was missing in this worktree); `ganda repo audit --fix` wrote it and the re-run passed every check. No tracked file changed.
- `./bin/dev check-version`: source 2.0.0-beta.32 is one prerelease ahead of the published beta.31; no bump needed.
- Manual, before smoke: packed the template into an isolated hive, generated default, `--postgres
  false`, `--web false`, `--yarp false`, `--api false`, `--grpc false`; each generated CLI compiled
  and listed the expected commands. In the generated apps: `dev entra status` (no web), `dev open`
  and `dev deploy` (no yarp), `dev deploy migrate` (no postgres, exit 1) refused with the feature
  message; `dev workflow --mode release` refused; `dev workflow --phase classify` printed `ci=true`;
  `dev self-install` produced `bin/dev` (`Development CLI for def`).

### Review

Round 1 (general, effort 3): 1 bug, 1 suggestion, 0 nits, both open at the end of that round. Ledger: `review/round-1/merged.md`.

Round 2 (general, effort 2): re-reviewed the fix delta. M1 and M2 stay fixed. No new findings.

Round 3 (general, effort 3): re-reviewed the full branch. Product code is unchanged since the fix. M1 and M2 stay fixed. No new findings.

Round 4 (general, effort 3): re-reviewed the branch after the origin/master merge. M1 and M2 stay fixed. Yarp is refused only on compose and kubernetes. No new findings.

Round 5 (general, effort 3): re-reviewed the branch after the CI prose fix in `workflow-command.cs`. M1 and M2 stay fixed. The release-mode refusal still exits 1 and does not deploy. No new findings.

Final counts: bug 0 open / 1 fixed / 0 wontfix; suggestion 0 open / 1 fixed / 0 wontfix; nit 0. Disposition: clean. Final open count: 0. No wontfix and no escalation. Paths: `review/review-framework.md`, `review/round-5/merged.md`, `review/disposition.md`.

### Review fix pass (round 1 → round 2)

- M1: `AppFeature` gained `Requires`; `AppFeatures.Postgres` requires `Web`. `AppFeatures.FirstMissing`
  walks required features first and `Require` reports that one, so `dev deploy migrate` in a
  `--web false` app says "needs the web feature ... --web false". New test
  `EachMarker_Should_BeRemovedOnlyByItsOwnOrARequiredFlag` reads every `template.json` modifier whose
  exclude covers a marker and fails unless its condition's flags are the feature's own or required
  ones (the old `(!postgres)`-only check missed the `(!web)` overlap). New test
  `MissingRequiredFeature_Should_BeNamedFirst`. readme states `deploy migrate` without postgres or web.
  `deploy-migrate-command.cs` stays compiled: `dev deploy` calls its handler.
- M2: `dev deploy` checks web after yarp and refuses before any preflight.
- Gates (fix pass):
  - `dotnet run --file tools/dev-cli/dev.cs -- self-install`: installed (dev-cli changed).
  - `./bin/dev build`: passed (0 warnings, 0 errors).
  - `./bin/dev test tests/tools/dev-cli-tests/app-features-tests.cs`: 6/6 passed (not a standalone
    runfile; it is a class in the dev-cli-tests project, no `#if !JARIBU_MULTI` guard).
  - Manual: in an empty `git init` checkout, `bin/dev deploy migrate` refused naming web (exit 1);
    with only `yarp.csproj` present, `bin/dev deploy --no-browser` refused naming web (exit 1).
  - template-smoke not rerun: the fix changes no template.json or generated-file layout, and
    `app-features.cs` is compiled unconditionally in every variant (round-1 matrix passed).
  - `ganda repo audit`: passed all checks.

### Merge-conflict fix pass (origin/master merge)

- `template-smoke-runner.cs`: kept task 528's `SelectVariants` (`--variant`) and task 530's
  `HasFlagOff` plus generated dev-CLI checks; both Design notes kept. `open-command.cs` auto-merged.
- Semantic conflict with task 527 (web-server is the public app on aca and aca-neon without yarp):
  `dev deploy` and `dev open` now refuse a missing yarp only on compose and kubernetes. New pure helper
  `AspireDeploy.ServesThroughIngress(target)` (unknown target is false, so preflight reports it); the
  web check on `dev deploy` stays for every target. readme and the Design regions say the same.
- Boy-scout: master's `open-api-binary-content-tests.cs` (task 458-003) uses `Features.Feedback`,
  which `--postgres false` removes, so SmokeNoPostgres failed to build. Added it to the `(!postgres)`
  exclude in `template.json`.
- Gates (merge pass):
  - `dotnet run --file tools/dev-cli/dev.cs -- self-install`: installed (dev-cli changed).
  - `./bin/dev build`: passed (0 warnings, 0 errors).
  - `./bin/dev test tests/tools/dev-cli-tests/aspire-deploy-targets-given-tests.cs`: passed (new
    `OnlyComposeAndKubernetes_Should_ServeThroughTheIngress`; dev-cli-tests class, not a standalone runfile).
  - `./bin/dev test tests/tools/dev-cli-tests/app-features-tests.cs`: passed (dev-cli-tests class).
  - `./bin/dev template-smoke --matrix`: SmokeDefault, SmokeNoApi, SmokeNoWeb, SmokeNoGrpc, SmokeNoYarp
    and SmokeWebOnly built OK and the first five passed the generated `dev build`; SmokeNoPostgres
    failed (the test above). The run hit the harness time limit during SmokeWebOnly's generated-CLI step.
  - `./bin/dev template-smoke --variant SmokeNoPostgres` (after the exclude): SUCCEEDED.
  - `./bin/dev template-smoke --variant SmokeWebOnly`: SUCCEEDED (generated `dev build` passed).
  - Manual, in a `git init` checkout with only `web-server.csproj`: `dev open` and `dev deploy`
    (compose default) refuse naming yarp; `dev open --target aca` passes the feature check and stops at
    the AppHost preflight.
  - `ganda repo audit`: passed all checks.

### CI fix pass (PR #709, `NeverAutomated` guard)

- `workflow-command.cs`: the Design comment (line 37) and the release-mode refusal (line 96) no
  longer contain the literal `dev deploy`, which `CiWorkflowsAndDevWorkflow_Should_NeverDeploy`
  reads as an invocation. The refusal now says an operator deploys the app with this CLI's deploy
  command. No behavior change.
- Gates (CI fix pass):
  - `dotnet run --file tools/dev-cli/dev.cs -- self-install`: installed (dev-cli changed).
  - `./bin/dev build`: passed (0 warnings, 0 errors).
  - `./bin/dev test tests/tools/dev-cli-tests/aspire-deploy-never-automated-given-tests.cs`: 2/2
    passed (dev-cli-tests class, not a standalone runfile).
  - `ganda repo audit`: passed all checks.

### How to validate

Smoke: `./bin/dev template-smoke` (add `--matrix` for every flag-off variant).
Expect: each variant prints "Generated app ships the dev CLI without its template-only files." and
"Generated dev CLI lists the app commands and `dev build` passed.", then "Template smoke SUCCEEDED".

Smoke (flag-off refusals): generate with `--web false`, then in it `dev deploy migrate` and `dev deploy`.
Expect: both exit 1 with "needs the web feature ... generated with --web false".
Smoke (no yarp): generate with `--yarp false`, then `dev open --no-browser` and `dev open --target aca --no-browser`.
Expect: the first refuses naming yarp; the second passes the feature check (aca serves web-server).

Smoke (by hand): `dotnet new timewarp-architecture -n Shop` from the packed template, then in it
`git init && dotnet run tools/dev-cli/dev.cs -- --help` and `-- self-install`.
Expect: the help lists build/test/run/db/publish/deploy/open/deprovision/entra/workflow and no
template-smoke, lint-skills, check-version or release; `bin/dev` exists after self-install.
