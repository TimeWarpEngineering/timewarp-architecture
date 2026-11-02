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

Round 1 left five items. This pass closes them:

- A generated app with `--postgres false` excludes the two EF agent-scope stores and their
  two entity-type configurations, the same way it already excludes the role-permission pair,
  so those files do not reference the removed `PostgresDbContext`.
- One helper in `platform/authorization` orders known agent scopes. SetRoleAgentScopes,
  SetPrincipalAgentScopes, AgentScopeApproval, GetRole, and GetRoles call that helper.
- The Member role page says that agent scopes on Member reach every principal with no other
  role, including an agent that registers without signing in. SetRoleAgentScopes records the
  same rule and still stores the list.
- One co-located Jaribu test builds the effective set from the in-memory stores and checks
  default, role (with role id), principal, a scope that has several sources, and catalog order.
- The Principal page disables Save agent scopes while that action is in flight.
- A generated app with `--web false` excludes `agent-scope-approval.cs`. That helper
  references the web authorization types, which are not in the app when web is off.

## Checklist

- [x] Role agent-scope list: stores (in-memory + EF + migration), contract, role editor UI
- [x] Principal scope grants: stores (in-memory + EF + migration), contract, Principal page UI with effective scopes and their sources
- [x] Issuance refuses unapproved scopes with 403 naming them; `identity:read` always approved
- [x] OpenAPI issuance remarks and reviewer guide updated
- [x] Integration tests for default, role, principal grant, removal and mixed requests
- [x] Existing agent-token tests approve scopes through a shared helper
- [x] `dev build` 0/0; affected suites pass; `dev template-smoke` (template output changes); `dev check-version` if platform packages change
- [x] Postgres-off template excludes the four EF agent-scope files
- [x] One `AgentScopeOrder` helper used by the five call sites
- [x] Member role warns that its agent scopes reach every unassigned principal
- [x] Co-located test covers effective-set sources and catalog order
- [x] Principal page disables Save agent scopes while that action is active
- [x] `--web false` excludes the agent-scope approval test helper

## Session

- Created: 431c3e64-803d-4442-8ca1-a6a431770511 (2026-11-02). Steve found that any self-registered
  agent can take feedback scopes while testing task 331's OpenAPI reviewer guide, and decided on
  roles plus principal grants, an `identity:read` default, and refusing over-asks.
- Implementation: 01a19bcd-9ede-71f0-8e07-26d06104fc68 (2026-11-02). Issuance checks the approved
  set after proof-of-possession. `dev db add-migration` also dropped a second model snapshot under
  `projects/web-infrastructure/TimeWarp/`; that copy was removed so only
  `platform/postgres/migrations` holds `PostgresDbContextModelSnapshot`. The two new contract
  runfiles are executable so `runfile-executable` passes. `dev check-version` was not run:
  TimeWarp.Identity's public API did not change. `ganda repo audit` exited 0 with the existing
  `kanban-landed` advisory for other claims that have no worktree on this machine.
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a19bcd-9ede-71f0-8e07-26d06104fc68, max-turns 500, Done — 2026-11-02T11:14:41Z
- Review oracle round 1: general reviewer (Claude Opus 5.5, effort 3, turn cap 200), 2026-11-02.
  Found 5 open (1 bug, 3 suggestions, 1 nit). The fix pass is requested on this id.
  Artifacts are in `review/review-framework.md` and `review/round-1/`.
- Review oracle: review by reviewer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session b87b06b2-42f3-493d-8fc4-3ae22addf67d, max-turns 200 — 2026-11-02T11:19:41Z
- Fix pass (review round 1, 2026-11-02): excluded the four EF agent-scope files when postgres
  is off, moved scope ordering to `AgentScopeOrder`, warned on the Member role, added the
  source-marking test, and disabled the principal save button while saving. The first
  `template-smoke --matrix` compiled SmokeNoPostgres and then failed SmokeNoWeb:
  `agent-scope-approval.cs` referenced web authorization types. That helper is now on the
  `(!web)` exclude list, beside `web-test-server-application.cs`. The matrix re-run
  printed `Template smoke SUCCEEDED` (SmokeDefault, SmokeNoPostgres, SmokeNoApi,
  SmokeNoWeb, SmokeNoGrpc, and SmokeNoYarp each OK). `ganda repo audit` exited 0
  with the existing `kanban-landed` advisory for other claims that have no worktree
  on this machine. `dev check-version` was not run: no package version changed.
  The admin pages were not opened in a browser. Fix-pass session
  01a19bfc-8d84-7b73-9711-6cf480787a59.

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

Round 1's five findings are addressed on this branch. A `--postgres false` app excludes the
two EF agent-scope stores and their entity-type configurations. A `--web false` app excludes
`tests/common/timewarp-testing/applications/agent-scope-approval.cs`, which references those
web types. `AgentScopeOrder.OrderKnown`
is the only catalog-order helper. The Member role page states that scopes on Member reach
every principal with no other role, including an agent that registers without signing in, and
`SetRoleAgentScopes` records that rule. The Principal page disables Save agent scopes while
`SetPrincipalAgentScopes` is active. The effective-set source marks are covered by
`AgentScopeApproval_Given_.MixedSources_Should_MarkDefaultRoleAndPrincipalInCatalogOrder`.

### How to validate

Smoke:

```bash
dotnet run source/container-apps/web/platform/authorization/agent-scope-approval-tests.cs
./bin/dev template-smoke --matrix
```

Expect: the co-located test passes 1/1. `identity:read` is marked default, plus the Developer
role and the principal. `demo:invoke` is the Developer role only. `credential:manage` is the
principal only. `feedback:read` is Member, then Developer, then the principal. Those four
scopes are in `AgentScopes.All` order, and an unknown stored string is absent. Template smoke
prints `Template smoke SUCCEEDED` for the matrix, including `SmokeNoPostgres` and `SmokeNoWeb`.
The first matrix failed SmokeNoWeb with CS0234 on `agent-scope-approval.cs` before that file
was excluded for `--web false`.

`./bin/dev build` succeeded with 0 warnings and 0 errors. The same co-located test passed
once as a runfile and once under `web-jaribu-tests` (`--filter-class AgentScopeApproval_Given_`,
1 passed). After the `(!web)` exclude, `./bin/dev template-smoke --matrix` printed
`Template smoke SUCCEEDED`. SmokeNoPostgres and SmokeNoWeb both printed OK, and the
generated SmokeNoWeb tree does not contain `agent-scope-approval.cs`.

### Review

Round 1 found 1 bug, 3 suggestions, and 1 nit. This fix pass implements all five. Round 2
has not run, so the ledger in `review/round-1/merged.md` is still the open round-1 list.

- **M1 (bug):** the `(!postgres)` exclude list now names the two EF agent-scope stores and
  their two entity-type configurations.
- **M2:** `AgentScopeOrder.OrderKnown` replaces the three copies. GetRole and GetRoles call it.
- **M3:** the Member role page shows the warning, and SetRoleAgentScopes records the rule.
- **M4:** the co-located in-memory test asserts sources and order.
- **M5:** Save agent scopes binds `Disabled` to the action's active state.

The reviewer re-ran the issuance suite (4/4) and the full web-server integration suite
(434 passed, 1 skipped, 0 failed) before this pass. Those suites were not re-run here.
Ledger: `review/round-1/merged.md`. Round 2 follows this node.

## Review

- Round 1 findings M1–M5 are implemented on this branch. Round 2 has not run.

## Notes

Related: done 331 (OpenAPI reviewer guide), done 378 (scoped feedback triage), 182-006 (agent
grants come only from token scopes).
