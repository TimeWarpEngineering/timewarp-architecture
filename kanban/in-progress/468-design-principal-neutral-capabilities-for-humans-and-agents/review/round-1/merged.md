# Round 1 — merged findings
**Date:** 2026-11-02
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 5 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: kanban/in-progress/468-design-principal-neutral-capabilities-for-humans-and-agents/design.md:148
- Description: A mixed token (own ids plus delegated ids) needs per-permission `on_behalf_of`, as example 3.3 and the first-party rule ("for the ids used") require. `IAgentTokenStore.Issue` takes one optional `onBehalfOf` for the whole token, which cannot represent that mix. First-party checks and audit then either reject the caller's own ids or accept a delegated id as the subject's own.
- Suggestion: Define a token shape with per-id delegator provenance and change `Issue` to match example 3.3. If mixed tokens are forbidden, say so and change the example.
- Source: general
- Disposition notes: Fixed 2026-11-02 (implementer-claude, fix pass). Section 2.5 defines `TokenPermission(PermissionId, DelegatedFrom?)`, allows mixed tokens, and gives own-sources-first provenance. Section 5 `AgentScopes` row: `Issue(principalId, credentialId, IReadOnlyList<TokenPermission>, lifetime)`, no token-wide `onBehalfOf`. Token contracts, evaluator, api-host claims, 2.6 not-self, section 4, 3.3 step 2, the 6 legend, and tasks 1 and 6 updated to per-id provenance.

### M2 — Severity: bug — Status: fixed
- File: kanban/in-progress/468-design-principal-neutral-capabilities-for-humans-and-agents/design.md:388
- Description: Section 7.2 may skip migrated `identity:read` rows because `identity.read` and `agent-link.manage.self` are in the proposed default bundle. Question 2 leaves that bundle open. Skipping before the answer drops link capability that today's scope map grants.
- Suggestion: Always copy the seed expansion, and skip a row only when the chosen default already contains that id.
- Source: general
- Disposition notes: Fixed 2026-11-02. Section 7.2 always copies the full seed expansion and skips only natural-key duplicates on the same principal or role, never on the expected default bundle; the `AgentScopePermissionSeed` row in section 5 says the copy does not depend on Q2.

### M3 — Severity: bug — Status: fixed
- File: kanban/in-progress/468-design-principal-neutral-capabilities-for-humans-and-agents/design.md:347
- Description: `DownloadFeedbackAttachment` has schemes and no policy. The handler allows the filer, `feedback.read`, or `admin.feedback.read`. Section 2.4 requires one permission id, section 6.1 folds the operation into the feedback-reviewer permission list, and section 5 keeps the handler check while dropping the scheme branch. Those do not describe one rule. The same gap exists for handler-level `admin.profiles.read` redaction on principal reads, which section 6 does not mention.
- Suggestion: Give download its own row and state the disjunction that remains after schemes stop selecting kind. State that `admin.profiles.read` inside principal and feedback reads stays.
- Source: general
- Disposition notes: Fixed 2026-11-02. `DownloadFeedbackAttachment` has its own 6.1 row (`feedback.file.self` on the attribute, then owner or `feedback.read` or `admin.feedback.read`). A 'handler-level checks that survive the scheme split' list after the 6.1 table states the download disjunction, the `admin.profiles.read` PII gate in principal and feedback admin reads, and filer-only reads. The section 5 Feedback row matches.

### M4 — Severity: bug — Status: fixed
- File: kanban/in-progress/468-design-principal-neutral-capabilities-for-humans-and-agents/design.md:430
- Description: Approve and deny of a capability request are first-party in section 6.2, so they need task 4's `Requires` attribute. Task 5 depends on task 2 (task 3 only for the hint) and its scope does not mention first-party. The analyzer will not catch a missing `Requires` when every scheme is the default. The closing paragraph says tasks 5, 6, and 7 wait for tasks 3 and 4; the Depends-on cells for tasks 5 and 7 do not.
- Suggestion: Depend task 5 on task 4 and put first-party approve and deny in its scope and proof gates. Make the Depends-on cells match the paragraph.
- Source: general
- Disposition notes: Fixed 2026-11-02. Task 5 depends on 2, 3, 4, carries `Requires = FirstParty` on approve and deny in scope, and has proof gates for a delegated-id 403 and an analyzer test. Task 7 depends on 2, 3, 4 with the reason stated. The closing paragraph matches the Depends-on cells and explains why the analyzer alone does not catch a missing `Requires`.

### M5 — Severity: bug — Status: fixed
- File: kanban/in-progress/468-design-principal-neutral-capabilities-for-humans-and-agents/design.md:308
- Description: Question 7 leaves agent-to-human kind checks open. Section 5 deletes the `PrincipalKind` checks in the request, approve, and deny handlers, and task 6 says "delete kind checks" with no tie to the answer. That settles question 7 as "any principal may delegate to any other".
- Suggestion: Keep or remove the checks according to question 7. Task 6 implements that answer.
- Source: general
- Disposition notes: Fixed 2026-11-02. Section 5 agent-link row and task 6 scope tie the three `PrincipalKind` checks to Q7: kept (and added to revoke) if the link stays agent-to-human, deleted with the entity renamed if generalised. Nothing settles Q7.

### M6 — Severity: nit — Status: fixed
- File: kanban/in-progress/468-design-principal-neutral-capabilities-for-humans-and-agents/design.md:24
- Description: "Both: 13" is 14 hosted human-and-agent contracts. Section 6 lists all 14. Results say all 34 human-only operations become permission-only; approve and deny stay first-party, and `SetRoleAgentScopes` is removed.
- Suggestion: Correct the count to 14 and the Results sentence so it matches section 6.
- Source: general
- Disposition notes: Fixed 2026-11-02. Verified against the contracts: 34 H, 5 A (4 web plus api host), 14 HA. Section 1 says 'Both: 14'. The task.md Results sentence now gives the split: 31 of 34 human-only become permission-only, link approve and deny stay first-party, `SetRoleAgentScopes` is removed; 10 of 14 both become permission-only and 4 stay first-party.

### M7 — Severity: suggestion — Status: fixed
- File: kanban/in-progress/468-design-principal-neutral-capabilities-for-humans-and-agents/design.md:269
- Description: The ceiling is written as the meaning of the grant invariant while question 3 is open. `ReplyToFeedback` is assigned `admin.feedback.manage` in section 6.1 with no pointer to question 6. Results call the first-party mechanism an open question; section 10 does not ask it, and sections 2.4, 6.2, and task 4 settle it.
- Suggestion: Point the ceiling text and the `ReplyToFeedback` row at question 3 and question 6. Either add a question for first-party versus a narrower present-person list, or correct Results so first-party is settled.
- Source: general
- Disposition notes: Fixed 2026-11-02. Section 2.6 rule 2 is 'Ceiling (proposed, Q3)' and says later uses of 'ceiling' mean whichever reading Steve picks; section 4 points at Q3 too. The `ReplyToFeedback` row in 6.1 and example 3.2 point at Q6. Section 2.4 and new Q18 ask first-party versus a present person; task.md Results now lists Q3, Q18, and Q1/Q2 as the open proposals.

## Duplicates / conflicts

- One general reviewer. No overlaps to collapse.
