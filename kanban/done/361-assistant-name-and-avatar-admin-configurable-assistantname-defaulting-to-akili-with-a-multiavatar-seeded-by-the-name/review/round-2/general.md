# Round 2 — general
**Date:** 2026-10-14
**Scope reviewed:** 639643d5f..bc1066749 (fix delta) + re-verification of round-1 M1–M10

## Summary
All ten round-1 findings are fixed. The new `AssistantIdentityRefreshHandler` follows the
`IncrementCountNotificationHandler` post-pipeline shape. It refreshes only after a successful
`UpdateSiteSettings` (or a `ResetStore`) and cannot loop. A failure in the nested load is
swallowed by that action's own `StateTransactionBehavior` (`RethrowHandlerExceptions` defaults
to false), so it cannot roll back the outer, already-committed save. One new gap: the M6 rule
checks UTF-16 code units, so astral-plane format characters (the Unicode tag block used for
invisible prompt "ASCII smuggling") pass. Gates were not re-run in this review; the task file
reports them green.

## Prior findings
| ID | Verified status | Note |
|----|-----------------|------|
| M1 | fixed | `AskSettingsMenu.razor` `ButtonName`/`StatusSubtitle` derive from `AgentSurfaceState.AssistantName`; probe text neutral; `ask-tag.ts` hint neutral. A grep of web-spa string literals finds no remaining user-facing name-as-"Ask" (only opt-out reasons and CrossSliceReference justifications). Tests updated (unit + Playwright). |
| M2 | fixed | `agent-surface-state.assistant-identity-refresh-handler.cs`: has a Purpose region; filters on `UpdateSiteSettingsActionSet.Action` && `!SaveFailed`. `PostPipelineNotificationRequestPostProcessor` publishes only after `next` returns, so a thrown handler never triggers it. `LoadAssistantIdentity`'s own notification matches no arm, so there is no loop. The CrossSliceReference on `SiteSettingsState` (Applications → Settings) is justified and matches the existing AuthenticationPage justification style; `ApplicationState` is same-slice. The dispatch is gone from `AuthenticationPage.HandleValidSubmit`. Tests cover success, 409 (no identity call: `Calls == 1`) and reset. A minor, harmless edge: if the semaphore wait is cancelled, `Handle` returns without touching `SaveFailed`, so a stale `false` causes one extra GET. |
| M3 | fixed | `RenderInstructions` reads `ScopedStore.GetState<AgentSurfaceState>().AssistantName` per request. The credential-label caveat is documented in the Design region. |
| M4 | fixed | `LoadAssistantIdentity` moved after `HandleUserAuthentication(...)`, with a comment saying why. |
| M5 | fixed | Single-entry immutable `AvatarEntry` record behind `Volatile.Read`/`Volatile.Write`. Reference writes are atomic and misses compute the same pure value, so the cache is thread-safe without a lock. The process-wide static is harmless across test hosts because the value is a pure function of the name. |
| M6 | fixed (partially, see N1) | Cc/Cf/Zl/Zp and `<` `>` are rejected via `CharUnicodeInfo.GetUnicodeCategory`; BMP cases tested in both validator and aggregate. Simple emoji (surrogate pairs report `Surrogate`) are not wrongly rejected; ZWJ sequences are (N2). |
| M7 | fixed | Validator = `NotEmpty` + `Must(AssistantNameRules.IsValid)` when non-blank, so both paths run the same check, on the trimmed value. Design text matches. |
| M8 | fixed | `JsonDocument` property-set assertion `["avatar","name"]`; substring checks removed. |
| M9 | fixed | `try`/`finally` restores `before.Name`. |
| M10 | fixed | Same handler reacts to `ApplicationState.ResetStoreActionSet.Action`; documented in the `AgentSurfaceState` Design region; test `Reload_The_Identity_After_A_Store_Reset`. |

## Issues
### N1 — Severity: suggestion
- File: source/libraries/timewarp-identity/settings/assistant-name-rules.cs:49
- Description: `IsPlainText` iterates `char`s, so every astral-plane code point reports
  `UnicodeCategory.Surrogate` and passes. That lets through astral format (Cf) characters the
  Design region says are rejected. Examples: U+E0001 and U+E0020–U+E007F (the tag block, which
  renders invisibly and is the known "ASCII smuggling" vector for hiding text in LLM prompts),
  and U+1D173–U+1D17A. For example, `"Akili" + "\U000E0049\U000E0047\U000E004E..."` passes,
  shows as "Akili" in every label, and carries hidden text into the system prompt's first line.
  That is the injection case M6 was meant to close. Lone (unpaired) surrogates also pass.
- Suggestion: Iterate `name.EnumerateRunes()` and test `Rune.GetUnicodeCategory(rune)` against
  the same set. Reject `Rune.ReplacementChar` produced from an invalid sequence, or check for
  unpaired surrogates (`UnicodeCategory.Surrogate` on a `char` that isn't part of a valid pair).
  Add `"Ak\U000E0041ili"` and `"Ak\uD800ili"` to both invalid-name test sets.
- Status: open

### N2 — Severity: nit
- File: source/libraries/timewarp-identity/settings/assistant-name-rules.cs:60
- Description: Rejecting all of Cf also rejects U+200D ZERO WIDTH JOINER. Single emoji pass
  (for example "Akili 🤖"), but ZWJ emoji sequences such as "🧑‍💻" or "🏳️‍🌈" fail with a
  "plain text" message that doesn't explain why. This may be an acceptable trade-off, but it
  is not stated, and the Design region lists "zero-width characters" only as a spoofing risk.
- Suggestion: Either state the trade-off in the Design region (ZWJ emoji sequences are not
  allowed) or allow U+200D when it sits between two emoji/pictographic runes. Add one
  valid-name test with a plain emoji so the surrogate path stays allowed.
- Status: open

### N3 — Severity: nit
- File: source/libraries/timewarp-identity/settings/assistant-name-rules.cs:14
- Description: The edited Design line is 146 characters. "Akili is Swahili for intelligence,
  mind or wit." was appended to it without a reflow, while the surrounding region wraps at
  about 100 characters.
- Suggestion: Reflow the region.
- Status: open
