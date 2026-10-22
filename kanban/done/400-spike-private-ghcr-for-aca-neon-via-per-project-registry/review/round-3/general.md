# Round 3 — general
**Date:** 2026-10-22
**Scope reviewed:** fix delta `a53c41364..e2ab3757a`: the Token paragraph in
`skills/tw-deploy/references/azure-container-apps-neon.md` (lines 99-102) and the task.md edits.

## Summary

The fix pass rewrote the Token paragraph. It now says `dev deploy` does not forward a registry
token, and it puts the user-secret home and the `dev deploy` copy in intended-design wording
("belongs", "should copy"). That closes round-2 M3. `rg -i "ghcr|github.*token" tools/dev-cli` is
empty. The only registry code in dev-cli is the endpoint `/v2/` reachability preflight
(`aspire-deploy-preflight.cs`), which sends no credential. So "does not forward a registry token"
is accurate. The task.md acceptance bullet and Results now use the same intended-design wording.
The smoke `rg` checks behave as documented: the forbidden-phrase pattern and `copies it` print
nothing, and `does not forward a registry token` and `should copy it` hit the Token paragraph.

## Issues

None.
