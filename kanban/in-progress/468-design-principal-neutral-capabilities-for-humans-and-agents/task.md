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
row names its reason. Handler-level resource checks (the attachment download disjunction, the
`admin.profiles.read` PII gate, filer-only reads) survive and are listed under 6.1.

Three design elements are proposals the description did not name and are listed as open questions
rather than settled: the grant ceiling (Q3), the first-party reading of "a present person" (Q18),
and the default bundle (Q1, Q2).

No product code changed. Only `task.md` and `design.md` in this folder are in the diff.

The implementer restatement matches the task. Service accounts are the third kind the resolver already special-cases, which the task's "every principal kind" and the kind exception already cover. No misread recorded.

### Review

Round 1, effort 2, roster general only. Artifacts: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`.

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 5 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 1 | 0 |

Fix pass landed 2026-11-02 (Session). M1 through M7 are `fixed` on `review/round-1/merged.md` with disposition notes. Disposition is not written; the host runs review round 2 against the post-fix diff.

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
```

Expect: the first command prints nothing (no change outside `kanban/`). The folder holds `task.md`,
`design.md`, and `review/` beside the host-owned journal and progress log. The design has 10 `##`
sections and 18 numbered open questions in section 10. No `on_behalf_of` remains (provenance is
per id as `DelegatedFrom`). The Depends-on cells for tasks 5, 6, and 7 each include 3 and 4.
Section 1 says "Both: 14". Section 6.1 has its own `DownloadFeedbackAttachment` row followed by a
handler-level checks list. Section 6.2 names a reason for every first-party row. Section 8 has
eight tasks, each with a Depends on column and proof gates and no estimate.

## Review

- Round 1: Blocked — open M1, M2, M3, M4, M5, M6, M7

## Notes

Related: done 466 (admin-approved agent scopes; its scope list and kind-based Member exception
are what this design replaces), done 331 (OpenAPI reviewer guide), done 378 (scoped feedback
triage), 182-006 (agent grants only from token scopes), 205 (agent ↔ human link), 104 decision 3
(agents may pay and call without a human), archived 467.
