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

## Checklist

- [ ] `design.md` covers the model, the four acquisition paths with worked examples, and the request flow
- [ ] Effect on every existing piece listed above, including the `TimeWarp.Identity` package
- [ ] Endpoint table: permission-only vs human-presence, with reasons
- [ ] Migration for existing tokens, stored grants, and generated apps
- [ ] Proposed implementation task breakdown with dependencies
- [ ] Open questions listed for Steve; no product choices settled by the author
- [ ] No product code changed

## Session

- Created: 431c3e64-803d-4442-8ca1-a6a431770511 (2026-11-02). While testing task 466, Steve asked
  why an agent can't hold any permission a human can. Discussion concluded the human/agent
  split should go: capabilities come from roles, grants, purchases, and delegation for every
  principal kind. Task 467 (seed a Feedback Reviewer role with agent scopes) was archived as moot.

## Notes

Related: done 466 (admin-approved agent scopes; its scope list and kind-based Member exception
are what this design replaces), done 331 (OpenAPI reviewer guide), done 378 (scoped feedback
triage), 182-006 (agent grants only from token scopes), 205 (agent ↔ human link), 104 decision 3
(agents may pay and call without a human), archived 467.
