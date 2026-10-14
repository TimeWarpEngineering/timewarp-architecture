# Round 1 — general
**Date:** 2026-10-14
**Scope reviewed:** `.github/workflows/workflow.yml` diff vs master; task Results claims

## Summary

The change widens `push`/`pull_request` branch filters from `master` + `feature/overnight` to
`master` + `'feature/**'` (glob subsumes the old entry) and drops `edited` from
`pull_request.types`. Re-verified against the workflow: `detect-paths` runs only on push/PR;
the NuGet OIDC login step (`if: release || probe`) and the `--api-key` branch of `dev workflow`
key on `github.event_name == 'release'` only — no job/step condition keys on branch name, so the
wider globs cannot reach publishing. New-branch pushes (`before` = zeros) enable all path-gated
jobs, which is the intended conservative behavior. The header comment documents the retarget
consequence of dropping `edited`, and Results record proof runs. Low risk.

## Issues

None.
