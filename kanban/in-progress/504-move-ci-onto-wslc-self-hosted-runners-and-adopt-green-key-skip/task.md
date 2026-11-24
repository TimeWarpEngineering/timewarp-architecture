# Move CI onto wslc self-hosted runners and adopt green-key skip

## Depends on

- 503

## Description

Move `timewarp-architecture` same-repo CI jobs onto the org `wslc` self-hosted container runners on TWE-001 (`["self-hosted", "twe-001", "wslc"]`, matching `timewarp-nuru`) and adopt `green-key` skip (matching `timewarp-ganda` tasks 469, 470, and 711) so `master` merges and identical trees do not re-run the entire CI pipeline after a green PR.

## Requirements

- **Route same-repo CI jobs to `wslc` runners:** Match `timewarp-nuru/.github/workflows/workflow.yml` (task 539):
  - Same-repo `push` and same-repo `pull_request` jobs (`classify`, `ci-group`, `skill-lint`, `template-smoke`) run on `["self-hosted", "twe-001", "wslc"]` when `vars.CI_HOSTED_ONLY != 'true'`.
  - Fork pull requests, `release`, `workflow_dispatch`, and the lightweight `ci` status aggregator stay on `ubuntu-latest`.
- **NuGet caching on `wslc` vs hosted:** Only use `actions/cache` when `runner.environment == 'github-hosted'` (matching `timewarp-nuru`'s `setup-dotnet` composite action so shared `wslc` volumes do not pollute this repo's Actions cache).
- **Verify container/tooling compatibility on `wslc`:** Confirm `aspire-tests` / Testcontainers / Docker, `dotnet dev-certs`, `Aspire.Cli`, `helm`, and `node`/`vally-cli` work on `wslc`. If any leg (e.g. Docker-dependent `integration` or `template-smoke`) cannot run inside `wslc`, document why and keep only that leg on `ubuntu-latest`.
- **Adopt `green-key` markers and skip:** Follow `timewarp-ganda` tasks 469, 470, and 711:
  - Emit a `green-key` marker artifact after a full same-repo CI pass.
  - Skip redundant rebuild/retest on `master` merge pushes (or unchanged re-runs) whose green key already passed, preserving the `Green-Key: full-run` commit trailer and manual `workflow_dispatch` escape hatches.
- **Audit compliance:** Verify `ganda repo audit` (including `self-hosted-runner`) passes cleanly.

## Goal (implementer restatement)

Two CI changes in this repo.

1. Same-repo push and same-repo pull request runs of `classify`, `ci-group`, `skill-lint`, and
   `template-smoke` go to the org wslc runners (`["self-hosted", "twe-001", "wslc"]`), unless the
   repo variable `CI_HOSTED_ONLY` is `true`. Fork pull requests, `release`, `schedule`,
   `workflow_dispatch`, and the `ci` aggregator stay on `ubuntu-latest`. A leg that needs Docker
   (the image has no Docker socket) stays hosted with the reason written next to it. The
   composite setup action uses the Actions NuGet cache only on GitHub-hosted runners.
2. A green-key ledger like timewarp-ganda 469/470: classify hashes the committed tree (minus
   kanban task content), the SDK, and the runner into a key. After a same-repo run passes a gate
   (ci-group, skill-lint, template-smoke), a marker artifact for that gate and key is uploaded.
   A later same-repo run whose key already has that gate's marker skips the gate, so the master
   merge push (and a kanban-only push to the pull request) does not re-run CI. The head commit
   trailer `Green-Key: full-run` and a manual dispatch force a full run.

Done when: the workflow routes as above, the cache is hosted-only, the ledger logic lives in
`dev workflow` with tests, `./bin/dev build` and the dev-cli tests pass, and `ganda repo audit`
passes.

## Checklist

- [x] Switch same-repo CI jobs in `.github/workflows/workflow.yml` to `["self-hosted", "twe-001", "wslc"]` with `CI_HOSTED_ONLY` fallback and fork-PR safety
- [x] Gate `actions/cache` to `runner.environment == 'github-hosted'` in the composite setup action
- [x] Verify Docker/Aspire/Helm/Node compatibility on `wslc` runners (from the image definition; the first run on this PR is the live proof)
- [x] Implement `green-key` marker upload and skip logic in `dev workflow` + `workflow.yml`
- [x] Run `./bin/dev build`, `dev-cli-tests`, and `ganda repo audit`

## Session

- Created: 2026-11-23 (from Task 500 walk performance review)
- 2026-11-24 implement (ganda task work): wslc routing, hosted-only NuGet cache, green-key ledger.
  Audit boy-scout: `ganda repo audit --fix --checks vscode-window-icon` (local, gitignored
  `.vscode/settings.json`) and `--checks kanban-landed`, which landed live claim 507's committed
  kitchen (two evidence PNGs and task.md) onto origin-home.
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session be194d43-c599-41cb-81e1-9ea44a0d2ac2, max-turns 500, Done — 2026-11-24T04:11:56Z

## Notes

- Reference implementations: `timewarp-nuru/.github/workflows/workflow.yml` (task 539, `wslc` runners) and `timewarp-ganda/.github/workflows/workflow.yml` (tasks 469, 470, 711, `green-key` skip + pack-only master).
- Parent performance review: **500** (`kanban/to-do/500-walk-performance-review-for-tasks-499-483-494-and-498/report/analysis.md`).

## Results

### What changed

- `.github/workflows/workflow.yml`
  - `classify`, `ci-group`, `skill-lint`, `template-smoke` use the nuru 539 `runs-on` expression:
    wslc for a push or a same-repo pull request when `vars.CI_HOSTED_ONLY != 'true'`, else
    `ubuntu-latest`. Fork pull requests, schedule, dispatch, release, `ci`, and `green-key` stay
    hosted.
  - `ci (integration)` has `matrix.hosted: true` and stays on `ubuntu-latest` on every event.
  - `classify` gets `actions: read` and `GH_TOKEN` for the marker lookup, and two new outputs:
    `green_key` and `green_key_writer`.
  - New `green-key` job (ubuntu-latest, `permissions: {}`). It uploads
    `green-key-v1-<gate>-<key>` (90 days) for each of ci-group, skill-lint, and template-smoke
    that succeeded.
  - New dispatch mode `full`: classify, ci-group, skill-lint, and template-smoke on hosted
    runners, with no green-key skip (the manual escape hatch). `release` excludes it.
- `.github/actions/setup-ci/action.yml`: both NuGet cache steps also need
  `runner.environment == 'github-hosted'`.
- `tools/dev-cli/services/green-key.cs` (new, pure): the key manifest (`green-key-v1`, sdk,
  runner, `tree` lines from `git ls-tree -r HEAD` minus kanban task content; `kanban/*.md` and the
  column `.gitkeep` files are packed by the template, so they stay key inputs), the event rules,
  the `Green-Key: full-run` trailer, marker selection, and `ApplyHits`.
- `tools/dev-cli/services/green-key-ledger.cs` (new): runs git, `dotnet --version`, the dpkg
  hash, and `gh api` lookups. Any error counts as a miss, so the gate runs.
- `tools/dev-cli/endpoints/workflow-command.cs`: classify applies the hits and writes the two
  outputs. A `workflow_dispatch` that passes `--phase` runs in Merge mode, not Release.
- `tests/tools/dev-cli-tests/green-key-tests.cs` (new, 14 tests) and a compile-include of
  `green-key.cs` in `dev-cli-tests.csproj`.

### wslc compatibility (from timewarp-infrastructure `runner/Dockerfile` and `runner/README.md`)

- No Docker socket in the wslc image. `ci (integration)` (Testcontainers, aspire-tests) stays
  hosted. Nightly `template-smoke --app-tests` runs on `schedule`, which is hosted already. The
  per-pull-request default smoke (pack, install, generate, build) needs no Docker.
- The image has Node 26, Helm 4.3.1, gh 2.83, and Playwright Chromium. `actions/setup-node`
  installs Node 22 for skill-lint into the tool cache. `/usr/share/dotnet` belongs to `runner`, so
  setup-dotnet can install the `global.json` SDK. `~/.dotnet/tools` is on PATH (Aspire CLI).
  `dotnet dev-certs https --trust` writes only under `$HOME`.
- `ci (publish)` (`aspire publish` plus helm lint) is expected to work. The image added Helm for
  it. Nothing checked here shows that it needs Docker. Its first live run is this PR's CI.
- Fork PR approval policy is `all_external_contributors` (`gh api .../fork-pr-contributor-approval`).
  Runner group 3 already lists this repo.

### Gates

- `./bin/dev build`: first run failed with RCS1118 (a local in the new test should be `const`).
  Fixed. Rerun: Build succeeded, 0 warnings, 0 errors.
- `dotnet run --file tools/dev-cli/dev.cs -- self-install` (dev-cli changed): first run failed
  with IDE1006 (a local const was PascalCase). Fixed. Rerun succeeded.
- `tests/tools/dev-cli-tests/green-key-tests.cs`: not a standalone runfile (it is part of the
  `dev-cli-tests.csproj` project). Ran the Release-built
  `tests/tools/dev-cli-tests/bin/Release/net11.0/dev-cli-tests`: 371 passed, 0 failed. That
  includes the new green-key tests and the existing workflow, vally, and Aspire.Cli checks that
  read `workflow.yml`. CI runs it too.
- `ganda repo audit`: failed `kanban-landed` (live claim 507 drift) and warned
  `vscode-window-icon`. Fixed both with `--fix --checks …` (see Session). Rerun: "Repository
  passes all audit checks", exit 0. `self-hosted-runner` is skipped because the repository is
  public.

### How to validate

Smoke: from this worktree, with `./bin/dev` freshly self-installed:

```bash
T=$(mktemp -d); echo "{\"before\":\"$(git rev-parse HEAD~12)\",\"after\":\"$(git rev-parse HEAD)\"}" > $T/e.json; : > $T/out
GITHUB_EVENT_NAME=push GITHUB_REPOSITORY=TimeWarpEngineering/timewarp-architecture GITHUB_EVENT_PATH=$T/e.json \
  GITHUB_OUTPUT=$T/out RUNNER_ENVIRONMENT=self-hosted ./bin/dev workflow --phase classify; cat $T/out
```

Expect: `green-key-v1 event=push candidate=true force=false key=<64 hex>`, a
`sdk=… runner=self-hosted|dpkg:… tree=…` line, and one `gate=<ci|skill|template> lookup=ok earlier=miss`
line per gate. `$T/out` has `ci=true skill=true template=true green_key=<key> green_key_writer=true`.
With `GITHUB_EVENT_NAME=workflow_dispatch`, the mode is Merge (not Release), `candidate=false`,
every job is on, and `green_key_writer=false`.

On GitHub: this PR's run puts classify, ci (fast), ci (publish), skill-lint, and template-smoke on
`twe-runner-01-*` and ci (integration) on a hosted runner. After it passes, the `green-key` job
uploads `green-key-v1-{ci,skill,template}-<key>`. The master merge push then logs
`earlier=hit trusted-run=<PR run>` and skips ci-group and template-smoke, and `ci` passes.

### Follow-ups / risks

- `ci (publish)` on wslc has not been proven yet. If `aspire publish` turns out to need Docker,
  set `hosted: true` on the publish matrix leg.
- The key hashes the classify job's runner. The always-hosted integration leg's image is not in it.

