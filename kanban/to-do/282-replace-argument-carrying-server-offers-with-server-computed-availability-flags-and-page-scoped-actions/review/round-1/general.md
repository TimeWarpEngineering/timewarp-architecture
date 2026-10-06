# Round 1 — general
**Date:** 2026-10-06
**Scope reviewed:** branch vs master (7f02904f9)

## Summary
The diff matches the task's Change table and Remove list. Handler flags are computed from the ACTIVE set via `CredentialRules` (`CanRevoke` over the active count and `!IsRevoked`, `CanRename` for active rows, `CanLinkMicrosoft365` over active types), so revoked rows get false under `IncludeRevoked` and the flags default to false. SPA components dispatch `CredentialsState` actions directly and then `FetchCredentials`, with no leftover `CrossSliceReference` for offers and no contextual-row remnants. `git grep -i offer` hits are only the Entra/site-settings "offered" concept and the intended retirement notes (AGENTS.md TWA0029-31 / TWE012-14 rows, descriptor SSOT comment), the skill text is public-safe, and the new tests (server flags + stale 409, SPA flags/dispatch, contract round-trips) are meaningful. I read the code and tests only and did not build or run anything.

## Issues

None.
