# Round 2 — merged findings
**Date:** 2026-09-23
**Sources:** general
**Scope:** merge resolution `3aeed9b1` (origin/master task 246 → this branch) + branch delta vs `origin/master` for the six conflicted files and their call sites. See `review-framework.md` "Round 2".

## Counts (final, rolled up across rounds)

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 4 | 0 |
| nit | 0 | 2 | 1 |

## Prior findings (carried from round-1/merged.md)

| ID | Severity | Status | Round-2 verification |
|----|----------|--------|----------------------|
| M1 | suggestion | fixed | Migration data step unchanged; file not in the conflict set. |
| M2 | suggestion | fixed | `PendingNicknameOwnedByPrompt` / `PendingListRenameCredentialId` intact; both Settings and Passkeys bind `PendingListRenameCredentialId` post-merge. |
| M3 | nit | fixed | Auto-opened editor draft retention intact in `SaveRenameAsync`. |
| M4 | suggestion | fixed | `credential-list-render-tests.cs` re-resolved to `RevokePasskey`; new fact `Revoke_Disabled_Disables_First_Step_And_Shows_Hint` pins 246's guard on the two-step row. |
| M5 | suggestion | fixed | Store-contract round-trip case intact; not in the conflict set. |
| M6 | nit | fixed | Wire-break Design notes intact; not in the conflict set. |
| M7 | nit | wontfix | Unchanged — the merge added no interactive component harness; rationale stands (see `disposition.md`). |

## New issues

None. The reviewer's falsifiable checks (all passed; orchestrator spot-checked the first three):

- `CredentialList.razor`: `RevokeDisabled` gates BOTH the first-step button (`CredentialList.razor:203`) and the Confirm button (`:220`); Confirm raises `OnRevoke` (`:137`); hint renders under `RevokeDisabledHintDataQa` (`:209`).
- No leftover `Delete*` / `OnDelete` identifiers except the intentional `ShouldNotContain("data-qa=\"DeletePasskey\"")` in `protected-page-deep-link-tests.cs:251`; no conflict-marker residue in `source`, `tests`, `kanban`.
- 246 intact: `CanRevoke = CredentialsState.CanUnlink(ActiveCredentialCount)` on Settings (both `CredentialList` instances) and Passkeys; hint text and `RevokePasskeyHint` data-qa; "Credential revoked." status text.
- 248-001 intact: nickname title, context line, fingerprint, inline rename, two-step restating confirmation, all rewired onto the `Revoke*` surface.
- `CredentialSummary` 9-arg construction in `credentials-spa-test-application.cs` and the render test's `Summary()` helper match `get-credentials-contracts.cs:98-109` parameter order.
- Design regions in `CredentialList.razor` and `SettingsPage.razor.cs` describe the merged behavior.

## Duplicates / conflicts

- Single reviewer; none.
