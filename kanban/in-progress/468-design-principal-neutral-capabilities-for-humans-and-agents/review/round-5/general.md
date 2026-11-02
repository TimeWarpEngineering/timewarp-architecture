# Round 5 — general
**Date:** 2026-11-03
**Scope reviewed:** The 2026-11-03 revision of `design.md` and the task Results, against the task description and the Decisions section. Prior rounds stayed on the pre-decision draft. Scheme counts were re-counted from hosted `[EndpointAuthorize]` attributes (34 human-only, 14 both, 5 agent-only including the api host). The download handler, role and scope seeds, the chat policy, and the role resolver were re-read.

## Summary

The revision follows Steve's 2026-11-03 decisions: three sources, a Principal role, `OnBehalfOf` with one subject per token, session-only `agent-chat.invoke`, and acting-as-self on approve and grant operations. Section 10 records those answers. Section 11 asks four new questions. The download rule still matches the handler, and the 34 / 14 / 5 scheme counts match the contracts. Three claims in the new text do not match the code or the design's own ceiling and acting-as-self lists.

## Issues

### Issue 1 — Severity: bug
- File: kanban/in-progress/468-design-principal-neutral-capabilities-for-humans-and-agents/design.md:249
- Description: Example 3.1 has an administrator approve a request for `demo.invoke`, and the parenthetical says Developer holds that id today. `RolePermissionSeed.DefaultGrants` gives `demo.invoke` to no role. Developer receives `developer.access`, `developer.claims.read`, and `SelfServicePermissions`. Administrator receives admin ids, `settings.write`, the four feedback reviewer ids, and self-service, and does not receive `demo.invoke`. `InMemoryRoleAgentScopeStore` starts empty, and no migration inserts `demo:invoke`. Developer also lacks `admin.principals.manage`, so a Developer could not approve the request even if the scope were seeded. Section 2.6 allows a grant only when the grantor holds the id. The worked example completes an approval the ceiling refuses, and it cites a holder that does not exist.
- Suggestion: Say that no seeded role holds `demo.invoke`. Make the example's approver a principal who already holds both `demo.invoke` and `admin.principals.manage` (a direct grant or a role edit done first). Drop the claim that Developer holds it.
- Status: open

### Issue 2 — Severity: bug
- File: kanban/in-progress/468-design-principal-neutral-capabilities-for-humans-and-agents/design.md:132
- Description: Section 2.4 seeds `agent-chat.invoke` into Member and Administrator "so today's signed-in humans keep the chat." Section 11 question 4 repeats that every human who can chat today still can, because the Member backfill covers roleless humans. `CompleteAgentChat` and `GetAgentChatConfiguration` use `XaiChatDefaults.AuthenticatedPolicy`, which admits any signed-in identity session. `EffectiveRolesResolver` returns only the stored roles when a row exists; it adds Member only for a roleless human. A human whose only stored role is Developer or Operator can call the chat today and receives neither Member nor `agent-chat.invoke` under this seed. The backfill does not insert Member for a human who already has a role row. Question 4 asks Steve to confirm the seed on that false continuity claim.
- Suggestion: State who keeps chat and who loses it. Roleless humans are backfilled to Member and keep it. Humans who already hold Member or Administrator keep it. A human whose only role is Developer or Operator can chat today and will not, unless question 4 seeds those roles too. Use that wording in section 2.4 and in question 4.
- Status: open

### Issue 3 — Severity: bug
- File: kanban/in-progress/468-design-principal-neutral-capabilities-for-humans-and-agents/design.md:420
- Description: The credential rows disagree about which operations refuse an on-behalf token. Section 6.1 puts `GetCredentials`, `AddAgentKey`, and `RenameCredential` on one acting-as-self row. Section 6.2 and section 11 question 2 list `AddPasskey`, `StartAddExistingPasskey`, `CompleteAddExistingPasskey`, `AddAgentKey`, `RenameCredential`, and `RevokeCredential`, and they omit `GetCredentials`. The four passkey and revoke operations are hosted today, human-and-agent, with no mock scheme (`IdentitySession` + `AgentToken`), and the 6.1 change table never records them. `GetCredentials` is the same shape and is a read; question 18's approve-and-grant set does not name it. Task 5 updates every hosted contract per section 6, so the two lists send the implementer different acting-as-self sets.
- Suggestion: Keep `GetCredentials` permission-only on `credential.manage.self` (owner is the subject) and take acting-as-self off that 6.1 cell. Add a 6.1 row for `AddPasskey`, `StartAddExistingPasskey`, `CompleteAddExistingPasskey`, and `RevokeCredential` with Was HA (no mock), pointing at the existing 6.2 row. Leave question 2 as the confirmation of that 6.2 row.
- Status: open
