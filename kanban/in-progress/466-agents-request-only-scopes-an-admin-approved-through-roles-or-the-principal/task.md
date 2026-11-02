# Agents request only scopes an admin approved through roles or the principal

## Description

Anyone can register an agent key without signing in, and the agent names its own scopes at token
issuance. Nothing limits which scopes it may name, so a brand-new agent can take
`feedback:read` / `note` / `severity` / `status` and read and edit all feedback (task 378's tests
issue those tokens to fresh, roleless agents). The admin Principal page
(`/Admin/Principals/{id}`) shows an agent as Member and edits only roles, and roles have no
effect on an agent token (task 182-006, `agent-scope-permission-seed-application.cs`).

**Decisions (Steve, 2026-11-02):**

1. **The agent still chooses its scopes, but only from an approved set.** The approved set is the
   union of:
   - **agent scopes carried by the principal's roles.** A role gains a list of agent scopes,
     separate from its human permissions. Example: a "Feedback Reviewer" role lists the four
     feedback scopes.
   - **scopes granted on the principal itself.** The admin Principal page adds or removes single
     scopes on top of what the roles give.
2. **A brand-new agent may request only `identity:read`** until an admin approves more. That keeps
   self-identity and human linking working. Every other scope (`feedback:*`, `credential:manage`,
   `demo:invoke`) needs a role or a principal grant.
3. **Asking for an unapproved scope refuses the request.** `complete-agent-token-issuance` returns
   403 problem details naming the unapproved scopes and issues no token. It does not narrow the
   token.

### What stays the same

- Human role **permissions** still never flow to an agent token. A role's agent-scope list is a
  separate field. The invariant pinned by `Role_Assigned_Agent_Should_Not_Inherit_Human_Grants`
  (an agent whose principal holds Administrator still gets only its token's scopes) still holds.
- Scope → permission expansion is unchanged (`AgentScopePermissionSeed`). The seed invariant (no
  `admin.*` via agent scopes) still holds.
- Registration stays anonymous. Issuance stays proof-of-possession.

### Requirements

- **Where the check runs:** in `complete-agent-token-issuance`, after the existing scope-shape
  and unknown-scope checks (unknown scope stays 400 `invalid_scope`), and after the credential
  lookup that yields the PrincipalId. Approved set = `identity:read` ∪ role agent scopes ∪
  principal scope grants. Requested ⊄ approved → 403 problem details listing the missing scopes.
  Keep the existing ordering rule: the challenge is consumed before any refusal.
- **Storage, both stores:** role → agent scopes and principal → agent scopes, each with an
  in-memory store and an EF store when `postgres` is on, following the existing
  role-permission-grant pair (`role-permission-grant-*`, `ef-role-permission-store-infrastructure.cs`,
  `in-memory-role-permission-store-application.cs`). EF migration included.
- **Admin contracts and UI:**
  - the role editor (`features/admin/roles`, beside `set-role-permissions`) edits a role's agent
    scopes;
  - the Principal page (`features/admin/principals`, beside `set-principal-roles`) shows the
    effective approved scopes (each marked as coming from a role, from the principal, or the
    default) and edits the principal's own grants;
  - the scope list comes from `AgentScopes`, not a hand-written copy.

  New contracts follow `tw-web-api-contracts` (`[Operation]`, admin permission policy). UI changes
  dispatch TimeWarp.State actions (`tw-blazor`).
- **OpenAPI:** the `complete-agent-token-issuance` description (contract remarks, as task 331 set
  up) says the agent may request only scopes an admin approved, that `identity:read` is always
  allowed, and that an unapproved scope returns 403 naming the scopes. The `JWTBearerAuth`
  reviewer guide (`open-api-agent-reviewer-guide-server.cs`) says the same in one sentence.
- **Tests (integration, in-proc host, no mocks):**
  - a new agent gets an `identity:read` token; asking for `feedback:read` returns 403 naming it;
  - after a role with feedback scopes is assigned, the same request succeeds;
  - a principal-level grant alone is enough; removing it refuses again;
  - asking for one approved and one unapproved scope returns 403 and issues no token;
  - the existing agent-token tests (feedback, credential, metered, identity, OpenAPI security)
    approve scopes first through a shared test helper. Do not weaken them to pass.

### Out of scope

- A durable agent token store (in memory today; see `tw-deploy`).
- Revoking live tokens when approval is removed. Tokens last 15 minutes, so approval changes take
  effect at the next issuance. Say so in the OpenAPI text.
- Human sponsorship or approval of agent registration itself.

## Goal (implementer restatement)

An agent still names the scopes on its token request, but issuance accepts only scopes an
admin has approved for that principal. The approved set is `identity:read` plus the agent
scopes on the principal's roles plus scopes granted on the principal itself. Human role
permissions stay off the token. A request that names anything outside that set gets 403
problem details that list the missing scopes and no token. Unknown scope strings stay 400
`invalid_scope`. The challenge is still consumed before that refusal. Approval changes do
not revoke a token that is already out; they apply on the next issuance.

Done when:

- A role stores an agent-scope list separate from its permissions, in memory and in EF when
  postgres is on, with a migration. The role editor replaces that list through a new admin
  contract. The scope choices come from `AgentScopes`.
- A principal stores its own agent-scope grants the same way. The Principal page replaces
  those grants and shows the effective approved set with each scope marked default, role, or
  principal.
- `complete-agent-token-issuance` enforces the approved set after the existing shape and
  unknown-scope checks and after the credential lookup. OpenAPI remarks and the JWT bearer
  reviewer guide say `identity:read` is always allowed, an unapproved scope is 403 and names
  the scopes, and a removed approval waits until the next issuance.
- Integration tests cover a new agent's `identity:read` success, a `feedback:read` refusal,
  success after a role grant, success from a principal grant alone, refusal after that grant
  is removed, and a mixed approved-plus-unapproved request that issues no token. Existing
  agent-token tests approve scopes through one shared helper and keep their assertions.
- `dev build` is 0/0 and the affected suites pass. Template smoke runs if template output
  changes. `dev check-version` runs if a platform package version changes.

## Checklist

- [x] Role agent-scope list: stores (in-memory + EF + migration), contract, role editor UI
- [x] Principal scope grants: stores (in-memory + EF + migration), contract, Principal page UI with effective scopes and their sources
- [x] Issuance refuses unapproved scopes with 403 naming them; `identity:read` always approved
- [x] OpenAPI issuance remarks and reviewer guide updated
- [x] Integration tests for default, role, principal grant, removal and mixed requests
- [x] Existing agent-token tests approve scopes through a shared helper
- [x] `dev build` 0/0; affected suites pass; `dev template-smoke` (template output changes); `dev check-version` if platform packages change

## Session

- Created: 431c3e64-803d-4442-8ca1-a6a431770511 (2026-11-02). Steve found that any self-registered
  agent can take feedback scopes while testing task 331's OpenAPI reviewer guide, and decided on
  roles plus principal grants, an `identity:read` default, and refusing over-asks.
- Implementation: 01a19bcd-9ede-71f0-8e07-26d06104fc68 (2026-11-02). Issuance checks the approved
  set after proof-of-possession. `dev db add-migration` also dropped a second model snapshot under
  `projects/web-infrastructure/TimeWarp/`; that copy was removed so only
  `platform/postgres/migrations` holds `PostgresDbContextModelSnapshot`. `dev check-version` was
  not run: TimeWarp.Identity's public API did not change. `ganda repo audit` exited 0 with the
  existing `kanban-landed` advisory for other claims that have no worktree on this machine.

## Results

An agent still names scopes on `complete-agent-token-issuance`, and the handler issues a token
only when every requested scope is approved. The approved set is `identity:read`, plus agent
scopes stored on the principal's effective roles, plus scopes granted on the principal. An
unknown scope stays 400 `invalid_scope`. An unapproved known scope is 403 `unapproved_scope`
and names only the missing scopes. The challenge is already consumed, and no token is issued.
Human role permissions still do not flow onto the agent token.

Role agent scopes and principal agent scopes each have an in-memory store and an EF store.
Migration `20261102105501_AddAgentScopeGrants` creates `identity.role_agent_scopes` and
`identity.principal_agent_scopes`. The role editor saves a role's agent scopes separately from
its permissions. The Principal page lists each effective scope as default, role, or principal,
and edits the principal's own grants. Both editors take the scope list from `AgentScopes`.
OpenAPI remarks and the JWT bearer reviewer guide say approval is required, `identity:read` is
always allowed, a refusal names the scopes, and removing an approval applies at the next
issuance.

The admin pages were not opened in a browser. The SPA project compiled, and `PrincipalState_`
covered opening a principal against a scripted API.

### How to validate

Smoke:

```bash
cd tests/container-apps/web/web-server-integration-tests
dotnet test -c Release -- --filter-class AgentScopeApproval_
```

Expect: 4 passed, 0 failed. A new agent receives `identity:read`. Requesting `feedback:read`
returns 403 whose detail is `Unapproved scope(s): feedback:read.` and issues no token.
Assigning the Developer role after that role's agent scopes include the feedback scopes makes
the same request succeed. A principal grant alone allows the scope, and clearing that grant
refuses it again. Requesting one approved scope and `demo:invoke` returns 403 naming only
`demo:invoke` and issues no token.

`./bin/dev build` succeeded with 0 warnings and 0 errors. `./bin/dev template-smoke` printed
`Template smoke SUCCEEDED`. `ganda repo audit` exited 0 (34 passed, 1 advisory `kanban-landed`,
2 skipped).

## Notes

Related: done 331 (OpenAPI reviewer guide), done 378 (scoped feedback triage), 182-006 (agent
grants come only from token scopes).
