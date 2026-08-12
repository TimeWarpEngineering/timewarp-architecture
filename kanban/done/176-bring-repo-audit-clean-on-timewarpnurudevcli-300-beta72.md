# Bring repo audit-clean on TimeWarp.Nuru.DevCli 3.0.0-beta.72

## Description

Org wave (timewarp-nuru 458-010 remediation + DevCli 3.0.0-beta.72 adoption —
they are the same wave: the audit's `nuru` check went red org-wide when
beta.72 shipped, by design). Passing `ganda repo audit` now means adopting the
full release toolkit: `dev release`, promotion gates, attestation verifier,
trusted-publishing probe, derived package sets.

## Checklist

- [x] `ganda repo audit --fix` (bumps TimeWarp.Nuru/DevCli to latest, fixes kebab/structure where fixable)
- [x] Verify Directory.Packages.props pins TimeWarp.Nuru.DevCli (and TimeWarp.Nuru where referenced) at 3.0.0-beta.72
- [x] Build — NURU050 names any missing DI registration (e.g. `IPackableProjectService`); add per the DevCli readme migration notes (CS0101 local-CiMode note also applies)
- [x] `dev self-install` (AOT binary is a snapshot; new commands like `release` are absent until reinstalled)
- [x] `ganda repo audit` → PASSES ALL CHECKS (if a check is structurally unfixable here, record it explicitly with a reason instead of forcing)
- [x] Smoke: `dev --help` shows `release`; `dev check-version` derives the packable set (publishers only)
- [x] Commit everything (audit fixes, props, dev.cs, kanban) — local commits fine; ride the repo's normal merge flow

## Notes

Created 2026-08-08 from the nuru 458 program session. timewarp-nuru is the
reference (audit-clean at beta.72, first release shipped through the full
machinery).

### Implementation notes (2026-08-08)

**Before:** 20 pass / 3 fail — `nuru` (beta.71), `cpm-consistency` (orphaned Microsoft.CodeAnalysis), `kebab-path-names` (3 done-task double-dash filenames).

**After:** 23 pass / 0 fail.

- `--fix` bumped Nuru, removed orphan PackageVersion, self-install OK on interim state
- Hand: DevCli still beta.71 after fix → set `3.0.0-beta.72`; DI removed `GitTagCheckService`, added `IPackableProjectService`/`PackableProjectService`
- Kebab fix renamed three `kanban/done/*--*.md` files (double-dash → single)
- Amuru already 1.0.0 + Amuru.Tools beta.2

## Results

Repo is audit-clean on TimeWarp.Nuru / DevCli **3.0.0-beta.72**. Dev CLI self-installed; `release` and `check-version` present; packable set derived (11 packages).

### How to validate

**Smoke**
```bash
cd /home/steve/worktrees/github.com/TimeWarpEngineering/timewarp-architecture/dev
grep -E 'TimeWarp\.Nuru' Directory.Packages.props
# Expect: both at 3.0.0-beta.72

ganda repo audit
# Expect: Repository passes all audit checks.

./bin/dev --help | grep release
./bin/dev check-version
# Expect: Packages checked lists TimeWarp.* packables
```

**Automated gate**
```bash
ganda repo audit   # exit 0
```

**Depends on / Not in scope**
- Local commits only; no push
- Partial publish state of 2.0.0-beta.14 is pre-existing, not this wave

## Session

- Implementation: grok (2026-08-08) — audit --fix + DevCli pin/DI + kebab + self-install
