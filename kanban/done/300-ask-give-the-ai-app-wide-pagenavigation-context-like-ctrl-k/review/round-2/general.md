# Round 2 — general
**Date:** 2026-10-15
**Scope reviewed:** fix commit HEAD over 5df7913d2, plus round-1 ledger

## Summary
The round-1 fixes match their dispositions and no new defects were found. Build and tests were taken from the reported results and not re-run. The NavMenu cache is sound. When the cascaded task is null on every render, `Loaded` plus `ReferenceEquals(null, null)` correctly computes once. A sign-in or sign-out replaces the task that `CascadingAuthenticationState` supplies, so destinations recompute then. The contiguity check reports a split group, because a group is added to `closed` when the group changes and a later re-entry fails `ShouldNotContain`. It deliberately ignores the top-level `""` group. The `GetConstantValue` change handles literals and const fields and falls back to the default for non-constants. The new generator test covers the const path. Fail-closed constraint handling keeps `""` and `string` permissive, so plain `{id}` tokens still match.

## Prior findings
| ID | Round-1 status | Verified |
|----|----|----|
| M1 | fixed | yes. Each principal's set is pinned: Anonymous `["/"]`, PlainUser the five urls in menu order, Developer with `/` and no `/Settings` or `/Admin*`, Administrator equal to `PageRegistry.All`. A pass-everything filter now fails. |
| M2 | fixed | yes. The guard now uses `AllDirectories` and adds `nameof(...Page)` and `XPage.GetPageUrl/Title/NavIcon/Policy` patterns. |
| M3 | fixed | yes. NavGroup and Description use `GetConstantValue`, the comments are corrected, and the new const test asserts the emitted row. |
| M4 | fixed | yes. The contiguity assertion is correct, with a nit on top-level (Issue 1). |
| M5 | fixed | yes. The comment now says NavOrder, then route. |
| M6 | fixed | yes. Unknown constraints return false, `""` and `string` still accept, and the Design text is updated. |
| M7 | wontfix | yes. The rationale is reasonable: the page choice predates the task, `navigate` refuses the url safely, and the case needs an action permission without the page's read policy. A wrong navigation cannot result. Recording it in Notes is enough. |
| M8 | fixed | yes. Recompute happens only when the auth task reference changes (see Summary). |

## Issues
### Issue 1 — Severity: nit
- File: tests/container-apps/web/web-spa-integration-tests/features/application/navigation-destinations-tests.cs:132
- Description: The contiguity check skips the top-level `""` group (`current is { Length: > 0 }`). A NavOrder that interleaves top-level pages around a categorised group would pass. This is probably harmless, since top-level pages draw no header.
- Suggestion: Optional. Add a comment saying the top-level group is exempt on purpose.
- Status: open

### Issue 2 — Severity: nit
- File: tests/container-apps/web/web-spa-integration-tests/features/application/navigation-destinations-tests.cs:270
- Description: The `PageMember` regex `\b\w+Page\.(GetPageUrl|Title|NavIcon|Policy)\b` would also flag a local such as `currentPage.Title` in the agent sources. There is no current offender, and the failure would be loud and easy to fix.
- Suggestion: None required. Optionally anchor on an uppercase-initial type name, `\b[A-Z]\w*Page\.`.
- Status: open
