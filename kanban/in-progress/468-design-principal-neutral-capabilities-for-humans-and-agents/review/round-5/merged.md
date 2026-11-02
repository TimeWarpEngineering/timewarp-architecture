# Round 5 — merged findings
**Date:** 2026-11-03
**Sources:** general

Re-review of the 2026-11-03 revision (`design.md` rewritten to the Decisions section; commits `903a7d384` and `f755d1167` after round 4's fix `65450aa43`). Prior IDs carried forward. Round 1 through round 4 files were not edited.

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 10 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 1 | 0 |

## Resolved prior

- M1 — bug — fixed. Steve's Q8 replaced per-id `DelegatedFrom` with one subject per token (`OnBehalfOf`, `SubjectPrincipalId`). The revision forbids a mixed token and says an agent that needs both subjects issues two tokens. That is the alternative M1 allowed. `grep` of `design.md` finds no `DelegatedFrom`.
- M2 — bug — fixed. Section 7.2 still copies the full `identity:read` expansion and skips a row only when that principal or role already has the id. It does not skip because the id is in the Principal bundle.
- M3 — bug — fixed. `DownloadFeedbackAttachment` still has its own 6.1 row with no attribute policy, and the handler-level list still names the `admin.profiles.read` gate.
- M4 — bug — fixed. The revision moves `Requires` onto task 5 and the capability-request approve and deny pins onto task 6. Task 5 depends on 4, task 4 depends on 2 and 3, task 6 depends on 5, and the closing paragraph matches those cells. Approve and deny still carry acting-as-self in scope and in a proof gate.
- M5 — bug — fixed. Q7 is decided: links stay agent to human. Section 5 keeps the three `PrincipalKind` checks and adds the Human check on revoke. Task 3 says the same.
- M6 — nit — fixed. Re-counted hosted `[EndpointAuthorize]` attributes: 34 human-only, 14 both, 5 agent-only (4 web plus `GetAgentBearerIdentity`). Section 1 still says that.
- M7 — suggestion — fixed. Q3, Q6, and Q18 are decided in section 10 and the body follows them: ceiling is what the grantor holds, `ReplyToFeedback` is `admin.feedback.manage` for any holder, and there is no browser-session rule.
- M8 — bug — fixed. Re-read `download-feedback-attachment-handler-application.cs`. Unlinked drafts (`FeedbackItemId` null) are owner-only before any permission check. Linked cross-owner read is `feedback.read` on the agent-token scheme and `admin.feedback.read` or `feedback.read` on the session schemes. Bytes-gone is `FeedbackProblems.AttachmentGone` (404, title "Attachment no longer available"). Section 6.1 states that order and the post-change single disjunction. The attribute has schemes and no `Policy`.
- M9 — bug — fixed. Task 5's download pins still expect 200 on another owner's linked attachment, 404 on another owner's unlinked draft, and 200 on the bearer's own unlinked draft. The task text keeps the section 6.1 order.

## Issues

### M10 — Severity: bug — Status: fixed
- File: kanban/in-progress/468-design-principal-neutral-capabilities-for-humans-and-agents/design.md:249
- Description: Example 3.1 has an administrator approve `demo.invoke` and says Developer holds that id today. `RolePermissionSeed.DefaultGrants` gives `demo.invoke` to no role. Developer holds `developer.access`, `developer.claims.read`, and self-service, and does not hold `admin.principals.manage`. Administrator's seed omits `demo.invoke`. `InMemoryRoleAgentScopeStore` starts with an empty list, and no migration inserts `demo:invoke`. Section 2.6 refuses a grant the grantor does not hold, so the example's approval cannot succeed on today's seeds.
- Suggestion: Say that no seeded role holds `demo.invoke`. The approver in the example must already hold `demo.invoke` and `admin.principals.manage`. Drop "Developer does".
- Source: general
- Disposition notes: Fixed 2026-11-03 (implementer-claude, fix pass). Example 3.1 step 3 now says no seeded role holds `demo.invoke` (`RolePermissionSeed.DefaultGrants` gives it to no role; the 466 role scope store starts empty; no migration inserts it), that the approver must already hold `admin.principals.manage` and `demo.invoke`, and that a host seeds it into Administrator first or the section 2.6 ceiling refuses the approval. "Developer does" is gone.

### M11 — Severity: bug — Status: fixed
- File: kanban/in-progress/468-design-principal-neutral-capabilities-for-humans-and-agents/design.md:132
- Description: Section 2.4 and section 11 question 4 say seeding `agent-chat.invoke` on Member and Administrator keeps chat for every signed-in human, with the Member backfill covering roleless humans. The two chat operations use `XaiChatDefaults.AuthenticatedPolicy` (any signed-in identity session). The resolver adds Member only when the stored role list is empty. A human whose only role is Developer or Operator can chat today and is outside both the seed and the backfill. Question 4 asks Steve to confirm the seed on that continuity claim.
- Suggestion: State the split in section 2.4 and in question 4. Roleless humans, Member, and Administrator keep chat. Developer-only and Operator-only humans can chat today and lose it unless those roles are seeded.
- Source: general
- Disposition notes: Fixed 2026-11-03. Section 2.4 states that today's chat policy admits any signed-in human and the resolver adds Member only on an empty role list, so roleless humans, Member, and Administrator keep chat while Developer-only and Operator-only humans lose it unless seeded. Section 11, Q4 restates the split and asks Steve to keep the Member and Administrator seed or add Developer and Operator. The section 5 seed row points at Q4.

### M12 — Severity: bug — Status: fixed
- File: kanban/in-progress/468-design-principal-neutral-capabilities-for-humans-and-agents/design.md:420
- Description: Section 6.1 marks `GetCredentials`, `AddAgentKey`, and `RenameCredential` acting-as-self. Section 6.2 and section 11 question 2 list `AddPasskey`, `StartAddExistingPasskey`, `CompleteAddExistingPasskey`, `AddAgentKey`, `RenameCredential`, and `RevokeCredential`, and omit `GetCredentials`. Those four missing operations are hosted today as human-and-agent with no mock scheme. `GetCredentials` is a read of the same shape. Task 5 applies section 6 as written, so the acting-as-self set is not one set.
- Suggestion: Leave `GetCredentials` permission-only. Add the four passkey and revoke operations to the 6.1 table as HA (no mock) pointing at the 6.2 row. Keep question 2 as the confirmation of that row.
- Source: general
- Disposition notes: Fixed 2026-11-03. Section 6.1 now has a permission-only `GetCredentials` row and a separate HA (no mock) row listing `AddPasskey`, `StartAddExistingPasskey`, `CompleteAddExistingPasskey`, `AddAgentKey`, `RenameCredential`, and `RevokeCredential` as acting-as-self pointing at 6.2. The 6.2 row and question 2 are unchanged except that question 2 names `GetCredentials` as the read that stays permission-only. Verified against the hosted attributes: all seven carry `IdentitySession,AgentToken` with no mock scheme.

## Duplicates / conflicts

- One general reviewer. M10, M11, and M12 are separate. No prior ID was reopened. M1's per-id provenance was replaced by the decided one-subject token, and that replacement matches Q8.
