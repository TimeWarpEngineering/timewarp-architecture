# Design principal-neutral capabilities for humans and agents

## Description

Today humans and agents get capabilities through two different systems:

- **Humans** hold **permissions** (`PermissionIds`, 23 ids) through roles.
- **Agents** request **scopes** (`AgentScopes`, 7 ids). `AgentScopePermissionSeed` maps each
  scope to permissions. Task 466 added an admin-approved scope allow-list per role and per
  principal, and made a roleless agent have no role while a roleless human is Member.
- **Endpoints** choose by credential type: most accept only a human session
  (`IdentitySession`), some only an agent token (`AgentToken`), a few both
  (`tw-web-api-contracts/references/endpoint-auth.md`).
- An optional agent ↔ human link exists (`features/agent-links`, task 205), approved by the
  human. It grants nothing today.

**Decision (Steve, 2026-11-02):** what a principal may do should not depend on whether it is a
human or an agent. Silicon or carbon, a principal is a principal. A principal that nobody
vouches for signs up with a minimal default and then gets more capability by requesting it
(an approver grants it), by paying for it, or through delegation from a principal that vouches
for it. An agent does not have to be linked to a human. If an agent pays for a capability, its
kind does not matter.

This task produces the **design** for that model. It writes no product code. Steve reviews the
design before any implementation task is filed.

### The model to design

1. **One vocabulary: permissions.** Agent scopes as a separate list go away. A token request
   names permission ids. The "Agent scopes" box on the role and Principal pages goes away, and
   roles mean the same for every principal.
2. **Four sources of a principal's permissions**, for every principal kind:
   - **roles**;
   - **direct grants** on the principal (task 466's principal grants, generalised to
     permissions);
   - **purchases** (metered / paid capability; see `features/metered-capability` and
     `source/libraries/timewarp-402`);
   - **delegation** from a linked principal, capped live by what the delegator holds now. An
     agent acting for a human gets at most the human's current permissions.
3. **One minimal default for every self-signed-up principal**, human or agent. Decide whether
   that is Member with a trimmed bundle or a new default role, and what it contains
   (self-identity, linking, filing feedback, …). This replaces task 466's kind-based Member
   exception.
4. **Endpoints authorize by permission, not by credential type.** An endpoint states the
   permission it needs, and any authenticated principal holding it passes, whatever its kind
   or sign-in. Keep a short, explicit list of operations that really need a present person
   (passkey ceremonies, a legal consent) as a per-operation requirement, not a blanket scheme
   rule.
5. **A token may carry a subset of its holder's permissions** (least privilege per token). This
   is a credential feature that applies to any principal with a bearer credential, not a
   separate vocabulary.

### Invariants the design must keep (species-neutral)

- **No self-escalation.** No principal grants itself a permission. Another authorized principal
  grants it, or a payment buys it. That covers an agent and a human approving their own link or
  grant alike.
- **Granting is itself a permission.** "No agent token ever holds `admin.*`" becomes "only a
  principal allowed to grant `admin.*` can grant it". Every grant and revoke is audited.
- **Delegation never exceeds the delegator**, evaluated at token issuance, so removing a
  permission from a human removes it from their delegates at the next token (tokens last
  `AgentTokenOptions.TokenLifetimeMinutes`).
- **Credential rules, not kind rules:** short bearer lifetime, rate limits and metering, and
  revocation apply to the credential type, whoever holds it.

### What the design document must cover

Write `design.md` in this task folder (not under `documentation/`):

- the model above, with a worked example for each acquisition path: an unvouched agent pays
  for a capability; an admin grants a feedback reviewer its permissions; a human delegates a
  subset to a linked agent; a human who signs themselves up gets the default;
- the request-for-more-capability flow: who sees a pending request, who may approve it, and
  how an outside agent learns about it from the OpenAPI document alone (task 331's goal);
- the effect on each existing piece: `AgentScopes` (in the `TimeWarp.Identity` package),
  `AgentScopePermissionSeed`, task 466's role and principal scope stores, EF migration, and
  admin UI; the token contracts; `JWTBearerAuth` OpenAPI text and the reviewer guide;
  `EffectiveRolesResolver`'s kind exception; the endpoint auth declarations and
  `endpoint-authorize-policy-agreement-analyzer`; the agent-link feature;
- which endpoints change from human-only to permission-only, which stay human-presence-only,
  and why for each of the second group;
- migration for existing tokens, stored scope grants, and generated apps (the template ships
  this, so say what an existing app's maintainer does);
- a proposed breakdown into implementation tasks with dependencies (scope, dependencies and
  proof gates only, no estimates);
- **Open questions** for Steve, one decision per item in plain words. Do not settle a product
  choice the description above leaves open; list it.

## Goal (implementer restatement)

Produce `design.md` in this task folder and change no product code. The document describes one
capability model that treats a human, an agent, and a service account the same way. There is one
vocabulary, permission ids, and the separate agent-scope list is removed. A principal holds the
union of four sources: roles, direct grants on the principal, purchases, and delegation from a
linked principal that is capped by what the delegator holds at token issuance. Every
self-signed-up principal starts from the same minimal default, whatever its kind. Endpoints name
a permission and accept any authenticated principal holding it; a short explicit list of
operations keeps a human-presence requirement per operation. A bearer token may carry a subset
of its holder's permissions.

The document must keep the invariants: no self-escalation, granting is itself an audited
permission, delegation never exceeds the delegator at issuance, and credential rules apply by
credential type rather than principal kind.

Done when `design.md` contains: the model with one worked example per acquisition path; the
request-for-more-capability flow including what an outside agent learns from OpenAPI alone; the
effect on each existing piece named in the description (including `AgentScopes` in the
`TimeWarp.Identity` package, task 466's stores and migration and UI, token contracts, OpenAPI
text, `EffectiveRolesResolver`, endpoint auth declarations and the agreement analyzer, agent
links); an endpoint table splitting permission-only from human-presence with a reason for each
of the latter; migration for existing tokens, stored scope grants, and generated apps; an
implementation task breakdown with dependencies and proof gates and no estimates; and an Open
questions list that leaves every product choice the description leaves open to Steve. The task
file gets `## Results` with a non-empty How to validate.

## Checklist

- [x] `design.md` covers the model, the four acquisition paths with worked examples, and the request flow
- [x] Effect on every existing piece listed above, including the `TimeWarp.Identity` package
- [x] Endpoint table: permission-only vs human-presence, with reasons
- [x] Migration for existing tokens, stored grants, and generated apps
- [x] Proposed implementation task breakdown with dependencies
- [x] Open questions listed for Steve; no product choices settled by the author
- [x] No product code changed

## Session

- Created: 431c3e64-803d-4442-8ca1-a6a431770511 (2026-11-02). While testing task 466, Steve asked
  why an agent can't hold any permission a human can. Discussion concluded the human/agent
  split should go: capabilities come from roles, grants, purchases, and delegation for every
  principal kind. Task 467 (seed a Feedback Reviewer role with agent scopes) was archived as moot.
- Design pass (2026-11-02), implementer-claude (Claude Fable 5.1). Surveyed the current model
  (PermissionIds, AgentScopes, the 466 stores, the evaluator, every hosted contract, agent links,
  metered capability, timewarp-402, tasks 104/182-006/205/331/378/466/467) and wrote `design.md`.
  No product code changed. `ganda repo audit` exited 0 with the existing `kanban-landed`
  advisory for other claims that have no worktree on this machine.
- Implement oracle: implemented by implementer-claude-fable (claude, vendor anthropic, model claude-fable-5-1, response model claude-sonnet-5-5), session a446a9be-4e1e-4a12-b6b4-e472a7f6e423, max-turns 500, Done — 2026-11-02T15:56:18Z
- Review oracle: grok session 01a19cfa-8b71-7823-acb1-a893538059fd (2026-11-02). Effort 2, roster general. Round 1 wrote `review/round-1/general.md` and `review/round-1/merged.md`. Open findings remain. Disposition is not written.
- Review oracle: review by reviewer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a19cfa-8b71-7823-acb1-a893538059fd, max-turns 120 — 2026-11-02T16:05:39Z
- Fix pass for round 1 (2026-11-02), implementer-claude (Claude Fable 5.1), implement-fix-2 oracle.
  Verified the scheme counts against the contracts (34 H, 5 A, 14 HA) and the download handler's
  disjunction before editing. All seven findings M1 to M7 fixed in `design.md`: per-id
  `TokenPermission(PermissionId, DelegatedFrom?)` replaces the token-wide `on_behalf_of` (M1);
  the 466 migration always copies the scope expansion and skips only natural-key duplicates
  (M2); `DownloadFeedbackAttachment` has its own 6.1 row and a "handler-level checks that
  survive" list covers its disjunction and the `admin.profiles.read` gate (M3); task 5 and 7
  depend on 2, 3, 4, task 5 carries first-party approve and deny with a proof gate (M4); the
  agent-link kind checks and task 6 follow Q7 (M5); "Both" is 14 and the Results count sentence
  is corrected (M6); the ceiling is marked proposed under Q3, `ReplyToFeedback` points at Q6,
  and Q18 asks first-party versus present person (M7). Statuses set to fixed on
  `review/round-1/merged.md`. No product code changed.
- Review round 2: grok session `01a19d09-e1b0-7c00-a784-e17012772ce4` (2026-11-02). Effort 2,
  roster general. Re-reviewed the delta since `9769c06c`. M1 through M7 re-verified fixed.
  New open finding M8 (download rule). Wrote `review/round-2/general.md` and
  `review/round-2/merged.md`. Disposition is not written.
- Fix pass for round 2 (2026-11-02), implementer-claude (Claude Fable 5.1), implement-fix-3 oracle.
  Re-read the download handler and both seeds before editing: `feedback:read` expands to
  `feedback.read` alone and `feedback.file.self` is not an agent scope, so any attribute
  permission would exclude a caller the handler allows. Fixed M8 in `design.md`: download keeps
  `[EndpointAuthorize]` with no `Policy` (its shape today, minus schemes), section 2.4 names it as
  the one exception to "every protected operation names a permission id", the 6.1 row names no
  attribute permission, the handler-level list restates the rule in handler order including the
  unlinked-draft owner-only gate, section 5 no longer ties agent download access to Q2, and task 4
  carries download pins. M8 set to fixed on `review/round-2/merged.md`. No product code changed.
- Review round 3: grok session `01a19d15-5d2f-7881-bde9-8558064255f4` (2026-11-02). Effort 1,
  roster general. Re-reviewed the delta since `074ea5a9c` (round-2 `base.sha`). M1 through M8
  re-verified fixed against the handler, seeds, and hosted `[EndpointAuthorize]` attributes.
  New open finding M9 (task 4 collapses the download handler to one disjunction and expects 404
  on the bearer's own unlinked draft). Wrote `review/round-3/general.md` and
  `review/round-3/merged.md`. Disposition is not written.
- Fix pass for round 3 (2026-11-02), implementer-claude (Claude Fable 5.1), implement-fix-4 oracle.
  Fixed M9 in `design.md` task 4: the scope says the handler keeps the section 6.1 order (an
  unlinked draft stays owner-only before any permission is consulted; only the linked cross-owner
  check becomes one disjunction of the credential), and the proof gate expects 200 on another
  owner's linked attachment, 404 on another owner's unlinked draft, and 200 on the bearer's own
  unlinked draft. M9 set to fixed on `review/round-3/merged.md`. No product code changed.
- Review round 4: grok session `01a19d1c-8f78-72d1-874c-5f02007032e7` (2026-11-02). Effort 1,
  roster general. Re-reviewed the delta since `00c2d430b` (round-3 `base.sha`, fix commit
  `65450aa43`). M1 through M8 are unchanged in that delta. M9 re-verified fixed against section
  6.1 and the download handler: task 4 keeps the section 6.1 order, and the proof gate expects
  200 on another owner's linked attachment, 404 on another owner's unlinked draft, and 200 on
  the bearer's own unlinked draft. No new findings. Wrote `review/round-4/general.md`,
  `review/round-4/merged.md`, and `review/disposition.md`. Outcome clean. The implementer
  restatement matches the task. No misread.

## Results

`design.md` in this folder is the deliverable. It has ten sections: where we are (a table of every
piece the design touches), the model (one vocabulary, four sources, one default, permission-only
endpoints, a credential subset), four worked examples (an unvouched agent buys `demo.invoke`, an
administrator grants a feedback reviewer, a human delegates two feedback ids to a linked agent, a
human signs up and gets the default), the request-for-more-capability flow with the OpenAPI text an
outside agent reads, the effect on every existing piece including the `TimeWarp.Identity` public
API break and release, an endpoint table, migration for tokens, stored grants, token requests, and
generated apps, an eight-task implementation breakdown with dependencies and proof gates, risks,
and eighteen open questions for Steve.

The endpoint table starts from today's scheme lists, verified against the contracts: 34 human-only,
5 agent-only (4 in web plus the api-host sample), and 14 accepting both. Of the 34 human-only, 31
become permission-only, link approve and deny stay first-party, and `SetRoleAgentScopes` is
removed. All 5 agent-only become permission-only. Of the 14 both, 10 become permission-only and 4
(passkey add and add-existing start and complete, credential revoke) stay first-party. New
first-party operations are link revoke and capability-request approve and deny. Every first-party
row names its reason. Handler-level resource checks are listed under 6.1. The
`admin.profiles.read` PII gate and the filer-only reads match the handlers. The attachment
download keeps no attribute policy, and the stated rule is the handler's: unlinked drafts are
owner-only, linked attachments open to the owner or a credential holding `feedback.read` or
`admin.feedback.read`.

Three design elements are proposals the description did not name and are listed as open questions
rather than settled: the grant ceiling (Q3), the first-party reading of "a present person" (Q18),
and the default bundle (Q1, Q2).

No product code changed. Only `task.md` and `design.md` in this folder are in the diff.

The implementer restatement matches the task. Service accounts are the third kind the resolver already special-cases, which the task's "every principal kind" and the kind exception already cover. No misread recorded.

### Review

Round 1, effort 2, roster general only. Artifacts: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`. Fix pass landed 2026-11-02. M1 through M7 are `fixed` on `review/round-1/merged.md`.

Round 2, same effort and roster, reviewed the delta since `9769c06c` (round-1 `base.sha`). Artifacts: `review/round-2/general.md`, `review/round-2/merged.md`. M1 through M7 re-verified fixed. M8 found that the download rule added in the round-1 fix did not match the handler it said it preserved (`feedback.file.self` on the attribute excluded a `feedback.read`-only reviewer, and the stated disjunction dropped the unlinked-draft owner-only gate). Fix pass 3 landed 2026-11-02: download keeps no attribute policy, and sections 2.4, 5, and 6.1 state the handler's rule in handler order. M8 is `fixed` on `review/round-2/merged.md`.

Round 3, effort 1, roster general only, reviewed the delta since `074ea5a9c` (round-2 `base.sha`, commit `00c2d430b`). Artifacts: `review/round-3/general.md`, `review/round-3/merged.md`. M1 through M8 re-verified fixed. Sections 2.4, 5, and 6.1 match the download handler. M9 found that task 4 still said the handler collapses to one disjunction, and its proof gate expected 404 on the bearer's own unlinked draft, which the owner passes. Fix pass 4 landed 2026-11-02: task 4 keeps the section 6.1 order and its pins expect 200 on the bearer's own unlinked draft. M9 is `fixed` on `review/round-3/merged.md`.

Round 4, effort 1, roster general only, reviewed the delta since `00c2d430b` (round-3 `base.sha`, commit `65450aa43`). Artifacts: `review/round-4/general.md`, `review/round-4/merged.md`, `review/disposition.md`. M1 through M8 are unchanged in that delta. M9 re-verified fixed: task 4 matches section 6.1 and the handler. No new findings.

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 7 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 1 | 0 |

Disposition is `clean` (`review/disposition.md`): four rounds, zero open, every finding `fixed`, no wontfix, no escalation. The implementer restatement matches the task; no misread under Learnings. Product tests were not run; the round-4 delta is design text, checked by reading the handler, `FeedbackProblems`, and section 6.1.

### How to validate

Smoke:

```bash
git diff --stat origin/master...HEAD -- . ':!kanban'
ls kanban/in-progress/468-design-principal-neutral-capabilities-for-humans-and-agents/
D=kanban/in-progress/468-design-principal-neutral-capabilities-for-humans-and-agents/design.md
grep -c '^## ' "$D"
sed -n '/^## 10\./,$p' "$D" | grep -c '^[0-9]*\. \*\*'
grep -c -iE 'on_behalf_of' "$D"
grep -n -E '^\| [567] \|' "$D"
grep -c 'feedback.file.self` on the attribute' "$D"
grep -n -A3 'DownloadFeedbackAttachment`, in handler order' "$D"
grep -c 'collapses to the one disjunction' "$D"
grep -c "200 on the bearer's own unlinked draft" "$D"
```

Expect: the first command prints nothing (no change outside `kanban/`). The folder holds `task.md`,
`design.md`, and `review/` beside the host-owned journal and progress log. The design has 10 `##`
sections and 18 numbered open questions in section 10. No `on_behalf_of` remains (provenance is
per id as `DelegatedFrom`). The Depends-on cells for tasks 5, 6, and 7 each include 3 and 4.
Section 1 says "Both: 14". Section 6.1 has its own `DownloadFeedbackAttachment` row that names no
attribute permission (the `feedback.file.self` count is 0), followed by a handler-level checks
list whose download entry puts the unlinked-draft owner-only gate before the `feedback.read` or
`admin.feedback.read` disjunction. Section 6.2 names a reason for every first-party row. Section 8 has
eight tasks, each with a Depends on column and proof gates and no estimate. Task 4 does not say
the handler "collapses to the one disjunction" (count 0), and its download pins expect 200 on
the bearer's own unlinked draft (count 1).

## Review

- Round 1: Blocked — open M1, M2, M3, M4, M5, M6, M7
- Round 2: Blocked — open M8
- Round 3: Blocked — open M9
- Round 4: Done
## Notes

Related: done 466 (admin-approved agent scopes; its scope list and kind-based Member exception
are what this design replaces), done 331 (OpenAPI reviewer guide), done 378 (scoped feedback
triage), 182-006 (agent grants only from token scopes), 205 (agent ↔ human link), 104 decision 3
(agents may pay and call without a human), archived 467.
