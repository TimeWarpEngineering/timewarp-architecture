# Round 1 — general
**Date:** 2026-10-22
**Scope reviewed:** branch 9a5271bd0 vs master f64b498d7, `source/` and `tests/`

## Summary

The change adds a native `<span class="twe-avatar">` initials circle, a pure `AvatarInitials.From`
helper built on `StringInfo`, the `InitialsAvatarSize` enum, an Avatars card in the style guide
after Segmented Control, and an overview row. It matches the task and the implementer
restatement. The ARIA rules, the single brand fill, the isolated CSS that uses existing `--twe-*`
tokens, the Purpose and Design regions, and the `Card`-style `Class`/unmatched-attribute root
all follow 379-001 to 379-005. The risk is low.

Re-verified in this worktree: `--filter-class AvatarInitials_Should_` passed 8 of 8 and
`--filter-class Avatar_Should_` passed 7 of 7. `git diff master...HEAD -- source tests | rg -i crunchit`
printed nothing. The tokens `--twe-primary`, `--twe-on-primary`, `--twe-text-helper`,
`--twe-text-eyebrow`, and `--twe-text-page-title` all exist in `tokens.css`. A caller's `class`
attribute binds to `Class`, because Blazor matches parameter names without regard to case, so
the splat after `class=` cannot overwrite the root class.

## Issues

### Issue 1 — Severity: suggestion
- File: tests/container-apps/web/web-spa-integration-tests/components/avatar-render-tests.cs:40
- Description: The checklist asks for a test that the default size has no modifier class.
  `Leave_The_Default_Size_Without_A_Modifier` passes `Size = InitialsAvatarSize.Medium`
  explicitly, so it covers Medium but not the default. No test that omits `Size` checks for the
  missing modifier. If someone changed the parameter default in `Avatar.razor` to `Small` or
  `Large`, every test would still pass.
- Suggestion: Drop the explicit `["Size"]` entry from that test, or add
  `RootTag(html).ShouldNotContain("twe-avatar--")` to `Render_The_Initials_On_A_Twe_Avatar_Root`,
  which already renders without `Size`.
- Status: open

### Issue 2 — Severity: nit
- File: source/container-apps/web/projects/web-spa/components/elements/avatar-initials.cs:26
- Description: Two guards can never fire. After `string.IsNullOrWhiteSpace(name)` returns false,
  splitting on whitespace with `RemoveEmptyEntries` always yields at least one word, so
  `words.Length == 0` is unreachable. Every such word is non-empty, so
  `LengthInTextElements == 0` in `FirstTextElement` (line 44) is unreachable too. This file ships
  in every generated app.
- Suggestion: Remove both guards, and have `FirstTextElement` return
  `StringInfo.GetNextTextElement(word)`, or `SubstringByTextElements(0, 1)` without the guard.
- Status: open
