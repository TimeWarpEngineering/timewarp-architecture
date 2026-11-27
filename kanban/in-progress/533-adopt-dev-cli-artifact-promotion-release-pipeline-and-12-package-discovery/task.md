# Adopt DevCli artifact-promotion release pipeline and 12-package discovery

## Description

`timewarp-architecture` currently diverges from the org-wide TimeWarp DevCli release methodology (`tw-release`, `TimeWarp.Nuru.DevCli 4.0.0-beta.17`, and sibling repos `timewarp-nuru`, `timewarp-ganda`, `timewarp-contracts`):

1. `push` to `master` never packs `.nupkg`s or uploads `Packages-${{ github.run_number }}`; instead, the `release` job rebuilds and packs fresh on `release:published`. Because `TimeWarp.Nuru.DevCli 4.0.0-beta.17`'s `dev release` guard (`release-command.guards.cs`, Nuru task 568) requires a live `Packages-*` artifact on the `master` push CI run, `./bin/dev release --dry-run` now refuses to cut a release.
2. When a PR merges to `master`, `green-key` skips `ci-group` (and `skills/**`-only pushes set `ci=false` even though `skills/**` ships inside the `TimeWarp.Architecture` template package), leaving `master` with no `Packages-*` artifact.
3. The `release` job omits the canonical 6-step DevCli promotion pipeline (`tag-gate` including `refs/notes/ganda-audit` attestation verification, `check-version`, `locate-run`, `download-artifact`, `verify`, `push`), `fetch-depth: 0`, `permissions: actions: read`, `GH_TOKEN`, and break-glass `workflow_dispatch` confirmation (`mode: release`, `confirm: release`).
4. DevCli's default `PackableProjectService` only scans `source/` (11 packages) and misses `timewarp-templates/source/timewarp-architecture-template/timewarp-architecture-template.csproj` (`TimeWarp.Architecture`), so `dev check-version` and `dev release` silently omit the template package (and `.timewarp/dev.jsonc` carries an obsolete `"checkVersionStrategy": "git-tag"` key).

Align `timewarp-architecture` with the standard DevCli artifact-promotion release pipeline used across TimeWarp repos, while keeping `timewarp-architecture`'s post-push `template-publish-smoke` gate.

## Reference implementations

Read the canonical sibling workflows and DevCli services before editing:
- `/home/steve/worktrees/github.com/TimeWarpEngineering/timewarp-nuru/master/.github/workflows/workflow.yml` and its `tools/dev-cli/endpoints/workflow-command*.cs`
- `/home/steve/worktrees/github.com/TimeWarpEngineering/timewarp-ganda/master/.github/workflows/workflow.yml` and its `tools/dev-cli/endpoints/` release/green-key pack-only / carry-forward code (tasks 470 & 711)
- `TimeWarp.Nuru.DevCli 4.0.0-beta.17` shipped services (`CiRunPromotion`, `TagAssertion`, `AttestationVerifyRunner`, `AttestationConfigResolver`, `CiModeDetector`, `IPackableProjectService`)

## Scope

1. **12-package discovery (`IPackableProjectService`):**
   - Add a template-only `ArchitecturePackableProjectService` (`#if templateRepository` / excluded by `.template.config/template.json`) registered for `IPackableProjectService` in `tools/dev-cli/dev.cs`, combining `PackableProjectService` (`source/`) with `timewarp-templates/source/timewarp-architecture-template/timewarp-architecture-template.csproj` (`TimeWarp.Architecture`) via MSBuild `-getProperty:IsPackable,PackageId`.
   - Ensure `dev check-version`, `dev release`, `pack`, `verify`, and `push` all share this one 12-package source of truth (remove duplicate hardcoded `PackableProjects` lists where superseded).
   - Remove obsolete `"checkVersionStrategy": "git-tag"` from `.timewarp/dev.jsonc`.
2. **`master` push packing & `Packages-*` upload (`.github/workflows/workflow.yml` + `tools/dev-cli/`):**
   - On `push` to `refs/heads/master`, pack all 12 Release-built packages into `artifacts/packages/*.nupkg`, verify the set via `CiRunPromotion.VerifyPackageSet`, and upload `staged-packages` (`if-no-files-found: error`, `retention-days: 3`). PR runs must **not** upload `Packages-*`.
   - In the final `ci` aggregator job (`permissions: contents: read, actions: write`), when `github.ref == 'refs/heads/master'` (and not PR/release/probe), download `staged-packages`, upload `Packages-${{ github.run_number }}` (`if-no-files-found: error`, `retention-days: 3` to respect the org 500 MB Free-plan cap — or match `releasing.md`), prune older `Packages-*` keeping the newest 2, and delete `staged-packages`.
   - Handle `master` push skips cleanly (`green-key` hit or `skills/**`-only push where `ci=false`): carry forward a live verified `Packages-*` from an earlier non-PR run with the same key when available, otherwise run a lightweight `pack-only` (`build -> pack`) job so every shippable `master` commit produces `Packages-${{ github.run_number }}` (and do not write a `green-key-v1-ci-*` marker from a `pack-only` run).
3. **Canonical 6-step `release` promotion + `template-publish-smoke`:**
   - Update `.github/workflows/workflow.yml` `release` job:
     - `workflow_dispatch` inputs: add `release` to `mode` and add `confirm` string input (`default: ''`); add break-glass confirmation step (`inputs.mode == 'release' && inputs.confirm != 'release'`).
     - Gate `release` job to `github.event_name == 'release' || (github.event_name == 'workflow_dispatch' && (inputs.mode == 'release' || inputs.mode == 'probe'))`.
     - Set `permissions: contents: read, id-token: write, actions: read`, `checkout` with `fetch-depth: 0`, `setup-ci` with `nuget-cache: restore` and `aspire-cli: 'true'` (needed by `template-publish-smoke`), and pass `GH_TOKEN: ${{ github.token }}` + `--api-key` (and `--mode release` on break-glass dispatch).
     - Remove `Upload Artifacts` from the `release` job (it promotes the merge run's `Packages-*`).
   - Update `tools/dev-cli/endpoints/workflow-command.cs` and `workflow-template-repository.cs`:
     - Add `--attestation <off|warn|require>` option (default `Warn` on PR/merge, `Require` on release via `AttestationConfigResolver`) and run `AttestationVerifyRunner.VerifyAsync` in PR/merge and release `tag-gate`.
     - Replace `RunReleaseAsync` with the canonical 6-step promotion pipeline:
       `tag-gate` (`TagAssertion`, tag-pin check, ancestor-of-`origin/master` check, `AttestationVerifyRunner`) -> `check-version` -> `locate-run` (`CiRunPromotion.OrderCandidateRuns`) -> `download-artifact` (`CiRunPromotion.SelectPackagesArtifact` + `gh run download`) -> `verify` (`CiRunPromotion.VerifyPackageSet`) -> `push` (`dotnet nuget push --skip-duplicate` per package) -> **`TemplatePublishSmokeCommand`**.
4. **Docs, template isolation, and tests:**
   - Keep `.template.config/template.json` excludes / `#if templateRepository` regions clean so generated apps never receive template-only files or release services.
   - Update `documentation/developer/guides/releasing.md` to document the promotion flow, `master` `Packages-*` upload (including green-key `pack-only`/carry-forward & keep-last-2 pruning), attestation verification, and break-glass recovery.
   - Add/update tests in `tests/tools/dev-cli-tests/` covering `ArchitecturePackableProjectService` (all 12 packages), `master` `pack-only`/green-key behavior, and keep `app-ci-workflow-shape-tests.cs` green.
   - Self-install `./bin/dev` and verify `./bin/dev build`, `./bin/dev test --project dev-cli-tests`, `./bin/dev template-smoke`, `./bin/dev check-version`, and `ganda repo audit`.

## Goal (implementer restatement)

Make this repo release the way the other TimeWarp repos do: build and test once on the master
merge, then promote those exact packages on `release:published`.

Acceptance:

- One packable set of 12 packages (the 11 under `source/` plus `TimeWarp.Architecture`) feeds
  `dev check-version`, `dev release`, the master pack, and the release verify and push. No
  hand-kept project list. `.timewarp/dev.jsonc` has no `checkVersionStrategy`.
- Every master push that CI accepts ends with a `Packages-<run_number>` artifact (3-day
  retention, the two newest kept): from the `ci (publish)` leg when ci-group runs, or from a
  `pack-only` job (carry-forward of a live same-key non-PR run first, else build and pack) when
  ci-group is skipped by green key or a skills-only push. A pull request uploads no `Packages-*`.
  A pack-only run writes no ci green-key marker.
- The `release` job runs the 6 steps: tag-gate (tag assertion, tag pin, ancestor of
  origin/master, attestation; default require) -> check-version -> locate-run ->
  download-artifact -> verify -> push, then template-publish-smoke. It no longer builds or uploads.
  It has `fetch-depth: 0`, `actions: read`, `GH_TOKEN`, and a break-glass dispatch
  (`mode: release` + `confirm: release`).
- `dev workflow --attestation off|warn|require`; the PR/merge classify verifies attestation
  (default warn).
- Generated apps get none of the template-only release code.
- `releasing.md` describes the new flow; dev-cli-tests cover the 12-package set, the master
  pack-only decision, and the workflow shape.

## Checklist

- [x] Register template-only `ArchitecturePackableProjectService` (`IPackableProjectService`) covering all 12 packages (`source/` + `TimeWarp.Architecture`) and clean `.timewarp/dev.jsonc`
- [x] Pack all 12 packages on `master` push (including green-key / `skills/**` carry-forward or `pack-only`), publish `Packages-${{ github.run_number }}` in `ci`, prune keeping newest 2, and keep PR runs free of `Packages-*`
- [x] Implement the canonical 6-step DevCli promotion pipeline (`tag-gate` + attestation -> `check-version` -> `locate-run` -> `download-artifact` -> `verify` -> `push`) followed by `template-publish-smoke`, plus `workflow.yml` permissions/checkout/break-glass updates
- [x] Update `documentation/developer/guides/releasing.md` and `.template.config/template.json` excludes as needed
- [x] Add/update `tests/tools/dev-cli-tests/` and verify `./bin/dev build`, `./bin/dev test --project dev-cli-tests`, `./bin/dev template-smoke`, `./bin/dev check-version`, and `ganda repo audit`

## Session

- Created: 2026-11-27
- 2026-11-27 implement (claude): packable set, master Packages-*, 6-step release, docs, tests. Dispatch
  mode `merge` (pack without publish through the release job) is gone: the release job no longer packs.
  `full` is now the default dispatch; `release` + `confirm` is break-glass.

## Notes

- Depends on: none (ordered after task 532 so task 533's merge to `master` produces the live `Packages-*` artifact ready for `dev release` of `v2.0.0-beta.32`).
- Do **not** bump `<Version>` in `source/Directory.Build.props` (already `2.0.0-beta.32`).
- Keep `dev-cli` self-installed (`./bin/dev` rebuilt) after editing `tools/dev-cli/`.

## Results

### What changed

- `tools/dev-cli/services/release-packages.cs` (pure, template-only): candidate projects
  (`source/**/*.csproj` minus bin/obj, plus the template pack), the master `packages`/`pack`/`carry_run`
  decision, and the carry-forward pick.
- `tools/dev-cli/services/architecture-packable-project-service.cs` (template-only):
  `IPackableProjectService` over those candidates through DevCli `PackableProjectService.EvaluateProjectsAsync`.
  `dev.cs` registers it in a `templateRepository` region; generated apps keep `PackableProjectService`.
  The hand-kept `PackableProjects` list in `workflow-template-repository.cs` is gone.
- `.timewarp/dev.jsonc`: `checkVersionConfig.checkVersionStrategy` removed.
- `tools/dev-cli/endpoints/workflow-template-packages.cs` (template-only): pack + `CiRunPromotion.VerifyPackageSet`,
  `--phase pack` (carry forward via `--carry-run`, else clean -> build -> pack), the classify carry lookup
  over `green-key-v1-ci-<key>` marker runs.
- `tools/dev-cli/endpoints/workflow-template-release.cs` (template-only): tag-gate (TagAssertion, tag pin,
  ancestor of origin/master, attestation require) -> check-version -> locate-run -> download-artifact ->
  verify -> push (`--skip-duplicate` per package) -> template-publish-smoke.
- `workflow-command.cs`: `--attestation`, `--pack` (with `--phase publish`), `--carry-run` (with
  `--phase pack`), phase `pack`; all in `templateRepository` regions. Classify verifies attestation
  (default warn) and writes `packages`, `pack`, `carry_run`.
- `.github/workflows/workflow.yml`: `ci (publish)` packs with `--pack` and stages `staged-packages` on
  master; new `pack-only` job; `ci` (actions: write) downloads staged-packages, uploads
  `Packages-<run_number>` (3 days, nupkg + snupkg), keeps the two newest `Packages-*`, deletes
  staged-packages; `release` job: fetch-depth 0, `actions: read`, `GH_TOKEN`, break-glass confirm,
  no build and no upload. Dispatch modes: full (default), release, probe, nightly; input `confirm`.
- `.template.config/template.json` excludes the four new template-only files.
- `documentation/developer/guides/releasing.md` rewritten for the promotion flow.
- `tests/tools/dev-cli-tests/release-packages-tests.cs` (13 tests): candidates, the real 12-package
  MSBuild evaluation, master pack-only/carry decisions, workflow shape (release promotes without upload,
  only `ci` uploads `Packages-*`, pack-only writes no green-key marker).

### Gates

- `./bin/dev build` — passed (0 warnings, 0 errors).
- `dotnet run --file tools/dev-cli/dev.cs -- self-install` — passed (dev CLI compiles AOT, no warnings).
- `tests/tools/dev-cli-tests/release-packages-tests.cs` — not a standalone runfile (no `#if !JARIBU_MULTI`
  guard; project-compiled). Ran `./bin/dev test --project dev-cli-tests`: passed 467/467 (13 new in
  `ReleasePackages_`). CI runs it too.
- `./bin/dev template-smoke` — SUCCEEDED (generated dev CLI compiles without the template-only files;
  web 504/504, api 9/9, testing 18/18).
- `./bin/dev check-version` — 12 packages checked (TimeWarp.402 … TimeWarp.Architecture … TimeWarp.Modules),
  2.0.0-beta.32 new vs 2.0.0-beta.31.
- `./bin/dev workflow --phase pack` (local) — 12 packages packed and verified at 2.0.0-beta.32 (3m10s).
- `ganda repo audit` — pass: 43 passed, 0 failed, 1 warning (`vscode-window-icon`, existing).

### How to validate

Smoke:

```bash
./bin/dev check-version
./bin/dev workflow --phase classify      # local: attestation line, packages=false pack=false carry_run=
./bin/dev workflow --mode release        # on a task branch: aborts at step 1 (not an ancestor of origin/master)
./bin/dev workflow --phase pack          # clean -> build -> pack -> verify 12 packages
./bin/dev test --project dev-cli-tests --filter-class ReleasePackages_
```

Expect: check-version lists 12 packages including TimeWarp.Architecture; classify prints
`packages=false`; release aborts with "HEAD is not an ancestor of origin/master"; pack ends
"Package set verified: 12 packages at 2.0.0-beta.32"; 13/13 tests pass. After merge, the master push
run of this task's merge commit (or its pack-only job on a green-key hit) shows a
`Packages-<run_number>` artifact, and `./bin/dev release --dry-run` from synced master finds it.

### Risks / follow-ups

- The locate-run, download, and push steps and the `ci` job's upload, prune, and staged-packages delete
  only run on GitHub; the first master run after merge and the `v2.0.0-beta.32` release exercise them.
- A master commit with no workflow run (paths filter, e.g. documentation-only) gets no `Packages-*`;
  remedy documented (dispatch `mode: full` on master).
