# Round 2 — merged findings
**Date:** 2026-11-25
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 2 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: skills/tw-web-api-contracts/references/mutability.md:105
- Description: Point 3 named both call sites and then stated one rule. The fix states them separately. `AuthenticationPage.AcceptPolicy` replaces a clean Command with `From` and copies only `Version` on a dirty Command when `SamePolicyFields` holds. `RoleEditForm.OnParametersSet` calls `From` when id, name, or description differ from the values last copied, and otherwise keeps the Command without patching `Version`.
- Suggestion: State the two rules separately.
- Source: general
- Disposition notes: confirmed fixed in round 2 by reading the fix delta and both call sites.

### M2 — Severity: bug — Status: fixed
- File: skills/tw-blazor/SKILL.md:288
- Description: The hook table required storing the whole Response, and the following paragraph forbade flattened fields, while `RoleState.OnGetRolesLoaded` stores rows and the attribute omitted `SetRoleAgentScopes`. The fix limits the whole-Response rule to the edit-form query, allows the list hook to store rendered rows, and adds `typeof(SetRoleAgentScopes)`.
- Suggestion: Limit the whole-response rule to the edit-form query, allow the list hook, and add `SetRoleAgentScopes`.
- Source: general
- Disposition notes: confirmed fixed in round 2 against `role-state.cs`, `role-state.get-role.cs`, and `role-state.get-roles.cs`.

### M3 — Severity: suggestion — Status: fixed
- File: skills/tw-web-api-contracts/references/mutability.md:92
- Description: Point 2 described `Clone` without the guide's null, element-sharing, and non-`ICloneable` rules. The fix adds those rules beside the collection sentence, and point 3 says `From` follows them.
- Suggestion: Add the guide's collection rules and the State field-copy fallback.
- Source: general
- Disposition notes: confirmed fixed in round 2 against `documentation/developer/guides/edit-form-details-copy.md` and `DetailsCopyEmitter.ValueExpression`.

## Duplicates / conflicts

- One general reviewer. No new findings. Prior M1–M3 stay fixed.
