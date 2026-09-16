# Disposition — task 229

**Date:** 2026-09-16
**Outcome:** clean
**Rounds:** 1
**Final open count:** 0

## Summary

Effort-1 general review of branch `task/229-settings-microsoft-365-card-one-linked-account-hid` vs `origin/master` merge-base `5a2073d7` (commit `ffad1e05`). Round 1 (`general`) raised no issues against the three requirements: one active EntraAccount per principal (Link hidden; link of a second handle is 409 `Microsoft 365 already linked` before `AddCredentialAsync`; same-handle link stays idempotent), Unlink disabled with hint when last active credential (`CanUnlink = ActiveCredentialCount > 1`; server `LastCredential` 409 unchanged), and credential Label from `preferred_username` else `name` else `"Microsoft 365"` on bootstrap and link (card title = label, subtitle = Microsoft 365). Processor, challenge, prerender, and SPA formula tests cover those behaviors. Disposition is **clean**.

## Exception log (if accepted-exceptions)

(none)

## Escalations

- None
