# Round 2 — merged findings
**Date:** 2026-11-02
**Sources:** general

Re-review of the delta since round-1 `base.sha` `9769c06c195759fd9ec8ae8ada0f455fdb322bff`. Prior IDs carried forward. Round 1 files were not edited.

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 6 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: kanban/in-progress/468-design-principal-neutral-capabilities-for-humans-and-agents/design.md:152
- Description: A mixed token (own ids plus delegated ids) needs per-permission `on_behalf_of`, as example 3.3 and the first-party rule ("for the ids used") require. `IAgentTokenStore.Issue` takes one optional `onBehalfOf` for the whole token, which cannot represent that mix. First-party checks and audit then either reject the caller's own ids or accept a delegated id as the subject's own.
- Suggestion: Define a token shape with per-id delegator provenance and change `Issue` to match example 3.3. If mixed tokens are forbidden, say so and change the example.
- Source: general
- Disposition notes: Fixed 2026-11-02 (implementer-claude, fix pass). Re-verified round 2. Section 2.5 defines `TokenPermission(PermissionId, DelegatedFrom?)`, allows mixed tokens, and gives own-sources-first provenance. Section 5 `AgentScopes` row: `Issue` takes the list and no token-wide `onBehalfOf`. Token contracts, evaluator, api-host claims, 2.6 not-self, section 4, 3.3 step 2, the section 6 legend, and tasks 1 and 6 match. No `on_behalf_of` remains.

### M2 — Severity: bug — Status: fixed
- File: kanban/in-progress/468-design-principal-neutral-capabilities-for-humans-and-agents/design.md:428
- Description: Section 7.2 may skip migrated `identity:read` rows because `identity.read` and `agent-link.manage.self` are in the proposed default bundle. Question 2 leaves that bundle open. Skipping before the answer drops link capability that today's scope map grants.
- Suggestion: Always copy the seed expansion, and skip a row only when the chosen default already contains that id.
- Source: general
- Disposition notes: Fixed 2026-11-02. Re-verified round 2 against `AgentScopePermissionSeed`: `identity:read` expands to `identity.read` and `agent-link.manage.self`. Section 7.2 always copies the full expansion and skips only a natural-key duplicate on that principal or role. The section 5 seed row says the copy does not depend on Q2.

### M3 — Severity: bug — Status: fixed
- File: kanban/in-progress/468-design-principal-neutral-capabilities-for-humans-and-agents/design.md:369
- Description: `DownloadFeedbackAttachment` has schemes and no policy. The handler allows the filer, `feedback.read`, or `admin.feedback.read`. Section 2.4 requires one permission id, section 6.1 folds the operation into the feedback-reviewer permission list, and section 5 keeps the handler check while dropping the scheme branch. Those do not describe one rule. The same gap exists for handler-level `admin.profiles.read` redaction on principal reads, which section 6 does not mention.
- Suggestion: Give download its own row and state the disjunction that remains after schemes stop selecting kind. State that `admin.profiles.read` inside principal and feedback reads stays.
- Source: general
- Disposition notes: Fixed 2026-11-02 as a separate row, a handler-level list, and the `admin.profiles.read` gate. Re-verified round 2: the profiles gate matches `FeedbackOwnerNames` and the principal handlers, and today's download scheme split matches the handler. The stated post-change download rule does not; that remainder is M8.

### M4 — Severity: bug — Status: fixed
- File: kanban/in-progress/468-design-principal-neutral-capabilities-for-humans-and-agents/design.md:476
- Description: Approve and deny of a capability request are first-party in section 6.2, so they need task 4's `Requires` attribute. Task 5 depends on task 2 (task 3 only for the hint) and its scope does not mention first-party. The analyzer will not catch a missing `Requires` when every scheme is the default. The closing paragraph says tasks 5, 6, and 7 wait for tasks 3 and 4; the Depends-on cells for tasks 5 and 7 do not.
- Suggestion: Depend task 5 on task 4 and put first-party approve and deny in its scope and proof gates. Make the Depends-on cells match the paragraph.
- Source: general
- Disposition notes: Fixed 2026-11-02. Re-verified round 2. Task 5 depends on 2, 3, 4, carries `Requires = FirstParty` on approve and deny, and has proof gates for a delegated-id 403 and an analyzer test. Task 7 depends on 2, 3, 4. The closing paragraph matches the Depends-on cells (each of 5, 6, and 7 includes 3 and 4) and explains why the analyzer alone does not catch a missing `Requires`.

### M5 — Severity: bug — Status: fixed
- File: kanban/in-progress/468-design-principal-neutral-capabilities-for-humans-and-agents/design.md:327
- Description: Question 7 leaves agent-to-human kind checks open. Section 5 deletes the `PrincipalKind` checks in the request, approve, and deny handlers, and task 6 says "delete kind checks" with no tie to the answer. That settles question 7 as "any principal may delegate to any other".
- Suggestion: Keep or remove the checks according to question 7. Task 6 implements that answer.
- Source: general
- Disposition notes: Fixed 2026-11-02. Re-verified round 2. The section 5 agent-link row and task 6 tie the three `PrincipalKind` checks to Q7, including revoke if the link stays agent-to-human. Nothing in the delta settles Q7.

### M6 — Severity: nit — Status: fixed
- File: kanban/in-progress/468-design-principal-neutral-capabilities-for-humans-and-agents/design.md:24
- Description: "Both: 13" is 14 hosted human-and-agent contracts. Section 6 lists all 14. Results say all 34 human-only operations become permission-only; approve and deny stay first-party, and `SetRoleAgentScopes` is removed.
- Suggestion: Correct the count to 14 and the Results sentence so it matches section 6.
- Source: general
- Disposition notes: Fixed 2026-11-02. Re-verified round 2 against hosted `[EndpointAuthorize]` scheme assignments: 53 contracts, 34 human-only, 14 both, 5 agent-only (4 web plus the api-host sample). Section 1 says "Both: 14". The task.md Results sentence matches section 6: 31 of 34 human-only become permission-only, link approve and deny stay first-party, `SetRoleAgentScopes` is removed; 10 of 14 both become permission-only and 4 stay first-party.

### M7 — Severity: suggestion — Status: fixed
- File: kanban/in-progress/468-design-principal-neutral-capabilities-for-humans-and-agents/design.md:170
- Description: The ceiling is written as the meaning of the grant invariant while question 3 is open. `ReplyToFeedback` is assigned `admin.feedback.manage` in section 6.1 with no pointer to question 6. Results call the first-party mechanism an open question; section 10 does not ask it, and sections 2.4, 6.2, and task 4 settle it.
- Suggestion: Point the ceiling text and the `ReplyToFeedback` row at question 3 and question 6. Either add a question for first-party versus a narrower present-person list, or correct Results so first-party is settled.
- Source: general
- Disposition notes: Fixed 2026-11-02. Re-verified round 2. Section 2.6 rule 2 is "Ceiling (proposed, Q3)" and later uses of "ceiling" mean whichever reading Steve picks. The `ReplyToFeedback` row points at Q6. Section 2.4 and Q18 ask first-party versus a present person. task.md Results lists Q3, Q18, and Q1/Q2 as the open proposals.

### M8 — Severity: bug — Status: fixed
- File: kanban/in-progress/468-design-principal-neutral-capabilities-for-humans-and-agents/design.md:369
- Description: The download rule added for M3 does not match the handler it says it preserves. The section 6.1 row (line 369), the bullet at lines 376–380, and the section 5 Feedback row (line 329) say the surviving rule is owner, or `feedback.read`, or `admin.feedback.read`, and that a reviewer holding `feedback.read` and an administrator holding `admin.feedback.read` both pass. Those sentences also put `feedback.file.self` on the `[EndpointAuthorize]` attribute. The attribute runs first, so the caller must hold `feedback.file.self` and one arm of the disjunction. A principal holding only `feedback.read` or only `admin.feedback.read` never reaches the handler. Today's feedback-reviewer agent is that principal: `AgentScopePermissionSeed` expands `feedback:read` to `feedback.read` alone, and `feedback.file.self` is not an agent scope. The design adds `feedback.file.self` for agents only through the default bundle, which Q2 leaves open. `RolePermissionSeed` gives Administrator both ids, so seeded humans hide the gap. The handler also 404s a non-owner when `FeedbackItemId` is null, before the cross-owner check (`download-feedback-attachment-handler-application.cs` lines 69–76; Design region: an unlinked draft stays owner-only). The new one-disjunction text drops that gate.
- Suggestion: State one access rule that matches the handler. Keep the attribute from excluding callers the handler allows: either leave download without a single policy and say why it stays handler-only, or name the attribute permission as necessary and stop saying `feedback.read` alone passes. Keep the unlinked-draft owner-only gate in the surviving rule.
- Source: general
- Disposition notes: Fixed 2026-11-02 (implementer-claude, fix pass 3). Download keeps `[EndpointAuthorize]` with no `Policy`, today's shape minus the scheme list, and the handler is the whole rule. Section 2.4 states it as the one exception to "every protected operation names a permission id" and why (no single id covers the owner arm and both reviewer arms). The section 6.1 row names no attribute permission. The handler-level list restates the rule in handler order: unlinked draft owner-only before any permission check, linked attachment owner or `feedback.read` or `admin.feedback.read` against the credential, bytes-gone 404. Section 5 Feedback row matches and no longer ties agent access to the Q2 default. Task 4 scope and proof gates pin a `feedback.read`-only bearer (200 linked, 404 unlinked draft) and an `admin.feedback.read` session without `feedback.file.self`.

## Duplicates / conflicts

- One general reviewer. M8 is the remainder of M3's download rule after the fix; M3 stays `fixed` for the separate row and the profiles gate. No other overlaps.
