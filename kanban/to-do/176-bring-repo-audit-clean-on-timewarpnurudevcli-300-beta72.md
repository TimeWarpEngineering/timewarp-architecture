# Bring repo audit-clean on TimeWarp.Nuru.DevCli 3.0.0-beta.72

## Description

Org wave (timewarp-nuru 458-010 remediation + DevCli 3.0.0-beta.72 adoption —
they are the same wave: the audit's `nuru` check went red org-wide when
beta.72 shipped, by design). Passing `ganda repo audit` now means adopting the
full release toolkit: `dev release`, promotion gates, attestation verifier,
trusted-publishing probe, derived package sets.

## Checklist

- [ ] `ganda repo audit --fix` (bumps TimeWarp.Nuru/DevCli to latest, fixes kebab/structure where fixable)
- [ ] Verify Directory.Packages.props pins TimeWarp.Nuru.DevCli (and TimeWarp.Nuru where referenced) at 3.0.0-beta.72
- [ ] Build — NURU050 names any missing DI registration (e.g. `IPackableProjectService`); add per the DevCli readme migration notes (CS0101 local-CiMode note also applies)
- [ ] `dev self-install` (AOT binary is a snapshot; new commands like `release` are absent until reinstalled)
- [ ] `ganda repo audit` → PASSES ALL CHECKS (if a check is structurally unfixable here, record it explicitly with a reason instead of forcing)
- [ ] Smoke: `dev --help` shows `release`; `dev check-version` derives the packable set (publishers only)
- [ ] Commit everything (audit fixes, props, dev.cs, kanban) — local commits fine; ride the repo's normal merge flow

## Notes

Created 2026-08-08 from the nuru 458 program session. timewarp-nuru is the
reference (audit-clean at beta.72, first release shipped through the full
machinery).
