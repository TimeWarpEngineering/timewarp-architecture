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

## Checklist

- [ ] Register template-only `ArchitecturePackableProjectService` (`IPackableProjectService`) covering all 12 packages (`source/` + `TimeWarp.Architecture`) and clean `.timewarp/dev.jsonc`
- [ ] Pack all 12 packages on `master` push (including green-key / `skills/**` carry-forward or `pack-only`), publish `Packages-${{ github.run_number }}` in `ci`, prune keeping newest 2, and keep PR runs free of `Packages-*`
- [ ] Implement the canonical 6-step DevCli promotion pipeline (`tag-gate` + attestation -> `check-version` -> `locate-run` -> `download-artifact` -> `verify` -> `push`) followed by `template-publish-smoke`, plus `workflow.yml` permissions/checkout/break-glass updates
- [ ] Update `documentation/developer/guides/releasing.md` and `.template.config/template.json` excludes as needed
- [ ] Add/update `tests/tools/dev-cli-tests/` and verify `./bin/dev build`, `./bin/dev test --project dev-cli-tests`, `./bin/dev template-smoke`, `./bin/dev check-version`, and `ganda repo audit`

## Session

- Created: 2026-11-27

## Notes

- Depends on: none (ordered after task 532 so task 533's merge to `master` produces the live `Packages-*` artifact ready for `dev release` of `v2.0.0-beta.32`).
- Do **not** bump `<Version>` in `source/Directory.Build.props` (already `2.0.0-beta.32`).
- Keep `dev-cli` self-installed (`./bin/dev` rebuilt) after editing `tools/dev-cli/`.
