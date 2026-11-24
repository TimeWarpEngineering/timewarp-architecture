# Round 1 — general
**Date:** 2026-11-25
**Scope reviewed:** branch `task/518-tw-web-api-contracts-skill-store-snapshot` vs `master` (merge-base `40d680e8`): `skills/tw-web-api-contracts/` (`SKILL.md`, `references/mutability.md`, `references/examples.md`) and `skills/tw-blazor/SKILL.md`, checked against task 518 and `## Goal (implementer restatement)`, and against `documentation/developer/guides/edit-form-details-copy.md`, `AuthenticationPage.razor`, `RoleEditForm.razor`, and `RoleState`.

## Summary

The skill now says the store keeps the fetched response, the form binds an `Update*` command from generated `From`, `Version` moves through `CopyBeyondDetails`, and TWA0047–TWA0049 are named next to the edit-form rules. The required stale passages in `SKILL.md` and `mutability.md` no longer tell a reader to bind the stored response. Recorded gates (`./bin/dev lint-skills`, `./bin/dev build`, `ganda repo audit`) cover a markdown-only change; `SKILL.md` is 499 lines as Results state. Two call sites are described as one reconciliation rule, and the `[ContractState]` section states a store rule `RoleState`'s list hook does not follow. The restatement matches the task's interface steps 5 and 6 (they are items 5 and 6 under section 3). No misread.

## Issues

### Issue 1 — Severity: bug
- File: skills/tw-web-api-contracts/references/mutability.md:96
- Description: Point 3 names both call sites and then states one rule. `AuthenticationPage.AcceptPolicy` replaces the command when it is clean, and when it is dirty copies `Version` only when the new response's policy fields match the snapshot the command was copied from. `RoleEditForm.OnParametersSet` calls `From` when `RoleId`, `Name`, or `Description` differ from the values last copied, which replaces the working copy, and leaves the command in place while those three match. That form does not patch `Version` onto a command it keeps. The sentences "An unedited Command is replaced from the new response. An edited Command keeps its edits, and takes the new `Version` when the response's form fields still match what it was copied from" are the authentication page. Read as the rule for the child form as well, they tell an agent to keep a role-name edit across a changed snapshot and to patch `Version` on `RoleEditForm`.
- Suggestion: State the two rules separately. Store-reading page: clean command is replaced from the new response; a dirty command keeps its field edits and copies `Version` when the new response's form fields still match the snapshot it was copied from (`AuthenticationPage.razor`). Child form: `OnParametersSet` calls `From` when the loaded role's id, name, or description differ from the values last copied, which replaces the working copy; while those match, the edits stay (`RoleEditForm.razor`).
- Status: open

### Issue 2 — Severity: bug
- File: skills/tw-blazor/SKILL.md:282
- Description: The hook table says `On<C>Loaded` must store the whole Response, and the following paragraph says the store holds the fetched Response and does not keep flattened fields. `RoleState.OnGetRolesLoaded` assigns `RoleRows` from `response.Items`. `OnGetRoleLoaded` assigns `RoleSnapshot = response`, which is the edit-form snapshot point 1 describes. The same section's attribute lists `GetRoles`, `GetRole`, and `SetRolePermissions`. `RoleState` also lists `SetRoleAgentScopes`. Results say the example matches the role state. An agent copying the snippet drops a generated action set, and an agent obeying "no flattened fields" will rewrite the list hook.
- Suggestion: Limit the whole-response rule to the edit-form query (`RoleSnapshot` holds `GetRole.Response`; the store holds no unsaved form edits). Say a list hook may store the rows the page renders (`OnGetRolesLoaded`). Add `typeof(SetRoleAgentScopes)` to the attribute snippet.
- Status: open

### Issue 3 — Severity: suggestion
- File: skills/tw-web-api-contracts/references/mutability.md:90
- Description: Point 2 says the generated `Clone` is a `MemberwiseClone`, then new `List<T>`, `HashSet<T>`, `Dictionary<TKey, TValue>`, and rank-1 arrays, with `ICloneable` elements cloned, and point 3 calls that the same deep copy `From` uses. `documentation/developer/guides/edit-form-details-copy.md` says elements are shared except a reference type other than `string` that implements `ICloneable`, a null nullable collection stays null, a null non-nullable collection becomes empty, and any other collection type is assigned as-is. When the object does not implement `ICloneable`, State copies backing fields itself. `## Notes` do not record this difference. The card's "deep-copies" wording is what the skill repeats.
- Suggestion: Add those guide rules beside the collection sentence, and say State copies backing fields when the object does not implement `ICloneable`.
- Status: open
