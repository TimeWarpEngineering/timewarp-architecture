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

## Checklist

- [ ] Role agent-scope list: stores (in-memory + EF + migration), contract, role editor UI
- [ ] Principal scope grants: stores (in-memory + EF + migration), contract, Principal page UI with effective scopes and their sources
- [ ] Issuance refuses unapproved scopes with 403 naming them; `identity:read` always approved
- [ ] OpenAPI issuance remarks and reviewer guide updated
- [ ] Integration tests for default, role, principal grant, removal and mixed requests
- [ ] Existing agent-token tests approve scopes through a shared helper
- [ ] `dev build` 0/0; affected suites pass; `dev template-smoke` (template output changes); `dev check-version` if platform packages change

## Session

- Created: 431c3e64-803d-4442-8ca1-a6a431770511 (2026-11-02). Steve found that any self-registered
  agent can take feedback scopes while testing task 331's OpenAPI reviewer guide, and decided on
  roles plus principal grants, an `identity:read` default, and refusing over-asks.

## Notes

Related: done 331 (OpenAPI reviewer guide), done 378 (scoped feedback triage), 182-006 (agent
grants come only from token scopes).
