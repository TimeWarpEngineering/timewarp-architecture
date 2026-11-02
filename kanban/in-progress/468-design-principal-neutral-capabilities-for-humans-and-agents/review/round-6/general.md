# Round 6 — general
**Date:** 2026-11-03
**Scope reviewed:** Delta since round-5 `base.sha` `d83763a5547c4acef3dc6d2ef32b6b88ffe82a73` (fix commit `4caeff5a6`). The `design.md` hunks are section 2.4's chat paragraph, example 3.1, the section 5 seed row, the section 6.1 credential rows, and section 11 questions 2 and 4. Prior M1–M9 are outside those hunks. No product code in the diff.

## Summary

The fix makes the three round-5 claims match the code they cite. Example 3.1 no longer says a seeded role holds `demo.invoke`: `RolePermissionSeed.DefaultGrants` gives that id to no role, `InMemoryRoleAgentScopeStore` starts empty, and no migration inserts `demo:invoke`. The approver must already hold `admin.principals.manage` and `demo.invoke`, or the section 2.6 ceiling refuses the approval. Section 2.4 and question 4 state the chat split. `CompleteAgentChat` and `GetAgentChatConfiguration` use `XaiChatDefaults.AuthenticatedPolicy` (`identity-session-authenticated`, any signed-in identity session). `EffectiveRolesResolver` adds Member when the stored role list is empty and the row is missing or Human, and it uses a non-empty stored list as-is except for a bootstrap principal, who also receives Member and Administrator. A human whose only stored role is Developer or Operator can chat today and does not receive `agent-chat.invoke` from a Member and Administrator seed. Section 6.1, section 6.2, and question 2 name one acting-as-self credential set: `AddPasskey`, `StartAddExistingPasskey`, `CompleteAddExistingPasskey`, `AddAgentKey`, `RenameCredential`, and `RevokeCredential`. `GetCredentials` stays permission-only. All seven hosted contracts use `credential.manage.self` with `IdentitySession` and `AgentToken` and no mock scheme. No new findings. The Done-when sentence in the restatement still asks for a human-presence split and for every original product choice to stay open. That mismatch is already under Learnings. The revision-pass paragraph matches the Decisions section. No new misread.

## Issues

No new issues.
