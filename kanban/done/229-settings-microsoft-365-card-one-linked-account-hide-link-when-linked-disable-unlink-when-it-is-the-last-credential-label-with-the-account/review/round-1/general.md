# Round 1 — general
**Date:** 2026-09-16
**Scope reviewed:** branch task/229-settings-microsoft-365-card-one-linked-account-hid vs origin/master merge-base 5a2073d7 (commit ffad1e05)

## Summary

The change enforces one active EntraAccount per principal (UI hide Link + server 409 before `AddCredentialAsync`), disables Unlink when it would leave zero active credentials (with the required hint; `LastCredential` 409 unchanged as backstop), and stores `Credential.Label` from `preferred_username` else `name` else `"Microsoft 365"` on bootstrap and link. Risk is low: same-handle link stays idempotent ahead of the new check, revoked rows are excluded from the one-account list, GetCredentials still returns all types for `ActiveCredentialCount`, and processor/prerender/SPA tests cover the stated behaviors. No product defects found against the task requirements.

## Issues
