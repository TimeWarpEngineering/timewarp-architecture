# Adopt the shared pull request build classifier and the one-workflow CI standard

## Description

Bring timewarp-architecture to the org CI standard (ganda 476 / 479 / 565 / 587, audited by ganda 655): `workflow.yml` has
no `paths` / `paths-ignore` filter on `pull_request`, so every pull request reports `CI`, and `dev workflow`
calls the shared classifier `PullRequestBuildClassifier` from `TimeWarp.Nuru.DevCli` 4.0.0-beta.15
(timewarp-nuru 595) to skip build and test on a pull request that touches no build path (kanban, docs, ...).

Tracked by timewarp-ganda 656 (architecture reposet rollout).

Survey 2026-11-16 for this repo: has a `pull_request` paths filter; `dev workflow` does not classify. Work: remove the filter, add the classifier call.

## Requirements

1. Run `ganda repo audit --fix` first and commit what it changes.
2. Bump the `TimeWarp.Nuru.DevCli` pin to `4.0.0-beta.15` (and `TimeWarp.Nuru` to the same version if pinned).
3. In the `workflow` command's pull-request path, call `PullRequestBuildClassifier.DecideAsync` first. On skip,
   print its message and exit 0. Push and release paths are unchanged.
4. Remove `paths` / `paths-ignore` from the `pull_request` trigger of `.github/workflows/workflow.yml`. Keep any
   `push` filter and add a comment saying why (a kanban-only merge must not rebuild and upload packages).
5. If this repo builds from roots outside the classifier defaults (`source`, `tests`, `tools`, `msbuild`), declare
   them through the classifier's config (`PullRequestBuildConfig`), not by copying or editing the rule.
6. No local copy of the classification rule remains in `tools/dev-cli/`.
7. `ganda repo audit` passes, including both CI-standard checks from ganda 655.

## Goal (implementer restatement)

Every pull request in this repo must report `CI`, and a pull request that only changes non-build
paths (kanban, documentation, root markdown, skills) must finish that run quickly without building.
The decision belongs to the shared `PullRequestBuildClassifier` in `TimeWarp.Nuru.DevCli`
4.0.0-beta.15, not to a regex in `workflow.yml` or a copy in `tools/dev-cli/`.

Acceptance:

- `TimeWarp.Nuru` and `TimeWarp.Nuru.DevCli` pinned at `4.0.0-beta.15`.
- `dev workflow` (pr/merge path) calls `PullRequestBuildClassifier.DecideAsync` first and returns
  exit 0 with its message on skip. Release is unchanged. A `--phase classify` writes the decision to
  `GITHUB_OUTPUT` so the parallel `ci-group` jobs skip on a non-build pull request (the Nuru pattern).
- `pull_request` in `workflow.yml` has no `paths` filter. The `push` filter stays, with a comment
  saying a kanban-only merge must not rebuild.
- The repo's extra build roots (`timewarp-templates`, `.template.config`, `goldens`) and its
  non-build roots (`skills`, `evals`, `.vally.yaml`) are declared in `.timewarp/dev.jsonc`
  `pullRequestBuild`.
- No classification rule copy in `tools/dev-cli/`; `ganda repo audit` passes (including
  `dev-workflow-classifier` and the workflow-file check).

## Checklist

- [x] `ganda repo audit --fix` run and committed (it changed nothing)
- [x] DevCli pin at 4.0.0-beta.15
- [x] `dev workflow` calls the shared classifier on pull requests; skip exits 0 with its message
- [x] No `paths` filter on `pull_request`; `push` filter kept with a why comment
- [x] Extra build roots (if any) declared through config
- [x] `ganda repo audit` clean
- [ ] This task's pull request shows a `CI` run (record the URL in Results)

## Notes


After merge, the ganda 656 tracker proves a kanban-only pull request here reports `CI` success and merges with
plain `ganda pr merge` (no `--allow-no-ci`).

## Session

- Created: 64677b22-d705-4b25-b537-4b53732bcb92 (2026-11-16)

## Results

### What changed

- `Directory.Packages.props`: `TimeWarp.Nuru` and `TimeWarp.Nuru.DevCli` 4.0.0-beta.14 to 4.0.0-beta.15.
- `tools/dev-cli/endpoints/workflow-command.cs`: the pr/merge path calls
  `PullRequestBuildClassifier.DecideAsync(RepoRoot, ct)` first (default `all` phase), prints the message,
  and returns exit 0 on skip. New `--phase classify` runs only that call and writes `ci=true|false` to
  `GITHUB_OUTPUT` (the Nuru `--stage classify` pattern). `--phase test` / `publish` do not classify:
  their CI checkout is shallow and the classify job already gated them. Release is unchanged.
- `.github/workflows/workflow.yml`: `pull_request` has no `paths` filter. `push` keeps its filter with a
  comment (a kanban-only merge must not rebuild). `detect-paths` on a pull request checks out full
  history, sets up .NET, restores the NuGet cache read-only, runs `dev workflow --phase classify`, and
  its `ci` output is the classifier's answer (push still uses the regex). `skill` and `template` gates
  are unchanged. The `ci` job now also needs `detect-paths` and fails when it did not succeed, so a
  failed classify step cannot read as a skipped (passing) ci-group.
- `.timewarp/dev.jsonc` `pullRequestBuild`: `buildPaths` `timewarp-templates/**`, `.template.config/**`,
  `goldens/**` (markdown there builds too); `nonBuildPaths` `skills/**`, `evals/**`, `.vally.yaml`
  (the skill-lint job gates those, as before).
- No classification rule in `tools/dev-cli/` (the call is the shared DevCli content).

### How to validate

Smoke:

```bash
./bin/dev self-install   # or: dotnet run --file tools/dev-cli/dev.cs -- self-install
B=$(git rev-parse HEAD~1); H=$(git rev-parse HEAD)
printf '{"pull_request":{"base":{"sha":"%s"},"head":{"sha":"%s"}}}' $B $H > /tmp/ev.json
GITHUB_EVENT_NAME=pull_request GITHUB_EVENT_PATH=/tmp/ev.json GITHUB_OUTPUT=/tmp/out ./bin/dev workflow --phase classify
cat /tmp/out
```

Expect: for a base..head diff of only kanban/docs/skills paths, the line "Pull request changes only
declared non-build paths. Skipping build and test. ..." with exit 0 and `ci=false` in `/tmp/out`;
any `source/`, `tools/`, `.github/workflows/`, `timewarp-templates/` (markdown included), or
`global.json` change prints "Build path changed: <path>. Full build." and writes `ci=true`. Without
`GITHUB_EVENT_NAME` the message is "No GITHUB_EVENT_NAME (local run). Full build."

Verified locally with synthetic commits (git commit-tree, no branch moved):
`skills/tw-kanban/evals/eval.yaml`, `.vally.yaml`, `evals/x.json`, `readme.md`, `documentation/x.txt`
gave `ci=false`; `timewarp-templates/readme.md`, `goldens/ingress/x.md`, `.template.config/x.md`,
`global.json` gave `ci=true`. A kanban-only diff gave `ci=false`, and the full `dev workflow` under
the same event skipped with exit 0. `--phase bogus` exits 1 naming classify, test, publish.

### Gates

| Gate | Command | Result |
|------|---------|--------|
| build | `./bin/dev build` | passed, 0 warnings, 0 errors |
| test | `./bin/dev test` | passed, 23 of 23 projects |
| audit | `ganda repo audit --fix` | passes; `--fix` changed nothing. One advisory warning, `source-file-line-cap` (severity warning since 469-001): three EF migration designer/snapshot files and one done-task research runfile. Not changed here. |

### Follow-ups

- The checklist item "This task's pull request shows a `CI` run" is filled when the host opens the PR.

