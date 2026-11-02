# Round 1 — general
**Date:** 2026-11-02
**Scope reviewed:** `design.md` and the task-468 edits to `task.md`, checked against the task description, `## Goal (implementer restatement)`, and the current permission, scope, and endpoint-auth code.

## Summary

The design is the right deliverable: one permission vocabulary, four sources, a shared default left to Steve, permission-named endpoints, a token subset, worked examples, a migration, and an eight-task breakdown. The restatement matches the task, including service accounts as the third kind `EffectiveRolesResolver` already special-cases. The survey of 23 permission ids, 7 agent scopes, and the `identity:read` expansion to `identity.read` plus `agent-link.manage.self` matches the code. The human-only count of 34 matches hosted `[EndpointAuthorize]` contracts. Two contracts in the design contradict themselves: a mixed delegated token cannot be stored with the `Issue` signature the design gives, and the migration drops `identity:read` rows on the assumption that an open default bundle will contain them. `DownloadFeedbackAttachment` has no route policy today and is not a single permission id. The task breakdown also schedules capability-request approval before the first-party attribute exists, and it deletes agent-link kind checks that question 7 still leaves open.

## Issues

### Issue 1 — Severity: bug
- File: kanban/in-progress/468-design-principal-neutral-capabilities-for-humans-and-agents/design.md:148
- Description: Section 2.5 and example 3.3 require per-permission delegation provenance. A token may mix the caller's own ids with ids delegated from one link, and `on_behalf_of` is recorded on the delegated ids only ("for the ids used" in section 6.2). Section 5's store API is a single optional `onBehalfOf` on `IAgentTokenStore.Issue(principalId, credentialId, permissions, lifetime, onBehalfOf?)`. One flag cannot say which of the token's ids were delegated. A mixed token from example 3.3 (`identity.read` own, `feedback.read` and `feedback.note` delegated) would either mark the whole token as on behalf of the human, so the agent's own first-party calls fail, or drop provenance, so a delegated id passes a first-party check.
- Suggestion: State the token shape that example 3.3 needs. Record delegated ids separately from own ids (a map of permission id to delegator, or two lists) and define the first-party check against that map. Make `Issue` take that shape. If a token must be entirely own or entirely delegated, say so and change example 3.3, which currently mixes them.
- Status: open

### Issue 2 — Severity: bug
- File: kanban/in-progress/468-design-principal-neutral-capabilities-for-humans-and-agents/design.md:388
- Description: Section 7.2 says migrated `identity:read` rows become `identity.read` plus `agent-link.manage.self` and "may be skipped" because both are in the default bundle. That expansion matches `AgentScopePermissionSeed`. The bundle is not decided: question 2 asks Steve whether a self-signed-up principal gets those ids. Skipping the rows unconditionally drops today's link permission for every principal who held `identity:read` if Steve removes `agent-link.manage.self` from the default.
- Suggestion: Copy the expanded rows into `principal_permission_grants` (and role permissions) always, and skip a row only when the chosen default bundle already contains that id. Say that the skip list is computed from Steve's answer to question 2, not from the proposal in section 2.3.
- Status: open

### Issue 3 — Severity: bug
- File: kanban/in-progress/468-design-principal-neutral-capabilities-for-humans-and-agents/design.md:347
- Description: `DownloadFeedbackAttachment` is the only hosted authorize attribute with schemes and no `Policy`. Its Design region is an owner-or-read check: the filer's `feedback.file.self`, `feedback.read`, and `admin.feedback.read` on a human session, with agents not evaluated under the human schemes. Section 2.4 says every protected operation names one permission id. Section 6.1 puts this operation on the `feedback.read / .note / .severity / .status` row. Section 5 says the handler check stays and the scheme branch goes. Those three statements do not pick one behavior. Stamping only `feedback.read` locks out a filer who has `feedback.file.self` and not `feedback.read`. Dropping the scheme branch without naming `admin.feedback.read` also changes who can download.
- Suggestion: Give this operation its own row. State the rule that remains after the scheme split goes away (filer, or `feedback.read`, or `admin.feedback.read`, any credential), and state that it is an exception to one policy id, enforced in the handler, with the attribute carrying whatever marker the analyzer will accept. Name `admin.profiles.read` the same way if other handlers keep a second gate the route policy does not express. ListPrincipals and GetPrincipal still redact profile fields on `admin.profiles.read` inside the handler; section 6 does not say that check stays.
- Status: open

### Issue 4 — Severity: bug
- File: kanban/in-progress/468-design-principal-neutral-capabilities-for-humans-and-agents/design.md:430
- Description: Section 6.2 makes `ApproveCapabilityRequest` and `DenyCapabilityRequest` first-party, which needs the `Requires` attribute introduced in task 4. Task 5 creates those operations and its Depends-on cell is "2 (3 for the hint)", not task 4. Task 5's scope does not mention `Requires = FirstParty`. The new analyzer flags `AuthenticationSchemes` without `Requires`, and does not flag a missing `Requires` when the generator emits every scheme by default, so a task 5 that lands before or beside task 4 can ship grant approval on a delegated token. The paragraph under the table says tasks 5, 6, and 7 start after 3 and 4; the Depends-on cells for 5 and 7 do not.
- Suggestion: Make task 5 depend on task 4, and put first-party on approve and deny, with the section 6.2 reasons, in task 5's scope and proof gates. Align the Depends-on cells with the paragraph, or change the paragraph so task 7 may start from task 2 when only the guide text waits on task 3.
- Status: open

### Issue 5 — Severity: bug
- File: kanban/in-progress/468-design-principal-neutral-capabilities-for-humans-and-agents/design.md:308
- Description: Question 7 asks whether a link stays "only an Agent may request, only a Human may delegate" or any principal may delegate to any other. Those rules are the `PrincipalKind` checks in the request, approve, and deny handlers (request also requires the target to be Human). Section 5 deletes those checks and leaves only the type name to question 7. Task 6's scope says "delete kind checks" with no dependency on the answer. Deleting the checks is the second option. The first option cannot be implemented afterward without putting the checks back.
- Suggestion: Say the checks stay or go with question 7, the same way section 2.3 leaves the default role shape open. Task 6 should implement the answer, not delete the checks unconditionally. The worked example of a human delegating to a linked agent still stands either way.
- Status: open

### Issue 6 — Severity: nit
- File: kanban/in-progress/468-design-principal-neutral-capabilities-for-humans-and-agents/design.md:24
- Description: Section 1 says "Both: 13". Hosted contracts with both human-session and agent-token schemes are 14: add-agent-key, add-passkey, start-add-existing-passkey, complete-add-existing-passkey, revoke-credential, rename-credential, get-credentials, list-agent-human-links, list-feedback, get-feedback-for-admin, add-feedback-note, set-feedback-severity, change-feedback-status, download-feedback-attachment. Section 6 accounts for all 14 (10 permission-only, 4 first-party). The human-only count of 34 and "agent-only: 4 plus the api-host sample" match the code. `task.md` Results says the 34 human-only operations become permission-only. Two of them stay first-party (approve and deny link) and `SetRoleAgentScopes` is removed rather than converted. Section 6 is the accurate inventory.
- Suggestion: Change "Both: 13" to 14. In Results, say 31 of the 34 human-only operations become permission-only, approve and deny stay first-party, and `SetRoleAgentScopes` is removed.
- Status: open

### Issue 7 — Severity: suggestion
- File: kanban/in-progress/468-design-principal-neutral-capabilities-for-humans-and-agents/design.md:269
- Description: Question 3 and question 6 are open, but the body states one side as the meaning of the model. Section 4 says the ceiling is what "only a principal allowed to grant `admin.*` can grant it" means in code. Section 6.1 assigns `ReplyToFeedback` to `admin.feedback.manage` with no pointer to question 6, while agent chat in the same table is marked "section 10". Results say the first-party mechanism is an open question. Section 10 has no item for it. Sections 2.4, 6.2, and task 4 settle `Requires = FirstParty`.
- Suggestion: Mark the ceiling paragraph and the `ReplyToFeedback` row as the proposal pending question 3 and question 6, the way agent chat points at question 5. Either add an open question for first-party versus a narrower present-person list, or change Results so first-party is a settled mechanism Steve is reviewing, not an unanswered question.
- Status: open
