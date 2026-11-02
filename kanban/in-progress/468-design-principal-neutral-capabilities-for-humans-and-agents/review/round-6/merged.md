# Round 6 — merged findings
**Date:** 2026-11-03
**Sources:** general

Re-review of the round-5 fix (`4caeff5a6`) against round-5 `base.sha` `d83763a5547c4acef3dc6d2ef32b6b88ffe82a73`. Prior IDs carried forward. Round 1 through round 5 files were not edited.

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 10 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 1 | 0 |

## Resolved prior

- M1 — bug — fixed. Unchanged in this delta. The token still names one subject (`OnBehalfOf`, `SubjectPrincipalId`). `design.md` has no `DelegatedFrom`.
- M2 — bug — fixed. Unchanged in this delta. Section 7.2 still copies the full scope expansion and skips only a natural-key duplicate.
- M3 — bug — fixed. Unchanged in this delta. `DownloadFeedbackAttachment` still has its own 6.1 row with no attribute policy.
- M4 — bug — fixed. Unchanged in this delta. Task dependencies and the approve and deny acting-as-self pins are the same.
- M5 — bug — fixed. Unchanged in this delta. Links stay agent to human, and the kind checks stay.
- M6 — nit — fixed. Unchanged in this delta. Section 1 still says 34 human-only, 14 both, and 5 agent-only.
- M7 — suggestion — fixed. Unchanged in this delta. Section 10 still records the ceiling, `ReplyToFeedback`, and no browser-session rule.
- M8 — bug — fixed. Unchanged in this delta. Section 6.1 still puts the unlinked-draft owner-only gate before the linked disjunction.
- M9 — bug — fixed. Unchanged in this delta. Task 5 still expects 200 on the bearer's own unlinked draft.
- M10 — bug — fixed. Example 3.1 (design.md:254) says no seeded role holds `demo.invoke`. `RolePermissionSeed.DefaultGrants` gives it to none of Administrator, Member, Developer, or Operator. `SelfServicePermissions` and `AdminPermissions` omit it. `InMemoryRoleAgentScopeStore` starts with an empty map, and no migration mentions `demo:invoke`. The example requires the approver to hold `admin.principals.manage` and `demo.invoke`, and it says a host seeds `demo.invoke` into Administrator first or the ceiling refuses the approval. "Developer does" is gone.
- M11 — bug — fixed. Section 2.4 (design.md:133), the section 5 seed row, and question 4 state the same split. Today's chat policy is `XaiChatDefaults.AuthenticatedPolicy` on both chat contracts (identity session and mock identity session, no agent token). The resolver adds Member for an empty role list when the principal is missing or Human. A stored Developer-only or Operator-only list does not gain Member, so those humans can chat today and lose it unless question 4 seeds those roles. A bootstrap principal also gains Member and Administrator even with a stored role, and still keeps chat through that union. Roleless humans stay on the Member backfill.
- M12 — bug — fixed. Section 6.1 (design.md:429) has a permission-only `GetCredentials` row and a separate acting-as-self row for `AddPasskey`, `StartAddExistingPasskey`, `CompleteAddExistingPasskey`, `AddAgentKey`, `RenameCredential`, and `RevokeCredential`. Section 6.2's last row and question 2 name that same six, and question 2 says `GetCredentials` stays permission-only. Each of the seven contracts is `PermissionIds.CredentialManageSelf` with `IdentitySession` and `AgentToken` and no mock scheme. Their routes sit under `api/identity/credentials`.

## Issues

No new issues.

## Duplicates / conflicts

- One general reviewer. No prior ID was reopened. M10, M11, and M12 stay fixed and separate.
