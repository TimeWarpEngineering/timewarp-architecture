# Round 1 — general
**Date:** 2026-09-24
**Scope reviewed:** same as framework (ticket processor, recorder Design region, 3 test files)

## Summary

`EntraTicketProcessor` takes the singleton `CredentialUsageRecorder` and stamps `LastUsedAt` at every
stamp point in the brief. The points are sync-hit, the already-linked bootstrap return, the bootstrap
race winner, new link / already-have attach, bootstrap mint, and link-merge (which re-reads the row after
the merge). Each sign-in stamp runs after that path's liveness/quarantine early return. Replacing
`RefreshAccountHintAsync` with `RecordSignInAsync` puts the hint and the stamp in one UPDATE, and
lost-race handling moves into the recorder, which has the same drop-never-retry rule. The Design regions
are reconciled, and the edge where the clock regresses and the hint is deferred is documented. Risk is
low. Re-verified: `--filter-class EntraTicketProcessor` 21/21, `--filter-class EntraChallenge` 17/17.

## Issues

<!-- None raised. -->
