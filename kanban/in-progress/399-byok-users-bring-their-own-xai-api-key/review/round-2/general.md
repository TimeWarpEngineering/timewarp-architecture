# Round 2 — general
**Date:** 2026-10-26
**Scope reviewed:** fix commit `fa3e2acfe` vs `37ac90005` (the round-1 base), plus the call sites
of `BeginKeyResolution`, `ResolvedMode`, `MissingKeyDetail`, and `UserXaiApiKeyNames.NameFor`.

## Summary

The fix pass addresses all six round-1 findings. The BYOK mode is now a field on the slot the
caller opens, so both handlers read the mode from the same resolution. `CompleteAsync` uses the
mode returned with the client. The single cached client is replaced by a per-key cache of eight
live clients. Each client has a use count. An evicted client is disposed when its last turn
releases it. The client is released in a `finally` that covers every path after the rent. The
merge handlers no longer run a second delete inside a suppressed empty catch. The EF store calls
`UserXaiApiKeyNames.NameFor`. A settings read that throws is logged and treated as Required.

Re-run here: `XaiChatUpstream` 16/16, `UserXaiApiKey` 12/12, `CompleteAgentChat` 21/21,
`GetAgentChatConfigurationHint` 7/7, all passing. These match the counts in Results.

## Prior findings re-verified

- **M1 fixed.** `KeySlot.Mode` is filled in `IsConfiguredAsync` (`xai-chat-upstream-server.cs:113`).
  `ResolvedMode` reads `OpenTurn.Value?.Mode` (`:240`). `GetAgentChatConfiguration` now calls
  `BeginKeyResolution` before `IsConfiguredAsync`. The completion's null-client path uses the mode
  returned from `GetClientAsync` (`:160`). The new tests `Required_Names_Profile_And_Omits_The_Admin_Hint`
  and `Off_Names_An_Admin_And_Does_Not_Use_The_User_Key` go through the real upstream.
- **M2 fixed.** `RentLocked` / `Release` keep at most `MaxCachedClients` (8) live clients. An evicted
  client with no turn using it is disposed at once. A client still in use is disposed by the
  `Release` of its last turn. Tests cover alternating keys (2 created, 2 live) and 12 distinct keys
  (12 created, 8 live).
- **M3 fixed.** `DeleteUserKeyAsync` and its CA1031 suppression are gone from both callers. The
  matching `CrossSliceReference` attributes and the optional `IUserXaiApiKey` constructor
  parameters are gone too. The EF merge save is the delete.
- **M4 fixed.** `ef-principal-store-infrastructure.cs:381` calls `UserXaiApiKeyNames.NameFor`. The
  build passes, so the project can reference it.
- **M5 fixed.** `ReadModeAsync` logs the exception type and returns Required. `UserXaiApiKey` has a
  test for a settings store that throws: the site key is refused, the host key still wins, and
  the log does not contain a secret.
- **M6 fixed.** The `user-xai-api-key-application.cs` Design region now says that status
  unprotects.

## Issues

None in the fix delta.
