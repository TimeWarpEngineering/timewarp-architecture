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

- [ ] `ganda repo audit --fix` applied; remaining items hand-fixed
- [ ] 26 runfiles executable, mode committed
- [ ] 238 research runfile shebang standardized
- [ ] TW0007 `.editorconfig` entry; SourceGenerators version verified; `dev build` 0/0
- [ ] memsearch hooks scaffolded via ganda
- [ ] `peacock.color` set
- [ ] audit step present in the PR workflow (or note why it already is)
- [ ] `ganda repo audit` exit 0 on the branch

## Session

- Created: 2026-09-21 cockpit session (board audit follow-up)

## Notes

Origin of each check in timewarp-ganda: `global-usings-analyzer` 3791aff (2026-09-20),
`runfile-shebang`/`runfile-executable` a10946c (2026-06-17), `memsearch-scaffold` ee9936f,
`vscode-window-icon` b71e0c5, `kebab-path-names` 45c63de.
