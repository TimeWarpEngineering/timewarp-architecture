# Round 2 — general
**Date:** 2026-11-25
**Scope reviewed:** product delta since round 1 (`9bfe613cd`..`41fe221da`): `skills/tw-web-api-contracts/references/mutability.md` and `skills/tw-blazor/SKILL.md`, checked against task 518, `## Goal (implementer restatement)`, `AuthenticationPage.AcceptPolicy`, `RoleEditForm.OnParametersSet`, `RoleState`, and `documentation/developer/guides/edit-form-details-copy.md`. Recorded fix-pass gates (`./bin/dev build`, `./bin/dev lint-skills`, `ganda repo audit`) cover the markdown-only delta. Gates were not re-run.

## Summary

The fix pass splits the two reconcile rules, limits the whole-response store rule to the edit-form query, lists `SetRoleAgentScopes` on the `[ContractState]` snippet, and copies the guide's collection rules into point 2. Those edits match the call sites and the guide. The restatement still matches the task. No new issues.

## Issues

None.

## Resolved prior

### M1 — Severity: bug — Status: fixed
- File: skills/tw-web-api-contracts/references/mutability.md:105
- Description: Point 3 now states two rules. A store-reading page replaces a clean Command with `From` and, when dirty, copies only `Version` when the new response's form fields still match the snapshot (`AuthenticationPage.AcceptPolicy`: `SamePolicyFields` then `Command.Version = stored.Version`). A child form calls `From` when id, name, or description differ from the values last copied, and otherwise returns without patching `Version` (`RoleEditForm.OnParametersSet`).
- Status: fixed

### M2 — Severity: bug — Status: fixed
- File: skills/tw-blazor/SKILL.md:288
- Description: The hook table says store what the page reads. The next paragraph limits the whole Response to the edit-form query (`OnGetRoleLoaded` sets `RoleSnapshot = response`) and allows a list hook to store rendered rows (`OnGetRolesLoaded` sets `RoleRows` from `response.Items`). The attribute lists `SetRoleAgentScopes`, matching `role-state.cs`. Point 1 in `mutability.md` is scoped to the edit-form query.
- Status: fixed

### M3 — Severity: suggestion — Status: fixed
- File: skills/tw-web-api-contracts/references/mutability.md:92
- Description: Point 2 now says elements are shared except a non-`string` `ICloneable` reference, a null nullable collection stays null, a null non-nullable collection becomes empty, other collection types are assigned as-is, and TimeWarp.State copies fields itself when the object does not implement `ICloneable`. That matches the guide and `DetailsCopyEmitter.ValueExpression`. Point 3 says `From` follows the same rules.
- Status: fixed
