# Principal-neutral capabilities: design

Task 468. Design only. No product code changes on this task. Steve reviews this document before
any implementation task is filed. Section 10 lists the decisions that are his.

Decision this design serves (Steve, 2026-11-02): what a principal may do does not depend on
whether it is a human, an agent, or a service account. A principal that nobody vouches for signs
up with a minimal default and gets more capability by requesting it, by paying for it, or through
delegation from a principal that vouches for it. An agent does not need a linked human.

## 1. Where we are

Two vocabularies, two grant paths, and kind checks spread across the stack.

| Piece | Today |
|---|---|
| Permission ids | 23 `PermissionIds` constants in `source/container-apps/web/features/authorization/permission-ids-contracts.cs` (template code). Policy name equals permission id. |
| Agent scopes | 7 `AgentScopes` constants in `source/libraries/timewarp-identity/tokens/agent-scopes.cs` (the `TimeWarp.Identity` package, source in this repo, pinned `2.0.0-beta.30` for generated apps). Colon form (`identity:read`) against the dot form of permissions (`identity.read`). |
| Scope to permission | `AgentScopePermissionSeed` (compile-time map, not persisted). `identity:read` expands to two permissions, the other six to one each. No scope reaches any `admin.*`, `developer.*`, `profile.*`, `settings.*`, or `feedback.file.self`. |
| Roles | Four fixed `RoleIds` (Member, Operator, Administrator, Developer). `RolePermissionSeed` bundles. Role names live in a static dictionary, not a table. Role permission rows in `identity.role_permissions`. |
| Role scopes and principal scopes | Task 466: `identity.role_agent_scopes` and `identity.principal_agent_scopes` (migration `20261102105501_AddAgentScopeGrants`), two stores each, `SetRoleAgentScopes` and `SetPrincipalAgentScopes`, an "Agent scopes" box on the role page and two agent-scope sections on the Principal page. |
| Default | `EffectiveRolesResolver`: a roleless human (or a missing row) is Member; a roleless agent or service has no role. `identity:read` is always approved at issuance. |
| Evaluator | `IPermissionEvaluator` branches on authentication scheme. Human session: effective roles then role permissions, live from the store. Agent token: the token's scopes through the seed. Never both. |
| Endpoints | `[EndpointAuthorize(Policy, AuthenticationSchemes)]`. The scheme list decides which credential kinds may call. Human-only: 34 operations. Agent-only: 4 plus the api-host sample. Both: 14. |
| Token | Opaque, in-memory store, 15 minutes default, claims `timewarp:principal_id` and one `timewarp:scope` per scope. The agent names scopes; issuance refuses unapproved ones with 403 `unapproved_scope`. |
| Agent link | `features/agent-links`: Pending, Approved, Denied. Approve is human-only and grants nothing. No revoke. |
| Purchases | `MeteredCapabilityGate` debits a per-principal credit ledger or runs the x402 flow per call. Settlement promotes `TrustTier.Funded`. No purchase creates a permission. Ledger is in memory. |
| Granting | `SetRolePermissions`, `SetPrincipalRoles`, and the two scope setters, each gated by an `admin.*.manage` permission and human session only. No per-principal permission grant. No audit record of any grant or revoke. |
| Kind checks | Resolver default, `AdminLockoutGuards.SimulateEffectiveRoles`, and the three agent-link handlers. The evaluator checks scheme, not kind. |

The consequence Steve hit while testing 466: a "Feedback Reviewer" has to be configured twice
(role permissions for humans, role agent scopes for agents), and an agent can never hold a
permission that is not on the seven-scope list however much an admin trusts it.

## 2. The model

### 2.1 One vocabulary

Permission ids are the only capability vocabulary. `AgentScopes` and `AgentScopePermissionSeed`
are removed. A bearer-token request names permission ids. Roles mean the same thing for every
principal. The word "scope" survives only as the OAuth wire term in the OpenAPI security scheme,
where each scope string is a permission id.

### 2.2 Four sources, one union

For every principal, whatever its kind:

```
effective(P) = roles(P) ∪ grants(P) ∪ purchases(P) ∪ delegated(P)
```

| Source | Store | Who writes it | Today's nearest thing |
|---|---|---|---|
| Roles | `IPrincipalRoleStore` plus `IRolePermissionStore` (unchanged) | Holder of `admin.principals.manage` (assign) and `admin.roles.manage` (edit bundle) | Same |
| Direct grants | New `principal_permission_grants` (PrincipalId, PermissionId, GrantedBy, GrantedAt, RequestId?) | Holder of the grant permission (section 2.6) | Task 466's `principal_agent_scopes`, generalised from scopes to permissions |
| Purchases | New `principal_purchased_permissions` (PrincipalId, PermissionId, ReceiptId, PurchasedAt, ExpiresAt?) | The x402 settle path, idempotent by receipt | The credit ledger (per-call), which stays for metering |
| Delegation | The agent-link entity grows a delegated permission list | The delegator, on its own link | The link, which grants nothing today |

Evaluation of `delegated(P)` is live: `link.Delegated ∩ effective(delegator)` computed at the
moment a credential is issued (section 2.5). The other three are stored facts.

### 2.3 One default for every self-signed-up principal

Every principal created by an anonymous ceremony (passkey registration, agent-key registration,
Entra bootstrap create) resolves to the same default when it has no stored role. Kind no longer
appears in `EffectiveRolesResolver`. Two shapes are possible; section 10 asks Steve to pick.

- **Member is the default for everyone.** Member's bundle becomes the minimal set and
  `identity.read` joins it. Simplest: no new role id, no row migration for humans, the Member
  page already exists.
- **A new default role** (for example "Principal") holds the minimal set, and Member becomes a
  granted role with its current bundle. Keeps Member's meaning for existing deployments, at the
  cost of a new seeded role and a data migration that assigns Member to every existing human.

Proposed minimal bundle, either way (the ids exist today unless marked new):

| Permission | Why it is in the default |
|---|---|
| `identity.read` | A principal can read who it is (`GetAgentIdentity`, today agent-only). |
| `agent-link.manage.self` | Request, list, approve, and deny links on its own principal. |
| `credential.manage.self` | Rotate its own keys or passkeys. Already self-service for humans and the `credential:manage` scope for agents. |
| `profile.read`, `profile.write` | Its own profile row. |
| `settings.read` | Passkey prompt mode and sign-in offers, needed by the SPA shell. |
| `feedback.file.self` | File feedback and follow its own items. Opens the feedback channel to agents, which 378 excluded only because scopes could not carry it. |
| `capability.request.self` (new) | File a request for more capability (section 4). |

Not in the default: anything `admin.*`, `developer.*`, `settings.write`, the four `feedback.*`
reviewer ids, `demo.invoke`. Those arrive by grant, role, purchase, or delegation.

This replaces task 466's kind-based Member exception and its hard-coded `identity:read` default.
Bootstrap (`BootstrapAdministratorOptions`) is unchanged and already kind-neutral.

### 2.4 Endpoints authorize by permission

An endpoint states the permission it needs. Any authenticated principal holding it passes,
whatever its kind and whatever credential authenticated it. Concretely:

- `[EndpointAuthorize]` keeps `Policy`. `AuthenticationSchemes` is no longer the way to choose a
  principal kind. The generator emits `AuthSchemes(all host credential schemes)` by default, or
  the permission policies carry the scheme list through `AddPermissionPolicies` (the reason
  endpoint-auth.md gives for not doing that, scheme-aware expansion in the evaluator, goes away
  with the scheme split). One of the two; the implementation task picks and documents it in the
  `IPermissionEvaluator` Design region.
- A short, explicit list of operations keeps a per-operation requirement that a credential cannot
  satisfy by permission alone (section 6). That is a new property on the attribute, for example
  `Requires = CredentialRequirement.FirstParty`, not a scheme list. The task asks for a
  per-operation "present person" requirement; this design reads it as "the credential is the
  subject's own", which an agent's own token satisfies. Q18 asks Steve to confirm that reading or
  to narrow those rows to a browser session.
- The non-permission policies `IdentitySessionDefaults.AuthenticatedPolicy` and
  `XaiChatDefaults.AuthenticatedPolicy` go away. Every protected operation names a permission id,
  with one exception: `DownloadFeedbackAttachment` keeps an `[EndpointAuthorize]` with no
  `Policy` (any host credential, authenticated), because its rule is a resource disjunction that
  no single id expresses and the handler is the whole rule (section 6.1). It is the only hosted
  operation with that shape today and stays the only one. The two agent-chat operations need a
  permission id (section 10 asks which).
- The api host's `agent-scope:*` claim policies become permission-claim policies over the same
  ids (`RequireClaim("timewarp:permission", PermissionIds.IdentityRead)`), so the sample endpoint
  reads like a web endpoint.

Build checks:

- TWA0024 (policy registered by this server) is unchanged and still the agreement check.
- A new analyzer id flags `AuthenticationSchemes` on a hosted contract outside the first-party
  list, so kind selection cannot creep back in through a scheme string. The first-party list is
  the attribute usages themselves; the OpenAPI text for those operations is generated from the
  attribute, so the two cannot disagree.
- The seed test `AgentScopeSeed_Should_NotIntersectAdminPermissions` is replaced by a grant
  ceiling test (section 2.6).

### 2.5 A credential may carry a subset

A bearer token request names the permission ids the token should carry. Issuance computes
`effective(P)` and refuses when the request names anything outside it: 403 problem details that
list the missing ids and the operation that files a request for them (section 4). An unknown id
stays 400. The token carries exactly the approved list. The request-time evaluator for a bearer
credential is `token.Permissions ∩ live effective(P)` or `token.Permissions` alone; section 10
asks which.

This is a credential feature. A browser session carries the whole effective set because the
session is minted by a ceremony with no request body; a token carries what was asked for. A
human who holds an agent key gets the same subset behaviour on that key's tokens. Nothing in the
subset rule reads the principal's kind.

Delegation is evaluated here. For a token issued to principal A whose link to B delegates list D:

```
delegated(A) = D ∩ effective(B)      evaluated now, at issuance
effective(A) = roles(A) ∪ grants(A) ∪ purchases(A) ∪ delegated(A)
```

Removing a permission from B removes it from A at A's next token. Tokens last
`AgentTokenOptions.TokenLifetimeMinutes` (15 by default, 1 to 60), so that is the revocation
latency, the same as today's approval removal.

Provenance is per permission id, not per token. The token shape is a list of
`TokenPermission(PermissionId, DelegatedFrom?)`: `DelegatedFrom` is null for an id A holds
through its own roles, grants, or purchases, and is B for an id that A holds only through the
link. A mixed token is allowed and is the normal case (example 3.3 carries `identity.read` as
A's own and two feedback ids delegated from B). Issuance decides each id's provenance in order:
own sources first, delegation only for an id A does not hold itself, so an id A holds both ways
is recorded as A's own. First-party checks (section 6), the not-self check (section 2.6), audit,
and problem details read `DelegatedFrom` on the ids an operation uses, so they can say whose
capability was spent without rejecting the caller's own ids or mistaking a delegated id for the
subject's own. Nothing on the token says "this whole token acts for B".

### 2.6 Granting is a permission, with a ceiling

Rules, each kind-neutral:

1. **Grant permission.** Writing `grants(P)` needs `admin.principals.manage`. Writing a role's
   bundle needs `admin.roles.manage`. Assigning roles needs `admin.principals.manage`. Unchanged
   ids; the change is that any principal holding them may call the endpoint.
2. **Ceiling (proposed, Q3).** The invariant the task states is "granting is itself a
   permission". How that permission is scoped is open. This design proposes a ceiling: a
   grantor may grant only permission ids it holds itself at the moment of the grant, whether
   the grant lands on a principal or on a role bundle, so "no agent token ever holds `admin.*`"
   becomes "nobody receives `admin.*` from a principal that does not hold it". Q3 offers the
   other reading, per-prefix `grant.<prefix>` permissions, under which a grantor needs
   `grant.admin` rather than the `admin.*` ids themselves. Every later mention of "ceiling" in
   this document means whichever of the two Steve picks. The Administrator protected-core guard
   (`ProtectedCoreConflict`) and the last-administrator guard stay under either.
3. **No self-escalation.** The actor of a grant, role assignment, link approval, or request
   approval may not be the beneficiary. The check compares the acting principal, and the
   `DelegatedFrom` principal of every token id the operation relies on, against the target. An
   administrator still cannot approve its own capability request; a second administrator does.
4. **Audit.** Every grant, revoke, role assignment, role-bundle change, purchase, link approval,
   link revocation, and delegation change writes one row to `authorization_audit` (At, Actor,
   OnBehalfOf?, Action, TargetPrincipal?, TargetRole?, PermissionId?, Source: request id,
   receipt id, or link id). Grant rows also carry `GrantedBy` and `GrantedAt` so the current
   state is self-describing without the audit table. `ListPrincipals` and the Principal page
   show the audit tail for the principal.

### 2.7 Credential rules stay credential rules

Short lifetime, reissue-with-key, rate limits on the ceremonies (104-015), metering through the
ledger, and token revocation on principal quarantine (the bearer handler re-reads the principal)
apply to the bearer credential type whoever holds it. A human who registers an agent key is
under the same rules on that key. Browser sessions keep their own rules. Nothing here moves to
`PrincipalKind`.

## 3. Worked examples

### 3.1 An unvouched agent pays for a capability

1. The agent runs `start-agent-key-registration` and `complete-agent-key-registration`
   (anonymous). Principal kind Agent, tier Provisional then Keyed, effective permissions = the
   default bundle.
2. It issues a token for `identity.read` and reads `/openapi/v1.json`. The `JWTBearerAuth`
   description names the default bundle, the request operation, and the purchase operation.
   `demo.invoke` is marked purchasable with its price.
3. It calls `POST api/capabilities/demo.invoke/purchase` with a token. The gate returns 402 with
   a `PAYMENT-REQUIRED` challenge (or 503 when payment is disabled). It pays and repeats with
   `PAYMENT-SIGNATURE`. Settlement credits the ledger (existing `SettlementFundingService`),
   promotes the tier to Funded, and writes a `principal_purchased_permissions` row keyed by the
   receipt id (idempotent on retry) plus an audit row with Action Purchase.
4. Its next token may name `demo.invoke`. `InvokeMeteredCapability` authorizes by permission and
   then bills each call through the ledger as it does today. Permission says "may attempt";
   the ledger says "paid for this call". No human was involved and no human is linked.

### 3.2 An admin grants a feedback reviewer its permissions

1. The reviewer, human or agent, signs up and gets the default. It files a capability request for
   `feedback.read`, `feedback.note`, `feedback.severity`, `feedback.status` with a reason
   (section 4), or an administrator starts from the Principal page.
2. An administrator who holds those four ids (Administrator does) approves the request. The
   handler checks the ceiling (approver holds all four), checks approver is not the requester,
   writes four grant rows with `GrantedBy`, marks the request Approved, and writes audit rows.
   Alternatively the administrator creates a "Feedback Reviewer" role with the four ids (one
   permissions box on the role page) and assigns it. Either path is one configuration, not two.
3. The reviewer's next session or token carries the four ids. `ListFeedback`,
   `GetFeedbackForAdmin`, `AddFeedbackNote`, `SetFeedbackSeverity`, `ChangeFeedbackStatus`
   authorize by permission. `ReplyToFeedback` needs `admin.feedback.manage`, which the reviewer
   does not hold, so the "no reply to the filer" posture from 378 is now a grant decision, not a
   kind rule (unless Q6 keeps reply first-party).
4. The administrator later removes `feedback.status`. The grant row is deleted, an audit row
   records the revoke, and the reviewer's next token (or next session evaluation) no longer
   carries it.

### 3.3 A human delegates a subset to a linked agent

1. The agent requests a link to the human (`RequestAgentHumanLink`, existing). The human sees it
   on the Agent links page and approves it, choosing the delegated list: say `feedback.read` and
   `feedback.note`. Approve is a first-party operation (section 6), so a delegated token acting
   for the human cannot approve it. An audit row records the delegation.
2. The agent issues a token naming `feedback.read`, `feedback.note`, `identity.read`. Issuance
   computes `delegated(agent) = {feedback.read, feedback.note} ∩ effective(human)`. The human
   holds both (through Administrator), so the token is issued with `DelegatedFrom = human` on the
   two feedback ids and `DelegatedFrom = null` on `identity.read`, which the agent holds through
   its own default. One token, mixed provenance (section 2.5).
3. The human loses Administrator. At the agent's next issuance the intersection is empty; a
   request naming `feedback.read` gets 403 listing it. The link still exists; it just delegates
   nothing the human no longer has.
4. The human revokes the link (new operation). Audit row. The agent is back to its own sources.

Delegation grants permission ids, not identity. `profile.write` delegated to an agent lets the
agent write the agent's own profile, because every `*.self` permission is evaluated against the
credential's subject. Section 10 asks whether acting as the delegator is wanted at all.

### 3.4 A human signs themselves up

1. `start-passkey-registration` and `complete-passkey-registration` (anonymous) mint a Human
   principal with no stored role. On an empty deployment the first human still becomes
   Administrator plus Member (existing first-admin rule, kind-neutral in effect because only
   humans run that ceremony today).
2. The session evaluator resolves the default (Member or the new default role) and the SPA
   receives the default bundle as `permission` claims. The person can edit their profile, manage
   passkeys, file feedback, approve agent links, and file a capability request. They cannot open
   `/Admin` until someone grants `admin.access` or assigns a role.
3. If this human registers an agent key for a script, that key's tokens carry any subset of the
   same effective set the browser session has. There is no second allow-list.

## 4. The request-for-more-capability flow

New slice `features/capability-requests`, following `tw-web-api-contracts`:

| Operation | Route | Permission | Caller |
|---|---|---|---|
| `RequestCapability` | `POST api/capability-requests` | `capability.request.self` | Any principal. Body: permission ids (validated against `PermissionIds.All`), reason (bounded text). One open request per (principal, permission id); a duplicate is 409. Response: request id and status Pending. |
| `ListMyCapabilityRequests` | `GET api/capability-requests/mine` | `capability.request.self` | The requester polls status. |
| `ListCapabilityRequests` | `GET api/admin/capability-requests` | `admin.principals.manage` | Approvers see Pending requests, filterable by permission id and principal. |
| `ApproveCapabilityRequest` | `POST api/admin/capability-requests/{id}/approve` | `admin.principals.manage` | Ceiling check (approver holds every requested id), not-self check, writes grants, audit. Partial approval is a product choice (section 10). |
| `DenyCapabilityRequest` | `POST api/admin/capability-requests/{id}/deny` | `admin.principals.manage` | Reason text returned to the requester. |

Who sees a pending request: holders of `admin.principals.manage` in the admin UI (a "Capability
requests" page beside Principals, with a badge count), and the requester through `mine`. There is
no notification system in the template; the Principal page and the list page are the inbox.

Who may approve: any principal holding `admin.principals.manage` and every requested id, other
than the requester and any `DelegatedFrom` principal on the ids the approving credential uses
(approve is first-party, section 6.2, so in practice the approver's own ids). Under the proposed
ceiling (Q3), this is what "only a principal allowed to grant `admin.*` can grant it" means in
code: holding `admin.principals.manage` without holding `admin.roles.manage` lets you approve a
request for `feedback.read` but not for `admin.roles.manage`. Under the per-prefix alternative
in Q3, the approver would need `grant.admin` instead of holding `admin.roles.manage` itself.

How an outside agent learns about it from the OpenAPI document alone (task 331's goal):

- The `JWTBearerAuth` scheme description (`open-api-agent-reviewer-guide-server.cs`) lists the
  issuance steps as today, then: the default permission ids every new principal holds, that a
  token request names permission ids, that an id outside the principal's effective set returns
  403 listing the missing ids, that `request-capability (POST /api/capability-requests)` files a
  request for them, that `list-my-capability-requests` shows its status, and that purchasable ids
  carry a price on `purchase-capability`. All of it is generated from `PermissionIds`, the
  default bundle, the purchasable catalog, and the contract types, so it cannot drift.
- Every protected operation's security requirement lists its permission id as the scope string
  (`OpenApiBearerSecurity` already does this through a scope map; the map becomes the identity
  function). The description sentence becomes "Requires permission feedback.read".
- The 403 problem details from issuance carry `missingPermissions` and `requestOperationId`.
  The 403 from an endpoint keeps `WWW-Authenticate: Bearer error="insufficient_scope"` and names
  the permission id in `scope=`.

## 5. Effect on each existing piece

| Piece | Change |
|---|---|
| `AgentScopes` (`TimeWarp.Identity`) | Removed. `AgentTokenGrant.Scopes` (strings) becomes `AgentTokenGrant.Permissions`, an `IReadOnlyList<TokenPermission>` where `TokenPermission` is a record `(string PermissionId, PrincipalId? DelegatedFrom)` in the package (section 2.5). `IAgentTokenStore.Issue(principalId, credentialId, IReadOnlyList<TokenPermission> permissions, lifetime)`; there is no token-wide `onBehalfOf`, because one token mixes own and delegated ids. Public API break: `<Version>` and the `TimeWarp.Identity` and `TimeWarp.402` pins bump in the same commit (`dev check-version`), and a release follows the merge (memory: release when libs change; never design around a release). The package has no permission catalog and should not get one; ids stay template code. |
| `AgentScopePermissionSeed` | Deleted after the migration has used it once: the migration copies every stored scope through the seed's expansion into grant or role-permission rows (section 7.2), including both ids of `identity:read`. Whether those two ids also sit in the default bundle is Q2; the copy does not depend on the answer, because `effective(P)` is a union and an id held twice is held once. |
| Task 466 role scope store | `IRoleAgentScopeStore`, `RoleAgentScopeGrant`, its EF configuration, in-memory and EF stores, `SetRoleAgentScopes`, the SPA action, and the "Agent scopes" box on `RoleDetailPage.razor` are removed. The Member role-page warning text goes with them. |
| Task 466 principal scope store | Becomes the direct-grant store: `IPrincipalPermissionStore`, `PrincipalPermissionGrant` (PrincipalId, PermissionId, GrantedBy, GrantedAt, RequestId?), EF table `identity.principal_permission_grants`. `SetPrincipalAgentScopes` becomes `SetPrincipalPermissions` (replace-set, ceiling check, not-self check, audit). The Principal page shows "Effective permissions" with a source chip per id (default, role name, grant, purchase, delegated from X) and a "Direct grants" editor. `AgentScopeApproval`, `AgentScopeOrder`, and `AgentScopeGrantKind` are replaced by an `EffectivePermissions` service that returns ids with sources, used by issuance, the Principal page, and `GetCurrentSession`. |
| EF migration | One migration: create `principal_permission_grants`, `principal_purchased_permissions`, `capability_requests`, `authorization_audit`; add `DelegatedPermissions` (and `RevokedAt`) to `agent_links.agent_links`; copy `principal_agent_scopes` rows through the scope-to-permission map (`identity:read` to two rows) with `GrantedBy = null`, `GrantedAt = migration time`; copy `role_agent_scopes` rows into `identity.role_permissions` through the same map, skipping duplicates; drop the two agent-scope tables. Also the seed inserts for the default bundle change (section 2.3). |
| Admin UI | Role page: one permissions editor. Principal page: effective permissions with sources, direct-grant editor, pending requests for this principal, audit tail. New Capability requests page. Agent links page: delegated list picker on approve, revoke button. All through TimeWarp.State actions (`tw-blazor`). |
| Token contracts | `CompleteAgentTokenIssuance.Command.Scopes` becomes `Permissions` (1 to 32 ids, validated against `PermissionIds.All`; unknown is 400 `invalid_permission`). Response `Permissions` is a list of `{ permissionId, delegatedFrom? }` so the agent can see which ids it holds for itself and which through a link; there is no token-wide `onBehalfOf` field. Remarks rewritten. `StartAgentTokenIssuance` unchanged. `GetAgentIdentity` returns the same per-id list. |
| `JWTBearerAuth` text and reviewer guide | Section 4. `OpenApiAgentBearer.PolicyScopesFromGrants` and its "two scopes grant one permission" exception are deleted; the scope string is the permission id. `open-api-bearer-security-tests.cs` probe table is rewritten per permission. |
| `EffectiveRolesResolver` | `DefaultWhenUnassignedAsync` no longer reads the principal store; an empty stored list resolves to the default role for every kind. `AdminLockoutGuards.SimulateEffectiveRoles` drops its `kind` parameter. The IEffectiveRolesResolver Design SSOT is rewritten. Tests `RolelessAgent_Should_YieldNoRoles` and `RolelessService_Should_YieldNoRoles` invert. |
| `IPermissionEvaluator` | Signature becomes `(PrincipalId, CredentialContext)` where the context carries the credential type and, for a bearer, the token's `TokenPermission` list with per-id `DelegatedFrom`. One expansion path: `effective(P)` from the four sources, intersected with the token list when present. `IAgentCallerContext` and `IAgentPermissionScopeSource` are removed. `PrincipalRoleClaimsTransformation` runs for every scheme. The external-PDP swap note survives. |
| Endpoint auth declarations | Every hosted contract loses its scheme list except the first-party set (section 6). The reference `tw-web-api-contracts/references/endpoint-auth.md` is rewritten: one policy per permission id, no scheme table, a first-party section. |
| `endpoint-authorize-policy-agreement-analyzer` (TWA0024) | Unchanged in rule. The api host's registered set changes from `agent-scope:*` literals to `PermissionIds` through `AddPermissionPolicies`, so the agent-scope test cases in `endpoint-authorize-policy-agreement-analyzer-tests.cs` become plain permission cases. |
| New analyzer | A convention analyzer flags `AuthenticationSchemes` on a hosted `[EndpointAuthorize]` unless `Requires = FirstParty` is also set, and flags `Requires = FirstParty` without a Design-region justification string. `AnalyzerReleases.Unshipped.md` gets the id. |
| `endpoint-auth-posture-analyzer` (TWA0013, TWA0014, TWA0020) | Unchanged. |
| Agent-link feature | Generalised to delegator and delegate. `AgentHumanLink` gains `DelegatedPermissions` and a Revoked status reachable from Approved. `ApproveAgentHumanLink` takes the delegated list and is first-party. `RevokeAgentHumanLink` is new. The three handlers' `PrincipalKind` checks (request: caller is an Agent; approve and deny: caller is a Human) follow Q7 and are not settled here: if Steve keeps the link agent-to-human, they stay, the revoke handler gets the same check, and their Design region says they define the product shape of the link and are not a capability rule; if he generalises the link, they are deleted and the entity's `AgentPrincipalId` and `HumanPrincipalId` become `DelegatePrincipalId` and `DelegatorPrincipalId`. Either way they are the only kind checks left in the stack after the resolver change. `GetHumanUx` is unchanged. |
| `features/metered-capability` and `timewarp-402` | The gate and ledger are unchanged. A new `PurchaseCapability` operation in a `features/capability-purchases` slice uses `PaymentGate` and `SettlementFundingService`, then writes the purchased-permission row. A purchasable catalog (`PurchasablePermissionOptions`: permission id, price, duration?) is host configuration validated at start. `InvokeMeteredCapability` keeps `demo.invoke` and keeps billing per call. |
| Feedback | `ReplyToFeedback` becomes permission-only on `admin.feedback.manage` unless Q6 keeps it first-party. `DownloadFeedbackAttachment` keeps `[EndpointAuthorize]` with no `Policy` and loses its scheme list; the handler stays the whole rule (section 6.1, handler-level checks): an unlinked draft is owner-only, and a linked attachment opens for the owner or for a credential whose effective set holds `feedback.read` or `admin.feedback.read`. The per-scheme branch, `IAgentPermissionScopeSource`, and the scheme sentences in the handler's Design region go; the region states the rule above. A principal holding only `feedback.read` (today's feedback-reviewer agent after the section 7.2 copy) still passes. The `admin.profiles.read` gate in `FeedbackOwnerNames` and the principal reads stays and is evaluated against the credential. |
| Agent chat | `CompleteAgentChat` and `GetAgentChatConfiguration` need a permission id instead of the authenticated policy (section 10). |
| api host | `agent-bearer-policies-server.cs` and `AgentTokenDefaults.IdentityReadPolicy`/`DemoInvokePolicy` are replaced by `AddPermissionPolicies` over claim type `timewarp:permission`. The bearer handler stamps one `timewarp:permission` claim per token id and, for each id with a `DelegatedFrom`, one `timewarp:delegated_from` claim whose value is `<permissionId>:<delegatorPrincipalId>`, so the first-party check and audit read claims only. |
| `GetCurrentSession` | Projects `effective(P)` for the session, with sources, so the SPA `AuthorizeView` names keep working. |
| Tests to invert or delete | `Role_Assigned_Agent_Should_Not_Inherit_Human_Grants`, `AgentToken_NeverGrantsAdmin_EvenWithAdminRolesInStore`, `AgentScopeSeed_Should_NotIntersectAdminPermissions`, `MemberScopes_Should_NotApproveARolelessAgent`, the 466 `AgentScopeApproval_` class, and the agent-only 401 pins. New pins: ceiling, not-self, delegation cap at issuance, subset token, purchase to grant, default-for-every-kind. |
| Skills | `tw-web-api-contracts` (endpoint-auth reference), `tw-deploy` (durable token store note unchanged). No new documentation page (memory: documentation tree retired). |

## 6. Endpoint table

Legend: **Permission** means any authenticated credential holding the id passes. **First-party**
means the credential must be the subject's own: a browser session, or a token on which every id
the operation relies on has `DelegatedFrom = null` (section 2.5). A token that also carries
delegated ids for other operations still passes; only the ids this operation uses are checked.
Stated per operation with the reason. **Anonymous** is unchanged. "Was" is today's scheme list:
H human session (plus mock), A agent token, HA both.

### 6.1 Change from human-only or agent-only to permission-only

| Operation | Route | Permission | Was |
|---|---|---|---|
| ListPrincipals, GetPrincipal | `GET api/admin/principals[/{id}]` | `admin.principals.read` | H |
| SetPrincipalRoles | `PUT api/admin/principals/{id}/roles` | `admin.principals.manage` + ceiling + not-self | H |
| SetPrincipalPermissions (was SetPrincipalAgentScopes) | `PUT api/admin/principals/{id}/permissions` | `admin.principals.manage` + ceiling + not-self | H |
| GetRoles, GetRole | `GET api/Roles[/{id}]` | `admin.roles.read` | H |
| CreateRole, UpdateRole, DeleteRole | `api/Roles[/{id}]` | `admin.roles.manage` | H |
| SetRolePermissions | `PUT api/Roles/{id}/permissions` | `admin.roles.manage` + ceiling | H |
| SubmitFeedback, ListMyFeedback, GetFeedback, ReplyAsFiler, UploadFeedbackAttachment, RemoveFeedbackAttachment | `api/Feedback…` | `feedback.file.self` | H |
| ReplyToFeedback | `POST api/admin/feedback/{id}/replies` | `admin.feedback.manage` (Q6: Steve may keep this first-party instead, in which case it moves to 6.2) | H |
| UpdateProfile | `PUT api/Users/Current/Profile` | `profile.write` | H |
| GetUserXaiApiKeyStatus / SetUserXaiApiKey / ClearUserXaiApiKey | `api/Users/Current/xai-api-key` | `profile.read` / `profile.write` | H |
| GetSiteSettings | `GET api/settings` | `settings.read` | H |
| UpdateSiteSettings, UpdateAssistantName, UpdateXaiByokMode, ResolveEntraTenant, GetXaiApiKeyStatus, SetXaiApiKey, ClearXaiApiKey | settings and admin/ai routes | `settings.write` | H |
| CompleteAgentChat, GetAgentChatConfiguration | `api/agent-chat/…` | new id, section 10 | H |
| ListAgentHumanLinks | `GET api/agent-links` | `agent-link.manage.self` | HA |
| RequestAgentHumanLink | `POST api/agent-links` | `agent-link.manage.self` | A |
| GetHumanUx | `GET api/agent-links/{id}/human-ux` | `agent-link.manage.self` (link Approved, caller is the delegate) | A |
| GetAgentIdentity | `GET api/identity/agent/me` | `identity.read` | A |
| InvokeMeteredCapability | `GET api/demo/metered-capability` | `demo.invoke`, then the ledger | A |
| GetCredentials, AddAgentKey, RenameCredential | `api/identity/credentials…` | `credential.manage.self` | HA (no mock) |
| ListFeedback, GetFeedbackForAdmin, AddFeedbackNote, SetFeedbackSeverity, ChangeFeedbackStatus | `api/admin/feedback…` | `feedback.read` / `.note` / `.severity` / `.status` | HA |
| DownloadFeedbackAttachment | `GET api/Feedback/attachments/{id}` | none on the attribute: `[EndpointAuthorize]` with no `Policy`, today's shape minus the scheme list. The handler rule below is the whole rule | HA |
| api host GetAgentBearerIdentity | `GET api/agent/bearer/me` | `identity.read` | A |

Handler-level checks that survive the scheme split. These are resource rules inside a handler,
not endpoint policies, and they stay; what changes is that each is evaluated against the
credential's effective set (the token's list for a bearer) with no scheme branch:

- `DownloadFeedbackAttachment`, in handler order, with no attribute policy in front of it:
  1. No principal: 401. No attachment row: 404.
  2. The attachment has no `FeedbackItemId` (an unlinked draft): the owner passes; every other
     caller is 404 before any permission is consulted. This gate is unchanged.
  3. The attachment is linked to an item: the owner passes; otherwise the credential's effective
     set (the token's list for a bearer) must hold `feedback.read` or `admin.feedback.read`, or
     the caller is 404.
  4. A row whose bytes are gone is 404 "attachment no longer available", unchanged.

  Today step 3 asks a different disjunction per scheme (`feedback.read` only on the agent-token
  scheme; `admin.feedback.read` or `feedback.read` on the session schemes). After the change it
  asks one disjunction of the credential. The attribute requires nothing beyond authentication
  because no single id covers the owner arm and both reviewer arms, so any attribute permission
  would exclude a caller the handler allows: a reviewer whose only feedback id is `feedback.read`
  (today's `feedback:read` agent after section 7.2) or an administrator holding
  `admin.feedback.read` without `feedback.file.self`. Both pass step 3; the filer passes as
  owner.
- `admin.profiles.read` inside `ListPrincipals`, `GetPrincipal`, and the feedback admin reads
  (`FeedbackOwnerNames`): the PII gate that nulls display names and emails, and 403s a profile
  search or sort, when the caller lacks it. It stays exactly as it is, evaluated against the
  credential. A bearer token that carries `admin.profiles.read` now sees names, which today's
  "an agent token is never granted `admin.profiles.read`" comment in `feedback-admin-view-contracts`
  rules out; that comment becomes a grant decision and is rewritten.
- `GetFeedback`, `ReplyAsFiler`, `ListMyFeedback`, and the attachment upload and remove: filer-only
  by resource ownership, unchanged.

### 6.2 Stay first-party, with the reason

| Operation | Route | Permission | Reason the credential must be the subject's own |
|---|---|---|---|
| StartPasskeyRegistration, CompletePasskeyRegistration, StartPasskeyAuthentication, CompletePasskeyAuthentication | `api/identity/passkey/…` | anonymous | WebAuthn ceremonies. The proof is the authenticator gesture in the browser; there is no principal yet, or the principal is being proven. Presence is intrinsic to the protocol, not a scheme rule. |
| AddPasskey, StartAddExistingPasskey, CompleteAddExistingPasskey | `api/identity/credentials/passkey…` | `credential.manage.self`, first-party | Adds authentication material to the subject. A delegate must not mint a way to become its delegator. The WebAuthn half also needs a browser authenticator. |
| RevokeCredential | `api/identity/credentials/{id}/revoke` | `credential.manage.self`, first-party | Can lock the subject out. A delegated credential may not remove the delegator's keys. |
| ApproveAgentHumanLink, DenyAgentHumanLink, RevokeAgentHumanLink | `api/agent-links/{id}/…` | `agent-link.manage.self`, first-party | Changes the delegation graph. If a delegate could approve links on the delegator's behalf, it could widen its own delegation: self-escalation by one hop. |
| ApproveCapabilityRequest, DenyCapabilityRequest | `api/admin/capability-requests/{id}/…` | `admin.principals.manage`, first-party | Approval is the grant act. A delegated credential approving with the delegator's permission would let the delegator's capability be spent by a principal the delegator never saw act. The not-self check also needs the real actor. |
| EndBrowserSession, SignOutBrowserSession, ChallengeEntra, Entra bootstrap choice operations | `api/identity/session/end`, `entra/…` | anonymous | Browser-cookie lifecycle. A bearer token has no session to end or create; these are credential-type operations, not capability operations. |
| Any future legal consent operation | none today | per operation | Listed here so the mechanism is named: `Requires = FirstParty` plus a Design-region reason, never a blanket scheme rule. |

First-party is about delegation and credential provenance, not kind. An agent approving a link
on its own principal with its own token is first-party and passes.

### 6.3 Unchanged

Anonymous discovery and ceremony operations (`GetSignInMethods`, `GetCurrentSession`,
`GetProfile` dual mode, `GetAssistantIdentity`, `SubmitTip`, `Hello`, `TrackEvent`,
`ForwardBrowserLogs`, agent-key registration and token issuance starts and completes) keep
`[EndpointAllowAnonymous]` with their reasons. Client-only contracts (todo items,
`GetCurrentUser`) and the SignalR hub are out of scope.

## 7. Migration

### 7.1 Existing tokens

The token store is in memory on every host (331's note in `tw-deploy`). The deploy that ships this
change restarts the host, which invalidates every live token; agents reissue with their key as
they already do after a 401. No token migration is needed. If a durable token store lands first,
the new validator rejects any stored grant without a permission list (fail closed) and the agent
reissues.

### 7.2 Stored scope grants

One EF migration (section 5) copies `identity.principal_agent_scopes` into
`identity.principal_permission_grants` and `identity.role_agent_scopes` into
`identity.role_permissions` through the 466 scope map, then drops both tables. Rows for
`identity:read` become `identity.read` plus `agent-link.manage.self`. The migration always
copies the full expansion. It skips a copied row only when the id is already present on that
principal or role (the natural key), never because the id is expected in the default bundle:
the bundle is Steve's answer to Q1 and Q2, a host may trim it later in the admin UI, and a
dropped row would silently remove link capability that today's scope map grants. A grant row
that duplicates a default is harmless (effective is a union) and the Principal page shows it as
a grant chip beside the default chip. In-memory stores need nothing. The `Down`
migration recreates the two tables but cannot reconstruct which permission rows came from scopes,
so it leaves `role_permissions` as is; this is stated in the migration's Design region.

### 7.3 Token requests from agents

The issuance body field changes from `scopes` (colon ids) to `permissions` (dot ids). A request
that still sends `scopes` fails validation with 400 naming the field, and the OpenAPI document is
the guide. Section 10 asks whether to accept colon ids as aliases for one release.

### 7.4 Generated apps

The template ships this. A maintainer of an existing generated app:

1. Bumps `TimeWarp.Identity` and `TimeWarp.402` to the released version in
   `Directory.Packages.props`.
2. Takes the template diff for `platform/authorization`, `features/identity`,
   `features/agent-links`, `features/admin`, the new slices, the analyzer and generator packages,
   and the SPA pages. There is no template upgrade tool; this is a manual merge as with every
   template change.
3. Adds the migration to their own migration history. The template's migration file compiles
   against the template snapshot; an app with its own snapshot runs `dev db add-migration` after
   taking the model changes and reviews the generated SQL against section 7.2.
4. Reviews every contract in their own slices for `AuthenticationSchemes` and removes it or adds
   `Requires = FirstParty` with a reason. The new analyzer makes the build fail until they do.
5. Tells any outside agent to request `permissions` instead of `scopes` and to read the new
   `JWTBearerAuth` text. Tokens last at most an hour, so there is no long tail.
6. Decides their default bundle. The seed migration applies the template's choice; a host that
   wants a tighter default edits the default role's permissions in the admin UI.

## 8. Implementation task breakdown

Scope, dependencies, and proof gates only. Same-repo batches run serially through one
`ganda task work-set` (memory: never parallel workers). Each task is a kanban task created with
`ganda kanban create`; the numbers here are order, not ids.

| # | Task | Scope | Depends on | Proof gates |
|---|---|---|---|---|
| 1 | Identity package: permissions on the token | Add the `TokenPermission(PermissionId, DelegatedFrom?)` record; `AgentTokenGrant.Scopes` becomes `Permissions` as a list of it; `IAgentTokenStore.Issue` takes that list and no token-wide `onBehalfOf`; delete `AgentScopes`. Bump `<Version>` and the two pins. | none | `dev build` 0/0; identity tests; `dev check-version`; release cut after merge (`tw-release`) |
| 2 | Default and effective permissions | Kind-free `EffectiveRolesResolver`; `IPrincipalPermissionStore` with EF and in-memory stores; `authorization_audit` store; `EffectivePermissions` service with sources; evaluator without scheme split; default bundle per Steve's answer to Q1; migration converting 466 tables; `SetPrincipalPermissions` with ceiling, not-self, audit; `SetRolePermissions` ceiling; remove role agent scopes and the two UI boxes; Principal page effective view and grant editor. | 1 | `dev build`; resolver, evaluator, set-permissions, get-principal co-located runfiles; web-server integration suite; `dev template-smoke --matrix` (postgres-off excludes) |
| 3 | Token issuance by permission | `CompleteAgentTokenIssuance` takes `Permissions`, subset check against effective, 403 with missing ids and request hint, response fields; `GetAgentIdentity` returns permissions; bearer handler stamps `timewarp:permission` claims; OpenAPI security scopes are permission ids; reviewer guide text; delete `OpenApiAgentBearer` inversion. | 2 | Issuance integration class; `open-api-bearer-security-tests`, `open-api-agent-reviewer-guide-tests` rewritten; `dev build` |
| 4 | Endpoints authorize by permission | Attribute gains `Requires`; generator emits all schemes by default and the first-party requirement; new analyzer id; every hosted contract updated per section 6; `DownloadFeedbackAttachment` keeps no policy and its handler collapses to the one disjunction in section 6.1; `endpoint-auth.md` rewritten; api host moves to permission claims and `AddPermissionPolicies`; replace the two authenticated policies with the permission from Q5; delete the agent-only and human-only pins and add permission-only pins (an agent token on `ListPrincipals` with `admin.principals.read` passes; without it, 403). | 3 | Analyzer and generator tests; `dev build`; full web and api integration suites; download pins (a bearer holding only `feedback.read` gets 200 on another owner's linked attachment and 404 on their unlinked draft; a session holding `admin.feedback.read` without `feedback.file.self` gets 200 on the linked one); `dev template-smoke --matrix` |
| 5 | Capability requests | New slice: request, mine, list, approve, deny; approve and deny carry `Requires = FirstParty` with a Design-region reason (section 6.2); ceiling, not-self, audit; admin page; 403 problem detail hint from issuance and endpoints; guide text. | 2, 3, 4 | Slice co-located tests; integration test of the full loop by an agent token from OpenAPI text; a token whose `admin.principals.manage` has a `DelegatedFrom` gets 403 on approve and deny while a token holding it as its own passes; new-analyzer test that approve and deny declare `Requires`; `dev build` |
| 6 | Delegation | Link gains delegated list and revoke; approve takes the list; first-party on approve, deny, revoke; issuance cap `D ∩ effective(delegator)` with own-sources-first provenance; `DelegatedFrom` set per token id and `OnBehalfOf` on the audit row; Agent links page picker and revoke button; the three handlers' `PrincipalKind` checks kept or removed per Steve's answer to Q7 (section 5, agent-link row). | 3, 4 | Link co-located tests; issuance tests for cap, removal at next token, revoke; `dev build`; integration suite |
| 7 | Purchases | `PurchasablePermissionOptions` with validator; `PurchaseCapability` through `PaymentGate` and `SettlementFundingService`; `principal_purchased_permissions` store (idempotent by receipt, optional expiry); evaluator source; OpenAPI 402 text with price; guide text. Depends on 3 for the guide text and the purchased id on the next token, and on 4 so the new endpoint is declared permission-only from the start. | 2, 3, 4 | Payment package tests with mocked facilitator (104-012 pattern); purchase then token then call integration test; `dev build` |
| 8 | Skills and reference | `tw-web-api-contracts` endpoint-auth reference, `tw-deploy` token note, lint. | 4 | `dev lint-skills`; `dev check-client-names` |

Tasks 5, 6, and 7 each depend on 3 and 4 (the Depends-on cells say so) and are independent of
each other after those two land. Task 4 ships the `Requires` attribute and the analyzer with the
credential and link operations in section 6.2 as the first-party set at that point; tasks 5 and
6 add their own first-party operations, and the analyzer makes a missing `Requires` on a new
contract a build error only when the contract still names a scheme, so the task 5 proof gate
pins the first-party behaviour directly. Task 8 rides with 4 if the reference is small.

## 9. Risks

- **Blast radius of task 4.** Every hosted contract changes. The analyzer makes a missed one a
  build error, and the posture analyzer already fails a contract with no auth marker, so the
  risk is wrong permission ids rather than missed endpoints. TWA0024 catches those.
- **Admin surface reachable by tokens.** After task 4 a bearer token with `admin.roles.manage`
  edits roles. The ceiling and audit are what make that acceptable; task 2 must land them before
  task 4 opens the endpoints, which the dependency order enforces.
- **Default bundle for agents widens.** Agents gain `feedback.file.self`, `profile.*`,
  `settings.read`, and `credential.manage.self` by default. 104-015 rate limits on registration
  are the Sybil control; the ledger and `TrustTier` stay orthogonal. Q1 and Q10 let Steve narrow.
- **Memory stores.** Token store, credit ledger, and role names are in memory. Purchased
  permissions in EF outlive a ledger that resets on restart, which is the right way round.
- **Two hosts.** The api sample host validates bearers against an in-memory store it does not
  share with web; this design does not fix that, and the sample stays a sample.

## 10. Open questions for Steve

One decision per item. The design does not settle these.

1. **Default role shape.** Member is the default for every kind with the bundle in section 2.3,
   or a new default role holds that bundle and Member stays a granted role?
2. **Default bundle contents.** Section 2.3 proposes seven ids. Remove any? In particular, should a
   self-signed-up principal get `credential.manage.self` and `feedback.file.self` before anyone
   vouches for it?
3. **Grant ceiling mechanism.** A grantor may grant only what it holds (section 2.6, proposed), or
   separate `grant.<prefix>` permissions decide what each grantor may give?
4. **Request-time evaluation for tokens.** The token's list alone (revocation at next token, as
   the task describes), or the token's list intersected with the live effective set on every
   request (immediate revocation, one store read per request as sessions already do)?
5. **Agent chat permission.** `CompleteAgentChat` and `GetAgentChatConfiguration` spend the site's
   xAI key. Which permission id gates them, and is it in the default bundle?
6. **Reply to the filer.** 378 kept `ReplyToFeedback` human-only so an agent never emails a
   filer. Under this model it is `admin.feedback.manage` for anyone who holds it. Accept that, or
   keep it first-party as a product choice?
7. **Link shape.** Keep "agent to human" (only an Agent may request, only a Human may delegate),
   or generalise to any principal delegating to any other principal?
8. **Acting as the delegator.** Delegation grants permission ids evaluated against the delegate's
   own subject (section 3.3). Is acting on the delegator's resources (its profile, its feedback)
   wanted? That is impersonation and a different design.
9. **Transitive delegation.** May a delegate re-delegate permissions it holds only by delegation?
   Proposed no.
10. **Trust tier and the default.** Should the default bundle differ for Provisional versus Keyed
    or Funded principals (a credential-posture rule, not a kind rule), or is tier only for
    metering as today?
11. **Purchasable set and terms.** Which permission ids may be bought, at what price, for how long
    (one-time, expiring, or until revoked), and may an administrator revoke a purchased
    permission?
12. **Partial approval.** May an approver approve some of a request's ids and deny others, or is a
    request all-or-nothing?
13. **Who approves.** `admin.principals.manage` plus the ceiling (proposed), or a dedicated
    `capability.approve` permission so an approver need not hold the full principal editor?
14. **Alias period.** Accept colon scope ids (`feedback:read`) in the token request as aliases for
    one release, or break at once with a 400 that names the new field?
15. **Token request default.** Is `permissions` required, or does an omitted list mean "everything
    I hold"? Proposed required, for least privilege.
16. **Session subset.** Should a browser session ever carry a subset (a "sudo" or step-up mode),
    or is the subset rule for bearer tokens only?
17. **Audit retention and visibility.** Who may read `authorization_audit` (a new
    `admin.audit.read`?) and does the template prune it?
18. **First-party versus a present person.** The task names "operations that really need a
    present person". Section 6.2 implements that as first-party: the credential is the subject's
    own, so an agent approving a link or a capability request on its own principal with its own
    token passes, and only delegated ids are refused. Accept that reading, or should some rows
    in 6.2 (link approve and deny, request approve and deny, credential revoke) require a
    browser session, meaning a human at a keyboard? The second answer reintroduces one
    credential-type rule, named per operation, and keeps the passkey rows as they are.
