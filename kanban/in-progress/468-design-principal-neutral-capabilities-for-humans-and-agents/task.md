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

## Decisions on the open questions (Steve, 2026-11-03)

Revise `design.md` to these. Replace section 10 with a "Decided" list that records each answer,
and keep an "Open questions" list only for anything new the revision raises.

- **Purchases are not a source.** A grant is a grant; the access check never asks how it was
  obtained. Payment, when it comes, is an automated grantor in a billing component that creates
  and removes ordinary grants. x402 is pay-per-call (credits) and does not create grants. Tiers
  are rate limits. Sources are now **roles, direct grants, and delegation**. Remove the purchase
  worked example (3.1) or recast it as "an admin grants an unvouched agent a capability". Drop
  the trust-tier and purchasable-set material (old Q10, Q11) as out of scope.
- **Q1:** a new default role named **Principal** holds the default bundle. Member stays an
  ordinary granted role.
- **Q2:** the default bundle is the section 2.3 proposal **without `feedback.file.self`** (sign-up
  spam). `credential.manage.self` and the rest stay.
- **Q3:** a grantor may grant only what it holds. No `grant.<prefix>` permissions.
- **Q4:** revocation takes effect at the next token. Tokens carry their list; no per-request
  re-check. Start there; per-request evaluation is a later option.
- **Q5:** agent chat (`CompleteAgentChat`, `GetAgentChatConfiguration`) spends the site's xAI key.
  It gets its own permission, outside the Principal bundle, and is not reachable with a bearer
  token from the API. Bring-your-own-key use is unaffected.
- **Q6:** reply to the filer is `admin.feedback.manage` for any principal that holds it. An agent
  holding it may reply.
- **Q7:** links stay agent → human only. An agent that wants a sub-agent mints a key with a
  smaller permission set and hands it over; the app does not model that.
- **Q8:** a linked agent **acts as the human** on the human's own resources. That is the point of
  the link, not a separate design. Each token request names who it acts as: **as itself** (its own
  grants) or **on behalf of its linked human** (the human's delegated subset, capped live by what
  the human holds). Every audited action records both the **actor** (the agent) and the
  **subject** (the human).
- **Q9:** no transitive delegation.
- **Q12:** requests are all-or-nothing. Asking for anything the requester may not ask for refuses
  the whole request.
- **Q13:** the approver is an admin (`admin.principals.manage` plus the ceiling). No separate
  approver role for now.
- **Q14:** no alias period. The token request switches from `scopes` to `permissions` at once;
  the old field returns a 400 naming the new one.
- **Q15:** the token request's `permissions` list is required.
- **Q16:** no subset for browser sessions. Subsets are for bearer tokens only.
- **Q17:** add `admin.audit.read`. Keep the audit log forever; no pruning in the template.
- **Q18:** no browser-session rule. Ask-AI approvals need nothing special (agents don't hold those
  permissions), and confirm-before-delete is screen UX, not API. Approve and grant operations
  (link approve/deny, capability request approve/deny, permission grants) **refuse a token acting
  on someone's behalf**, so a linked agent cannot approve its own escalation. A principal acting
  as itself, human or agent, may approve within its ceiling.

## Goal (implementer restatement)

Produce `design.md` in this task folder and change no product code. The document describes one
capability model that treats a human, an agent, and a service account the same way. There is one
vocabulary, permission ids, and the separate agent-scope list is removed. A principal holds the
union of three sources: roles, direct grants on the principal, and delegation from a linked
human that is capped by what the human holds at token issuance (Steve, 2026-11-03: purchases are
not a source; payment is a future automated grantor). Every self-signed-up principal starts from
the same minimal default, a new Principal role, whatever its kind. Endpoints name a permission
and accept any authenticated principal holding it; a short explicit list of approve and grant
operations refuses a token acting on someone else's behalf, and there is no browser-session
rule. A bearer token names who it acts as, itself or its linked human, and carries a subset of
that subject's permissions.

Revision pass (2026-11-03): rewrite `design.md` to Steve's answers on all eighteen questions,
replace section 10 with a Decided list, and keep an open-questions list only for what the
revision itself raises.

Fix pass for review round 5 (2026-11-03): make the design's claims match today's seeds and
hosted attributes. The `demo.invoke` example must not assume a role holds an id no seed grants;
the chat seed must say which humans keep chat and which lose it; and the acting-as-self
credential set must be the same list in section 6.1, section 6.2, and question 2.

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

- [x] `design.md` revised to the 2026-11-03 decisions (sources, Principal role, act-as on the token, delegated tokens refused for approvals)

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
- Installed ganda changed during this walk: was 50296336 bytes 970fac827e28467d47743c6e9cdca8036036a6be2f0b3305a50018f5b8cf54af; now 50312784 bytes 50172fe6a7b99392943130486d96ba3649903cf8cf43e50ad96f1d4a64dbb327

- Decisions: 431c3e64-803d-4442-8ca1-a6a431770511 (Steve, 2026-11-03). All open questions answered;
  revision pass on the same PR (#625).
- Revision pass (2026-11-03), implementer-claude (Claude Fable 5.1), implement oracle. Re-read
  `RolePermissionSeed` (Member is `SelfServicePermissions`, which includes `feedback.file.self`),
  `PermissionIds`, the `AddAgentKey` command, and the `CompleteAgentChat` attribute before
  rewriting. `design.md` now follows every 2026-11-03 decision: three sources (purchases removed,
  payment is a future automated grantor, metering unchanged), a Principal role held by every
  principal as a floor with Member backfilled for existing roleless humans, `agent-chat.invoke`
  as a session-only id that token issuance refuses, `ReplyToFeedback` permission-only, links
  stay agent to human with the kind checks kept, the token names `OnBehalfOf` and carries
  `SubjectPrincipalId` (the per-id `DelegatedFrom` from the round-1 fix is gone, since one token
  acts as one subject), ownership checks compare the subject and audit records actor and
  subject, all-or-nothing requests, admin approver, no alias period, required `permissions`,
  no session subset, `admin.audit.read` and `ListAuthorizationAudit`, and
  `Requires = ActingAsSelf` on link, request, grant, and credential operations instead of
  first-party. Section 10 is the Decided list (17 entries); section 11 holds the four questions
  the revision raised (session-only mechanism, credential operations on behalf, a per-key cap for
  handed-over keys, which roles seed the chat id). The breakdown is seven tasks. No product code
  changed.
- Implement oracle: implemented by implementer-claude-fable (claude, vendor anthropic, model claude-fable-5-1, response model claude-fable-5-1), session 7e4128ba-e3e9-4f60-91ea-4ba271e87bce, max-turns 500, Done — 2026-11-02T17:46:04Z
- Review round 5: grok session `01a19d5f-0286-7340-befc-d3c369397937` (2026-11-03). Effort 3, roster general, turn cap 200. Re-reviewed the 2026-11-03 revision of `design.md`. M1 through M9 stay fixed. New open findings M10, M11, M12. Wrote `review/round-5/general.md` and `review/round-5/merged.md`. Disposition is not closed.
- Review oracle: review by reviewer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a19d5f-0286-7340-befc-d3c369397937, max-turns 200, artifacts general.md, merged.md — 2026-11-02T17:56:07Z
- Fix pass for round 5 (2026-11-03), implementer-claude (Claude Fable 5.1), implement-fix-2 oracle.
  Re-verified each finding before editing: `RolePermissionSeed.DefaultGrants` gives `demo.invoke`
  to no role and `InMemoryRoleAgentScopeStore` starts empty; `XaiChatDefaults.AuthenticatedPolicy`
  admits any signed-in session and `EffectiveRolesResolver` adds Member only on an empty role
  list; all seven credential contracts carry `IdentitySession,AgentToken` with no mock scheme.
  Fixed M10 (example 3.1 says no seeded role holds `demo.invoke`, the approver must already hold
  it and `admin.principals.manage`, and a host seeds it into Administrator first or the ceiling
  refuses), M11 (section 2.4, the section 5 seed row, and question 4 state that roleless, Member,
  and Administrator humans keep chat while Developer-only and Operator-only humans lose it unless
  seeded), and M12 (section 6.1 has a permission-only `GetCredentials` row and a separate
  acting-as-self row for the six passkey, key, rename, and revoke operations; question 2 names
  `GetCredentials` as the read that stays permission-only). Statuses set to fixed on
  `review/round-5/merged.md`. No product code changed.

## Results

`design.md` in this folder is the deliverable, revised 2026-11-03 to Steve's decisions. It has
eleven sections: where we are (a table of every piece the design touches), the model (one
vocabulary, three sources, the Principal role as a floor for every principal, permission-only
endpoints, a bearer token that names who it acts as and carries a subset), four worked examples
(an administrator who already holds `demo.invoke`, which no seed grants, approves an unvouched
agent's request for it, an administrator grants a feedback reviewer, a human delegates `feedback.file.self` and `profile.read` to a linked
agent that then files feedback as the human, a human signs up and gets the Principal bundle), the
request-for-more-capability flow with the OpenAPI text an outside agent reads, the effect on every
existing piece including the `TimeWarp.Identity` public API break and release, an endpoint table,
migration for tokens, stored grants, the Member backfill, token requests, and generated apps, a
seven-task implementation breakdown with dependencies and proof gates, risks, the Decided list
(section 10, one entry per answered question plus Purchases), and four open questions the
revision raised (section 11).

What changed against the first draft: purchases are not a source and the purchase slice, store,
catalog, and `TimeWarp.402` changes are gone; the default is a new Principal role held by every
principal, with `feedback.file.self` left in Member and a migration that backfills Member for
every roleless human; the token request names `OnBehalfOf` and the grant carries
`SubjectPrincipalId`, replacing the per-id `DelegatedFrom` provenance because one token acts as
one subject; ownership and `*.self` checks compare the subject and audit rows carry actor and
subject; `Requires = ActingAsSelf` replaces first-party and applies to link approve, deny, and
revoke, capability-request approve and deny, the three grant setters, and the credential
operations; `agent-chat.invoke` is a session-only id that issuance refuses; `admin.audit.read`
and `ListAuthorizationAudit` are added; requests are all-or-nothing; there is no alias period and
`permissions` is required.

The endpoint table starts from today's scheme lists, verified against the contracts: 34 human-only,
5 agent-only (4 in web plus the api-host sample), and 14 accepting both. Every one of them becomes
permission-only; `SetRoleAgentScopes` is removed. The acting-as-self requirement sits on top of
the permission for the section 6.2 set, and every row there names its reason. Handler-level
resource checks are listed under 6.1. The `admin.profiles.read` PII gate and the filer-only
reads match the handlers. The attachment download keeps no attribute policy, and the stated rule
is the handler's: unlinked drafts are owner-only, linked attachments open to the owner or a
credential holding `feedback.read` or `admin.feedback.read`, where owner means the subject.

No product code changed. Only `task.md` and `design.md` in this folder are in the diff.

Service accounts are the third kind the resolver already special-cases, which the task's "every principal kind" and the kind exception already cover. The restatement's Done-when sentence still asks for a human-presence split and for every original product choice to stay open. That sentence mismatches the Decisions section. The design follows the Decisions section. The leftover is recorded under Learnings.

### Review

Round 1, effort 2, roster general only. Artifacts: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`. Fix pass landed 2026-11-02. M1 through M7 are `fixed` on `review/round-1/merged.md`.

Round 2, same effort and roster, reviewed the delta since `9769c06c` (round-1 `base.sha`). Artifacts: `review/round-2/general.md`, `review/round-2/merged.md`. M1 through M7 re-verified fixed. M8 found that the download rule added in the round-1 fix did not match the handler it said it preserved (`feedback.file.self` on the attribute excluded a `feedback.read`-only reviewer, and the stated disjunction dropped the unlinked-draft owner-only gate). Fix pass 3 landed 2026-11-02: download keeps no attribute policy, and sections 2.4, 5, and 6.1 state the handler's rule in handler order. M8 is `fixed` on `review/round-2/merged.md`.

Round 3, effort 1, roster general only, reviewed the delta since `074ea5a9c` (round-2 `base.sha`, commit `00c2d430b`). Artifacts: `review/round-3/general.md`, `review/round-3/merged.md`. M1 through M8 re-verified fixed. Sections 2.4, 5, and 6.1 match the download handler. M9 found that task 4 still said the handler collapses to one disjunction, and its proof gate expected 404 on the bearer's own unlinked draft, which the owner passes. Fix pass 4 landed 2026-11-02: task 4 keeps the section 6.1 order and its pins expect 200 on the bearer's own unlinked draft. M9 is `fixed` on `review/round-3/merged.md`.

Round 4, effort 1, roster general only, reviewed the delta since `00c2d430b` (round-3 `base.sha`, commit `65450aa43`). Artifacts: `review/round-4/general.md`, `review/round-4/merged.md`. M1 through M8 are unchanged in that delta. M9 re-verified fixed: task 4 matches section 6.1 and the handler. No new findings. That round closed the pre-decision draft.

Round 5, effort 3, roster general only, turn cap 200, reviewed the 2026-11-03 revision (`903a7d384`, `f755d1167`) against the task description and the Decisions section. Artifacts: `review/review-framework.md`, `review/round-5/general.md`, `review/round-5/merged.md`, `review/disposition.md`. M1 through M9 stay fixed. M10: example 3.1 approves `demo.invoke` and says Developer holds it; no seeded role does, and the ceiling refuses the approval. M11: seeding `agent-chat.invoke` on Member and Administrator does not keep chat for every signed-in human; Developer-only and Operator-only humans can chat today under `XaiChatDefaults.AuthenticatedPolicy`. M12: section 6.1's acting-as-self credential row includes `GetCredentials` and omits four hosted passkey and revoke operations that section 6.2 and question 2 list. Fix pass landed 2026-11-03: example 3.1 no longer assumes a role holds `demo.invoke`, section 2.4 and question 4 state the chat split by role, and section 6.1 lists the six acting-as-self credential operations with `GetCredentials` permission-only. M10, M11, and M12 are `fixed` on `review/round-5/merged.md`.

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 10 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 1 | 0 |

Disposition is not closed (`review/disposition.md`); the next review round closes it. Round 4's clean outcome covered the pre-decision draft. Round 5 raised three bugs (M10, M11, M12), each fixed in the round-5 fix pass, no wontfix, and no escalation. A misread of the restatement's Done-when sentence against the Decisions section is under Learnings. Product tests were not run. Round 5 checked the rewritten design by reading the download handler, `FeedbackProblems`, `RolePermissionSeed`, `InMemoryRoleAgentScopeStore`, `XaiChatDefaults.AuthenticatedPolicy`, `EffectiveRolesResolver`, and the hosted `[EndpointAuthorize]` attributes (34 human-only, 14 both, 5 agent-only).

### How to validate

Smoke:

```bash
git diff --stat origin/master...HEAD -- . ':!kanban'
ls kanban/in-progress/468-design-principal-neutral-capabilities-for-humans-and-agents/
D=kanban/in-progress/468-design-principal-neutral-capabilities-for-humans-and-agents/design.md
grep -c '^## ' "$D"
sed -n '/^## 10\./,/^## 11\./p' "$D" | grep -c '^- \*\*'
sed -n '/^## 11\./,$p' "$D" | grep -c '^[0-9]*\. \*\*'
grep -c -E 'DelegatedFrom|FirstParty|first-party|principal_purchased|PurchaseCapability' "$D"
grep -c 'OnBehalfOf' "$D"
grep -n '^effective(P) = ' "$D"
grep -n 'RoleIds.Principal' "$D" | head -1
grep -c 'ActingAsSelf' "$D"
grep -n -E '^\| [1-7] \|' "$D" | cut -c1-60
grep -c 'feedback.file.self` on the attribute' "$D"
grep -n -A3 'DownloadFeedbackAttachment`, in handler order' "$D"
grep -c "200 on the bearer's own unlinked draft" "$D"
grep -c 'Developer does' "$D"
grep -c 'No seeded role holds `demo.invoke`' "$D"
grep -c 'only role is Developer or Operator' "$D"
grep -n '^| GetCredentials |' "$D" | cut -c1-40
grep -c '^| AddPasskey, StartAddExistingPasskey, CompleteAddExistingPasskey, AddAgentKey, RenameCredential, RevokeCredential |' "$D"
```

Expect: the first command prints nothing (no change outside `kanban/`). The folder holds `task.md`,
`design.md`, and `review/` beside the host-owned journal and progress log. The design has 11 `##`
sections, 17 Decided bullets in section 10 (Purchases plus Q1 to Q9 and Q12 to Q18), and 4
numbered open questions in section 11. No `DelegatedFrom`, first-party, or purchase-slice term
remains (count 0); `OnBehalfOf` appears (count above 0). Section 2.2's formula is the union of
the Principal bundle, roles, and grants. `RoleIds.Principal` is named. `ActingAsSelf` appears
more than once. Section 8 has seven task rows, each with a Depends on column and proof gates
and no estimate. Section 6.1 has its own `DownloadFeedbackAttachment` row that names no attribute
permission (the `feedback.file.self` count is 0), followed by a handler-level checks list whose
download entry puts the unlinked-draft owner-only gate before the `feedback.read` or
`admin.feedback.read` disjunction, and task 5's download pins expect 200 on the bearer's own
unlinked draft (count 1). The round-5 fixes hold: `Developer does` is gone (count 0), example 3.1
says no seeded role holds `demo.invoke` (count 1), the Developer-or-Operator chat split appears
in section 2.4 and question 4 (count 2), `GetCredentials` has its own permission-only row in
section 6.1, and the six-operation acting-as-self credential row appears twice, once in 6.1 and
once in 6.2 (count 2).

## Review

- Round 1: Blocked — open M1, M2, M3, M4, M5, M6, M7
- Round 2: Blocked — open M8
- Round 3: Blocked — open M9
- Round 4: Done
- Round 5: Blocked — open M10, M11, M12
## Learnings

The implementer restatement's Done-when sentence still asks for an endpoint table that splits permission-only from human-presence, and for an open-questions list that leaves every product choice in the original description open. The Decisions section (Steve, 2026-11-03) already closed both: Q18 is no browser-session rule, and section 10 is a Decided list with new questions only. The revision-pass paragraph in the same restatement matches those decisions, and `design.md` follows them. The Done-when sentence is the misread. The checklist lines "four acquisition paths" and "no product choices settled by the author" are the same leftover: the decisions reduced the sources to three and recorded Steve's answers.

## Notes

Related: done 466 (admin-approved agent scopes; its scope list and kind-based Member exception
are what this design replaces), done 331 (OpenAPI reviewer guide), done 378 (scoped feedback
triage), 182-006 (agent grants only from token scopes), 205 (agent ↔ human link), 104 decision 3
(agents may pay and call without a human), archived 467.
