# Bring master back to a clean ganda repo audit (beta.29 checks)

## Description

`ganda repo audit` on architecture master fails 5 checks (ganda 1.0.0-beta.29). None were
introduced by a recent PR; they are ganda audit checks added or tightened after the files
landed. The tw-pr gate says audit is blocking, so every new PR from a fresh worktree now
starts red. Fix them once here.

Findings on master, 2026-09-21:

| Check | Severity | What |
|---|---|---|
| `runfile-executable` | Error | 26 shebang runfiles without the executable bit — all co-located `*-tests.cs` under `source/container-apps/{web,api}/…` plus `kanban/done/238-…/research/run-rank-experiment.cs` |
| `runfile-shebang` | Error | `kanban/done/238-…/research/run-rank-experiment.cs` uses `#!/usr/bin/dotnet --` (non-standard) |
| `global-usings-analyzer` | Error | `.editorconfig` is missing `dotnet_diagnostic.TW0007.filename = global-usings.cs` (ganda check added 2026-09-20: fails when the SourceGenerators GlobalUsingsAnalyzer is missing or PascalCase) |
| `memsearch-scaffold` | Warning | `.githooks/pre-commit` and `.githooks/pre-push` missing |
| `vscode-window-icon` | Warning | `.vscode/settings.json` does not set `peacock.color` |

Not in scope: `kebab-path-names` fired only on an untracked, empty leftover
`TimeWarp.Architecture/Source/**` directory tree in the operator's master checkout (0 tracked
files); removed locally, nothing to change in the repo.

## Requirements

- Prefer `ganda repo audit --fix` (or `--fix --checks <id>`) for every check that offers a
  fixer; hand-edit only what it does not fix. Re-run `ganda repo audit` until it exits 0.
- `runfile-executable`: set the executable bit on all 26 (`git update-index --chmod=+x` so the
  mode is committed, not just the working tree). Then add a guard so new co-located
  `*-tests.cs` runfiles cannot land without it — check whether ganda's audit already runs in
  CI for this repo (`dev workflow` / `.github/workflows/workflow.yml`); if not, that is the
  guard to add (audit step in the PR workflow), not a bespoke script.
- `runfile-shebang`: change the 238 research runfile to the house shebang (match the other
  runfiles in the repo; `head -1` a co-located test).
- `global-usings-analyzer`: add the TW0007 `.editorconfig` line the check asks for and confirm
  the SourceGenerators package version in `Directory.Packages.props` ships TW0007 (bump forward
  if not — never pin backward). `dev build` must stay 0/0 after enabling.
- `memsearch-scaffold`: install via the ganda command the check names (`ganda memsearch …` /
  `ganda repo audit --fix --checks memsearch-scaffold`), not hand-written hooks.
- `vscode-window-icon`: set `peacock.color` in `.vscode/settings.json` via the fixer.
- Gates: `ganda repo audit` exit 0; `dev build` 0/0; `dev test` unaffected (executable bit
  and shebang changes only touch runfile metadata).

## Checklist

- [x] `ganda repo audit --fix` applied; remaining items hand-fixed
- [x] 26 runfiles executable, mode committed
- [x] 238 research runfile shebang standardized
- [x] TW0007 `.editorconfig` entry; SourceGenerators version verified; `dev build` 0/0
- [x] memsearch hooks scaffolded via ganda
- [x] `peacock.color` set
- [x] audit step present in the PR workflow (or note why it already is)
- [x] `ganda repo audit` exit 0 on the branch
- [x] Implementation review: disposition `accepted-exceptions` (M1 fixed, M2 wontfix)

## Session

- Created: 2026-09-21 cockpit session (board audit follow-up)
- Implementer: grok task-work 2026-09-21 (claim worktree)
- Review oracle: grok task-work 2026-09-21 (effort 1, general; rounds 1–2)

## Results

Brought master back to a clean `ganda repo audit` (ganda 1.0.0-beta.29). Prefer `--fix` for every check that offered a fixer; hand-fixed only the SourceGenerators pin (TW0007 ships in 1.0.0-beta.11, not the previous 1.0.0-beta.10 pin) and the CI guard.

**What landed**

- `runfile-executable`: `git update-index --chmod=+x` on 26 shebang runfiles (25 co-located `*-tests.cs` under `source/container-apps/{web,api}/` plus the 238 research runfile). Index mode is `100755`.
- `runfile-shebang`: `kanban/done/238-…/research/run-rank-experiment.cs` now uses `#!/usr/bin/env -S dotnet --`.
- `global-usings-analyzer`: `[*.cs]` `dotnet_diagnostic.TW0007.filename = global-usings.cs`. CPM pin `TimeWarp.SourceGenerators` **1.0.0-beta.10 → 1.0.0-beta.11** (TW0007 is in beta.11; diagnostic stays package-default disabled so this is not a using sweep). `dev build` **0/0**.
- `memsearch-scaffold`: `ganda repo audit --fix --checks memsearch-scaffold` (not hand-written hooks). Added `.githooks/pre-commit`, `pre-push`, `post-checkout` and unified `post-commit`/`post-merge` with the baseline dispatcher.
- `vscode-window-icon`: fixer set `peacock.color` to `#83F8E4` (matches `peacock.remoteColor`).
- CI guard: new `repo-audit` job in `.github/workflows/workflow.yml` runs `ganda repo audit` on push/PR. Installs `TimeWarp.Ganda` from GitHub Packages only (`packages: read`; nuget.org omitted so a 401 cannot soft-fallback to public `1.0.0-beta.15`). Post-install version gate refuses nuget.org-vintage tools. Path filters now include `.editorconfig` and `.githooks/**` so audit-only PRs still fire the job.

**Not in scope:** `kebab-path-names` (untracked leftover tree on the operator master checkout; nothing tracked). TW0007 **severity** left at package default (disabled); enabling it would be a dedicated file-level-using sweep.

**Gates:** `ganda repo audit` exit 0 (28/28). `./bin/dev build` 0 Warning(s) / 0 Error(s). Executable-bit and shebang changes do not affect `dev test` product code.

### How to validate

**Smoke**

```bash
cd /path/to/this/worktree
ganda repo audit
./bin/dev build
git ls-files -s -- 'source/container-apps/**/*-tests.cs' \
  'kanban/done/238-evaluate-jev-for-ctrl-k-and-webmcp-action-catalog/research/run-rank-experiment.cs' \
  | awk '{print $1}' | sort | uniq -c
head -1 kanban/done/238-evaluate-jev-for-ctrl-k-and-webmcp-action-catalog/research/run-rank-experiment.cs
rg -n 'dotnet_diagnostic.TW0007.filename' .editorconfig
rg -n 'TimeWarp.SourceGenerators' Directory.Packages.props
rg -n 'peacock.color' .vscode/settings.json
test -x .githooks/pre-commit.cs && test -x .githooks/pre-push.cs && echo hooks-executable
rg -n 'repo-audit:|ganda repo audit|nuget.org-vintage|nuget.pkg.github.com' .github/workflows/workflow.yml
```

**Expect**

- `ganda repo audit`: exit 0, `Failed: 0`, 28 passed (including `runfile-executable`, `runfile-shebang`, `global-usings-analyzer`, `memsearch-scaffold`, `vscode-window-icon`).
- `./bin/dev build`: `0 Warning(s)` / `0 Error(s)`.
- `git ls-files -s` modes: only `100755` for those 26 paths.
- 238 research runfile first line: `#!/usr/bin/env -S dotnet --`.
- `.editorconfig` has `dotnet_diagnostic.TW0007.filename = global-usings.cs` under `[*.cs]` (not under `[ganda.audit]`).
- `Directory.Packages.props` pins `TimeWarp.SourceGenerators` at `1.0.0-beta.11`.
- `.vscode/settings.json` has `"peacock.color": "#83F8E4"`.
- `.githooks/pre-commit.cs` and `pre-push.cs` exist and are executable; `hooks-executable` prints.
- `workflow.yml` defines job `repo-audit` whose last step is `ganda repo audit`.
- Install ganda step: GitHub Packages only (no nuget.org `packageSources` add); version `case` refuses `1.0.0-beta.15` and earlier.

**Automated gate**

```bash
ganda repo audit    # expect: Repository passes all audit checks. / exit 0
./bin/dev build     # expect: 0 Warning(s) 0 Error(s)
```

**Not in scope:** full `dev test` (metadata/config only). Enabling `dotnet_diagnostic.TW0007.severity`. Live GitHub Packages install of ganda on a runner (needs `packages: read` against the org feed).

**Review disposition:** `accepted-exceptions` (0 open). Effort 1, roster `general`, 2 rounds.

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 1 |
| nit | 0 | 0 | 0 |

- M1 (suggestion, **fixed**): `repo-audit` install no longer lists nuget.org; post-install version gate refuses nuget.org-vintage `1.0.0-beta.15` or earlier so the CI guard cannot silently run stale checks.
- M2 (suggestion, **wontfix**): `kanban/**` stays out of workflow path filters; the required guard is co-located `*-tests.cs` under `source/**`.

Artifacts: `review/review-framework.md`, `review/round-1/merged.md`, `review/round-2/merged.md`, `review/disposition.md`.

## Notes

Origin of each check in timewarp-ganda: `global-usings-analyzer` 3791aff (2026-09-20),
`runfile-shebang`/`runfile-executable` a10946c (2026-06-17), `memsearch-scaffold` ee9936f,
`vscode-window-icon` b71e0c5, `kebab-path-names` 45c63de.

Review kitchen: `review/review-framework.md`, `review/round-1/`, `review/round-2/`, `review/disposition.md`.
