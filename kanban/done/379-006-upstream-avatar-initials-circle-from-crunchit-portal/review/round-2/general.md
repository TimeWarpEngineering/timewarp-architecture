# Round 2 — general
**Date:** 2026-10-22
**Scope reviewed:** fix delta `51dd32538..b336a866e` (`source/` and `tests/`): `avatar-initials.cs`
and `avatar-render-tests.cs`

## Summary

The fix pass changes two files. It removes the two unreachable guards from `AvatarInitials.From`
and stops passing `Size` in the default-size render test. Both round-1 findings are resolved.
`string.Split(Array.Empty<char>(), RemoveEmptyEntries)` splits on `char.IsWhiteSpace`, the same
test `string.IsNullOrWhiteSpace` uses. So a name that passes the guard always has at least one
non-empty word, and `StringInfo.GetNextTextElement` on a non-empty string returns one whole text
element. The default-size test now fails if the `Size` default changes to Small or Large, because
the root would then carry `twe-avatar--`. `--filter-class Avatar` ran 15 tests and all 15 passed.

## Prior findings

- M1 (suggestion): fixed. `Leave_The_Default_Size_Without_A_Modifier` renders with only `Name`
  and asserts `ShouldNotContain("twe-avatar--")`.
- M2 (nit): fixed. Both dead guards are gone. `FirstTextElement` is a one-line
  `StringInfo.GetNextTextElement` call. The Design region still matches the behavior.

## Issues

None in the fix delta.
