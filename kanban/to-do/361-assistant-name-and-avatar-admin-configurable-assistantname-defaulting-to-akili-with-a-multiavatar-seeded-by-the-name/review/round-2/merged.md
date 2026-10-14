# Round 2 — merged findings
**Date:** 2026-10-14
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 2 | 0 |
| suggestion | 0 | 5 | 0 |
| nit | 0 | 6 | 0 |

## Resolved prior

M1–M10 re-verified as fixed against bc1066749 (see `round-2/general.md` Prior findings table).

## Issues

### N1 — Severity: suggestion — Status: fixed
- File: source/libraries/timewarp-identity/settings/assistant-name-rules.cs:49
- Description: `IsPlainText` iterates UTF-16 chars, so astral Cf characters (Unicode tag block U+E0001, U+E0020–E007F — ASCII smuggling into the system prompt) and lone surrogates pass.
- Suggestion: Iterate `EnumerateRunes()` with `Rune.GetUnicodeCategory`; reject invalid sequences / lone surrogates; add `"Ak\U000E0041ili"` and `"Ak\uD800ili"` to both invalid-name test sets.
- Source: general
- Disposition notes: `IsPlainText` now walks Unicode scalars with `Rune.DecodeFromUtf16` and checks `Rune.GetUnicodeCategory` against Cc, Cf, Zl and Zp; any status other than `OperationStatus.Done` (lone surrogate) is rejected, so a literal U+FFFD is not the discriminator. `<` `>` rejection kept. `"Ak\U000E0041ili"` added to both `[Input]` invalid sets. A lone surrogate cannot round-trip through attribute metadata (stored as UTF-8, becomes U+FFFD), so it is covered by dedicated tests that build the string in code: `Lone_Surrogate_Assistant_Name_Should_Fail_Validation` (update-site-settings-tests.cs) and `Rename_Should_Reject_Lone_Surrogate` (site-settings-assistant-name-tests.cs).

### N2 — Severity: nit — Status: fixed
- File: source/libraries/timewarp-identity/settings/assistant-name-rules.cs:60
- Description: Rejecting all Cf also rejects U+200D ZWJ, so ZWJ emoji sequences fail; trade-off not stated.
- Suggestion: State the trade-off in the Design region; add a valid-name test with a plain (astral) emoji.
- Source: general
- Disposition notes: Design region states that single emoji are allowed but ZWJ emoji sequences are not, because U+200D is a format character and every format character is rejected. `"Akili \U0001F916"` added as a valid name in the validator tests and in a new `Rename_Should_Accept_Valid_Names` aggregate test.

### N3 — Severity: nit — Status: fixed
- File: source/libraries/timewarp-identity/settings/assistant-name-rules.cs:14
- Description: Design region line is 146 characters (no reflow).
- Suggestion: Reflow to ~100 columns.
- Source: general
- Disposition notes: Design region reflowed to at most 100 columns.

## Duplicates / conflicts

- None.
