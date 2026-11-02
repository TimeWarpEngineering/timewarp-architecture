# Principal-neutral capabilities: design

Task 468. Design only. No product code changes on this task. Steve reviews this document before
any implementation task is filed. Section 10 records his decisions of 2026-11-03 on the first
draft's open questions; section 10 also records Q19–Q22, which this revision raised, and section 11 is empty.

Decision this design serves (Steve, 2026-11-02): what a principal may do does not depend on
whether it is a human, an agent, or a service account. A principal that nobody vouches for signs
up with a minimal default and gets more capability by requesting it (an approver grants it) or
through delegation from a principal that vouches for it. An agent does not need a linked human.
Payment, when it arrives, is an automated grantor, not a fourth kind of permission (section 10,
"Purchases").

## 1. Where we are

Two vocabularies, two grant paths, and kind checks spread across the stack.

| Piece | Today |
|---|---|
| Permission ids | 23 `PermissionIds` constants in `source/container-apps/web/features/authorization/permission-ids-contracts.cs` (template code). Policy name equals permission id. |
| Agent scopes | 7 `AgentScopes` constants in `source/libraries/timewarp-identity/tokens/agent-scopes.cs` (the `TimeWarp.Identity` package, source in this repo, pinned `2.0.0-beta.30` for generated apps). Colon form (`identity:read`) against the dot form of permissions (`identity.read`). |
| Scope to permission | `AgentScopePermissionSeed` (compile-time map, not persisted). `identity:read` expands to two permissions, the other six to one each. No scope reaches any `admin.*`, `developer.*`, `profile.*`, `settings.*`, or `feedback.file.self`. |
| Roles | Four fixed `RoleIds` (Member, Operator, Administrator, Developer). `RolePermissionSeed` bundles. Role names live in a static dictionary, not a table. Role permission rows in `identity.role_permissions`. Member is `SelfServicePermissions`: `profile.read`, `profile.write`, `settings.read`, `credential.manage.self`, `agent-link.manage.self`, `feedback.file.self`. |
| Role scopes and principal scopes | Task 466: `identity.role_agent_scopes` and `identity.principal_agent_scopes` (migration `20261102105501_AddAgentScopeGrants`), two stores each, `SetRoleAgentScopes` and `SetPrincipalAgentScopes`, an "Agent scopes" box on the role page and two agent-scope sections on the Principal page. |
| Default | `EffectiveRolesResolver`: a roleless human (or a missing row) is Member; a roleless agent or service has no role. `identity:read` is always approved at issuance. |
| Evaluator | `IPermissionEvaluator` branches on authentication scheme. Human session: effective roles then role permissions, live from the store. Agent token: the token's scopes through the seed. Never both. |
| Endpoints | `[EndpointAuthorize(Policy, AuthenticationSchemes)]`. The scheme list decides which credential kinds may call. Human-only: 34 operations. Agent-only: 4 plus the api-host sample. Both: 14. |
| Token | Opaque, in-memory store, 15 minutes default, claims `timewarp:principal_id` and one `timewarp:scope` per scope. The agent names scopes; issuance refuses unapproved ones with 403 `unapproved_scope`. |
| Agent link | `features/agent-links`: Pending, Approved, Denied. Approve is human-only and grants nothing. No revoke. |
| Payment | `MeteredCapabilityGate` debits a per-principal credit ledger or runs the x402 flow per call. Settlement promotes `TrustTier.Funded`. No payment creates a permission. Ledger is in memory. |
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

### 2.2 Three sources, one union

For every principal, whatever its kind, acting as itself:

```
effective(P) = Principal bundle ∪ roles(P) ∪ grants(P)
```

The access check never asks how an id was obtained. A grant is a grant.

| Source | Store | Who writes it | Today's nearest thing |
|---|---|---|---|
| Roles | `IPrincipalRoleStore` plus `IRolePermissionStore` (unchanged). The Principal role (section 2.3) is held by every principal without a row. | Holder of `admin.principals.manage` (assign) and `admin.roles.manage` (edit bundle), within the ceiling (section 2.6) | Same |
| Direct grants | New `principal_permission_grants` (PrincipalId, PermissionId, GrantedBy, GrantedAt, RequestId?) | Holder of `admin.principals.manage`, within the ceiling | Task 466's `principal_agent_scopes`, generalised from scopes to permissions |
| Delegation | The agent-link entity grows a delegated permission list `D` | The linked human, on its own link | The link, which grants nothing today |

Delegation is the third source and is reachable only through a bearer token that acts on behalf
of the linked human (section 2.5). It is evaluated live at issuance:

```
delegated(A acting for H) = D ∩ effective(H)      evaluated at issuance
```

The other two sources are stored facts. Payment is not a source: the x402 flow stays pay-per-call
through the credit ledger, creates no row in any permission store, and `TrustTier` stays a
rate-limit and metering input. A billing component, when one is written, is an ordinary grantor
that creates and removes `principal_permission_grants` rows under the same ceiling and audit as
an administrator (section 10, "Purchases").

### 2.3 One default for every self-signed-up principal: the Principal role

A new seeded role **Principal** (`RoleIds.Principal`) holds the default bundle. Every principal
holds it, whatever its kind and whether or not it has a stored role row; `EffectiveRolesResolver`
returns the stored roles plus Principal, and kind no longer appears in it. Member stays an
ordinary granted role with its current `SelfServicePermissions` bundle. The Principal bundle is
a floor, so assigning Member never removes a default id, and a host that wants a tighter floor
edits the Principal role's permissions in the admin UI like any other role.

The Principal bundle (the ids exist today unless marked new):

| Permission | Why it is in the default |
|---|---|
| `identity.read` | A principal can read who it is (`GetAgentIdentity`, today agent-only). |
| `agent-link.manage.self` | Request, list, approve, and deny links on its own principal. |
| `credential.manage.self` | Rotate its own keys or passkeys. Already self-service for humans and the `credential:manage` scope for agents. |
| `profile.read`, `profile.write` | Its own profile row. |
| `settings.read` | Passkey prompt mode and sign-in offers, needed by the SPA shell. |
| `capability.request.self` (new) | File a request for more capability (section 4). |

Not in the default: `feedback.file.self` (sign-up spam; it stays in Member, so filing feedback is
a vouched capability), anything `admin.*`, `developer.*`, `settings.write`, the four `feedback.*`
reviewer ids, `demo.invoke`, and `agent-chat.invoke` (section 2.4). Those arrive by grant, role,
or delegation.

This replaces task 466's kind-based Member exception and its hard-coded `identity:read` default.
A human who signs up today becomes Member by the resolver default and can file feedback; after
this change a new human holds Principal and files feedback only once someone grants Member or
`feedback.file.self`. Existing humans keep what they have through the migration in section 7.2.
Bootstrap (`BootstrapAdministratorOptions`) and the first-admin rule are unchanged and already
kind-neutral.

### 2.4 Endpoints authorize by permission

An endpoint states the permission it needs. Any authenticated principal holding it passes,
whatever its kind and whatever credential authenticated it. Concretely:

- `[EndpointAuthorize]` keeps `Policy`. `AuthenticationSchemes` is no longer the way to choose a
  principal kind and no hosted contract names one. The generator emits
  `AuthSchemes(all host credential schemes)` for every protected endpoint, or the permission
  policies carry the scheme list through `AddPermissionPolicies` (the reason endpoint-auth.md
  gives for not doing that, scheme-aware expansion in the evaluator, goes away with the scheme
  split). One of the two; the implementation task picks and documents it in the
  `IPermissionEvaluator` Design region.
- A short, explicit list of operations carries `Requires = CredentialRequirement.ActingAsSelf`
  (section 6.2): the credential must act as its own holder, so a token issued on behalf of a
  linked human is refused there. That is a per-operation property on the attribute with a
  Design-region reason, not a scheme list, and it is the only credential requirement an
  endpoint can state. There is no browser-session rule anywhere (section 10, Q18).
- The non-permission policies `IdentitySessionDefaults.AuthenticatedPolicy` and
  `XaiChatDefaults.AuthenticatedPolicy` go away. Every protected operation names a permission id,
  with one exception: `DownloadFeedbackAttachment` keeps an `[EndpointAuthorize]` with no
  `Policy` (any host credential, authenticated), because its rule is a resource disjunction that
  no single id expresses and the handler is the whole rule (section 6.1). It is the only hosted
  operation with that shape today and stays the only one.
- Agent chat (`CompleteAgentChat`, `GetAgentChatConfiguration`) spends the site's xAI key. It
  gets a new id, `agent-chat.invoke`, outside the Principal bundle and seeded into every other seeded
  role: Member, Operator, Developer, and Administrator (section 10, Q22). Today the two chat operations use `XaiChatDefaults.AuthenticatedPolicy`, so any
  signed-in human can chat, and the resolver adds Member only when the stored role list is
  empty. After the migration every signed-in human who can chat today still can, including
  roleless humans (the Member backfill, section 7). Only the Principal default lacks it. The
  id is **session-only**: it is in a
  small `PermissionIds.SessionOnly` set that token issuance refuses (403 `not_issuable`), so no
  bearer token reaches the chat however the id was granted. That is a credential rule (the
  bearer credential type cannot carry it, whoever holds the key), not a kind rule. A human's
  own agent key is refused the same way. Bring-your-own-key use is unaffected: the user's own
  key is a profile field, not a permission.
- The api host's `agent-scope:*` claim policies become permission-claim policies over the same
  ids (`RequireClaim("timewarp:permission", PermissionIds.IdentityRead)`), so the sample endpoint
  reads like a web endpoint.

Build checks:

- TWA0024 (policy registered by this server) is unchanged and still the agreement check.
- A new analyzer id flags `AuthenticationSchemes` on any hosted `[EndpointAuthorize]`, so kind
  selection cannot creep back in through a scheme string, and flags `Requires = ActingAsSelf`
  without a Design-region justification. The acting-as-self list is the attribute usages
  themselves; the OpenAPI sentence for those operations is generated from the attribute, so the
  two cannot disagree.
- The seed test `AgentScopeSeed_Should_NotIntersectAdminPermissions` is replaced by a grant
  ceiling test (section 2.6) and a session-only test (`agent-chat.invoke` is never on a token).

### 2.5 A bearer token acts as someone and carries a subset

A bearer-token request names two things:

1. **Who the token acts as.** `OnBehalfOf` is null (the token acts as its holder, on the holder's
   own grants) or the principal id of a human the holder has an Approved link to (the token acts
   as that human). One token acts as exactly one subject; there is no mixed token.
2. **The permission ids the token should carry.** `Permissions` is required (1 to 32 ids,
   validated against `PermissionIds.All`; an unknown id is 400 `invalid_permission`; a
   session-only id is 403 `not_issuable`).

Issuance computes the ceiling for the subject and refuses when the request names anything
outside it: 403 problem details listing the missing ids and the operation that files a request
for them (section 4).

```
acting as self:     ceiling = effective(A)
acting for H:       ceiling = D(link A→H) ∩ effective(H)      evaluated now, at issuance
```

The token carries exactly the approved list plus `SubjectPrincipalId`. Its holder is the
**actor**; the principal it acts as is the **subject**; for a self token they are the same id.
Every `*.self` permission and every resource-ownership check in a handler (the filer of a
feedback item, the owner of a profile or credential) is evaluated against the **subject**, so a
token acting for H reads and writes H's own resources. That is the point of the link. Every
audited action records both actor and subject (section 2.6), and a slice that already records an
author (feedback notes and replies) records the actor beside the subject.

The request-time evaluator for a bearer credential is the token's list alone. Revocation takes
effect at the next token: removing a permission from H removes it from A's on-behalf tokens at
A's next issuance, and tokens last `AgentTokenOptions.TokenLifetimeMinutes` (15 by default, 1 to
60), so that is the revocation latency, the same as today's approval removal. Per-request
re-evaluation against the live store is a later option, not part of this design (section 10,
Q4). Quarantine still revokes immediately because the bearer handler re-reads the principal.

A token acting for H carries only ids from the delegated ceiling; the holder's own ids are not
on it. An agent that needs both issues two tokens, one as itself and one for H, and uses each
where it fits. Tokens are short and reissue-with-key is cheap, so this costs one extra round
trip and keeps every token's subject unambiguous.

This is a credential feature. A browser session carries the whole effective set of its holder
and never a subset or another subject (section 10, Q16). A human who registers an agent key
gets the subset behaviour on that key's tokens. Nothing in the subset rule reads the principal's
kind.

### 2.6 Granting is a permission, with a ceiling

Rules, each kind-neutral:

1. **Grant permission.** Writing `grants(P)` needs `admin.principals.manage`. Writing a role's
   bundle needs `admin.roles.manage`. Assigning roles needs `admin.principals.manage`. Unchanged
   ids; the change is that any principal holding them may call the endpoint.
2. **Ceiling.** A grantor may grant only permission ids it holds itself at the moment of the
   grant, whether the grant lands on a principal, on a role bundle, or through an approved
   request. "No agent token ever holds `admin.*`" becomes "nobody receives `admin.*` from a
   principal that does not hold it". There are no `grant.<prefix>` permissions. The
   Administrator protected-core guard (`ProtectedCoreConflict`) and the last-administrator guard
   stay.
3. **No self-escalation.** The actor of a grant, role assignment, link approval, or request
   approval may not be the beneficiary. Approve and grant operations carry
   `Requires = ActingAsSelf` (section 6.2), so a token acting for H cannot approve or grant with
   H's permissions; the not-self check then compares the actor, which on such a token is the
   subject, against the target. A linked agent therefore cannot approve its own escalation
   through its human, and an administrator still cannot approve its own capability request; a
   second administrator does.
4. **Audit.** Every grant, revoke, role assignment, role-bundle change, link approval, link
   denial, link revocation, delegation change, and capability-request decision writes one row to
   `authorization_audit` (At, Actor, Subject, Action, TargetPrincipal?, TargetRole?,
   PermissionId?, Source: request id or link id). Actor and Subject are both present on every
   row; on an acting-as-self credential they are equal. Grant rows also carry `GrantedBy` and
   `GrantedAt` so the current state is self-describing without the audit table. Reading the
   audit needs the new `admin.audit.read` (`ListAuthorizationAudit`, section 6.4; seeded into
   Administrator). The template keeps the log forever and does not prune it.

### 2.7 Credential rules stay credential rules

Short lifetime, reissue-with-key, rate limits on the ceremonies (104-015), metering through the
ledger, session-only ids, and token revocation on principal quarantine (the bearer handler
re-reads the principal) apply to the bearer credential type whoever holds it. A human who
registers an agent key is under the same rules on that key. Browser sessions keep their own
rules. `TrustTier` stays a rate-limit and metering input. Nothing here moves to `PrincipalKind`.

## 3. Worked examples

### 3.1 An admin grants an unvouched agent a capability

1. The agent runs `start-agent-key-registration` and `complete-agent-key-registration`
   (anonymous). Principal kind Agent, tier Provisional then Keyed, effective permissions = the
   Principal bundle. No human is involved and no human is linked.
2. It issues a self token for `identity.read` and reads `/openapi/v1.json`. The `JWTBearerAuth`
   description names the Principal bundle, the request operation, and the 403 shape. It tries a
   token for `demo.invoke` and gets 403 listing `demo.invoke` as missing and
   `request-capability` as the operation to call.
3. It calls `POST api/capability-requests` with a self token carrying
   `capability.request.self`, naming `demo.invoke` and a reason. An administrator sees the
   request on the Capability requests page. No seeded role holds `demo.invoke` today:
   `RolePermissionSeed.DefaultGrants` gives it to no role, and no migration inserts it (the
   466 role scope store starts empty). Section 2.6 refuses a grant the grantor does not hold,
   so the approver in this example must already hold both `admin.principals.manage` and
   `demo.invoke`. A host that wants administrators to grant it seeds `demo.invoke` into
   Administrator first; without that seed the ceiling check refuses the approval. With the seed
   in place the administrator approves, and the handler writes a grant row with `GrantedBy`,
   marks the request Approved, and writes an audit row with Actor = the administrator,
   Subject = the administrator, Target = the agent.
4. Its next token may name `demo.invoke`. `InvokeMeteredCapability` authorizes by permission and
   then bills each call through the ledger or the x402 challenge as it does today. Permission
   says "may attempt"; the ledger says "paid for this call". Payment never wrote a grant.

### 3.2 An admin grants a feedback reviewer its permissions

1. The reviewer, human or agent, signs up and gets the Principal bundle. It files a capability
   request for `feedback.read`, `feedback.note`, `feedback.severity`, `feedback.status` with a
   reason (section 4), or an administrator starts from the Principal page.
2. An administrator who holds those four ids (Administrator does) approves the request. The
   handler checks the ceiling (approver holds all four), checks approver is not the requester,
   writes four grant rows with `GrantedBy`, marks the request Approved, and writes audit rows.
   Alternatively the administrator creates a "Feedback Reviewer" role with the four ids (one
   permissions box on the role page) and assigns it. Either path is one configuration, not two.
3. The reviewer's next session or token carries the four ids. `ListFeedback`,
   `GetFeedbackForAdmin`, `AddFeedbackNote`, `SetFeedbackSeverity`, `ChangeFeedbackStatus`
   authorize by permission. `ReplyToFeedback` needs `admin.feedback.manage`, which the reviewer
   does not hold; an agent that is granted it may reply (section 10, Q6). The "no reply to the
   filer" posture from 378 is a grant decision, not a kind rule.
4. The administrator later removes `feedback.status`. The grant row is deleted, an audit row
   records the revoke, and the reviewer's next token (or next session evaluation) no longer
   carries it.

### 3.3 A human delegates a subset to a linked agent

1. The agent requests a link to the human (`RequestAgentHumanLink`, existing; only an Agent may
   request and only a Human may approve, section 10, Q7). The human sees it on the Agent links
   page and approves it, choosing the delegated list `D`: say `feedback.file.self` and
   `profile.read`. Approve is acting-as-self (section 6.2), so a token acting for the human
   cannot approve it. An audit row records the delegation with Actor = Subject = the human.
2. The agent issues a token with `OnBehalfOf = human` naming `feedback.file.self` and
   `profile.read`. Issuance finds the Approved link, computes
   `D ∩ effective(human) = {feedback.file.self, profile.read}` (the human holds both through
   Member), and issues the token with `SubjectPrincipalId = human`.
3. With that token the agent calls `SubmitFeedback` and `ListMyFeedback`. The filer is the
   **subject**, so the item is the human's and appears in the human's own list; the feedback row
   records the agent as the actor beside the human as the filer. `GetProfile` returns the
   human's profile. `GetAgentIdentity` on this token reports actor, subject, and the two ids.
4. The agent also needs its own `identity.read` and `capability.request.self`: it issues a
   second token with `OnBehalfOf = null` for those. The two tokens never mix.
5. The human loses Member. At the agent's next on-behalf issuance the intersection is empty; a
   request naming `feedback.file.self` gets 403 listing it. The link still exists; it just
   delegates nothing the human no longer has.
6. The human revokes the link (new operation, acting-as-self). Audit row. The agent is back to
   its own sources; a live on-behalf token expires within `TokenLifetimeMinutes`.

No transitive delegation (section 10, Q9): a human never acts on behalf of anyone, so an
on-behalf ceiling is always `D ∩ (Principal bundle ∪ roles(H) ∪ grants(H))` and never contains
an id H holds only through someone else. Sub-agents are not modelled (section 10, Q7): an agent
that wants a worker mints another key on its own principal with a permission cap and hands it
over; that worker is the same principal, and every token from that key is capped by it
(section 10, Q21).

### 3.4 A human signs themselves up

1. `start-passkey-registration` and `complete-passkey-registration` (anonymous) mint a Human
   principal with no stored role. On an empty deployment the first human still becomes
   Administrator plus Member (existing first-admin rule, kind-neutral in effect because only
   humans run that ceremony today).
2. The session evaluator resolves Principal and the SPA receives the Principal bundle as
   `permission` claims. The person can edit their profile, manage passkeys, approve agent links,
   and file a capability request. They cannot file feedback, use the site-key assistant chat, or
   open `/Admin` until someone grants Member, `feedback.file.self`, `agent-chat.invoke`, or
   `admin.access`, or assigns a role. The SPA hides the feedback and chat entry points the same
   way it hides `/Admin` today, through `AuthorizeView` on the permission claim.
3. If this human registers an agent key for a script, that key's tokens carry any subset of the
   same effective set the browser session has, except session-only ids. There is no second
   allow-list.

## 4. The request-for-more-capability flow

New slice `features/capability-requests`, following `tw-web-api-contracts`:

| Operation | Route | Permission | Caller |
|---|---|---|---|
| `RequestCapability` | `POST api/capability-requests` | `capability.request.self` | Any principal. Body: permission ids (validated against `PermissionIds.All`), reason (bounded text). All-or-nothing: one unknown or session-only id refuses the whole request (400 or 403, naming the id). One open request per (subject, permission id); a duplicate is 409. The row records Actor and Subject, so an approver sees when a linked agent filed for its human. Response: request id and status Pending. |
| `ListMyCapabilityRequests` | `GET api/capability-requests/mine` | `capability.request.self` | The subject's requests; the requester polls status. |
| `ListCapabilityRequests` | `GET api/admin/capability-requests` | `admin.principals.manage` | Approvers see Pending requests, filterable by permission id and principal. |
| `ApproveCapabilityRequest` | `POST api/admin/capability-requests/{id}/approve` | `admin.principals.manage`, acting-as-self | All-or-nothing: ceiling check (approver holds every requested id, else 403 naming the ids it lacks), not-self check, writes every grant, audit. There is no partial approval. |
| `DenyCapabilityRequest` | `POST api/admin/capability-requests/{id}/deny` | `admin.principals.manage`, acting-as-self | Reason text returned to the requester. |

Who sees a pending request: holders of `admin.principals.manage` in the admin UI (a "Capability
requests" page beside Principals, with a badge count), and the requester through `mine`. There is
no notification system in the template; the Principal page and the list page are the inbox.

Who may approve: any principal holding `admin.principals.manage` and every requested id, acting
as itself, other than the requester (section 10, Q13). Holding `admin.principals.manage` without
holding `admin.roles.manage` lets you approve a request for `feedback.read` but not for
`admin.roles.manage`; an approver who cannot cover the whole request denies it or leaves it for
one who can.

How an outside agent learns about it from the OpenAPI document alone (task 331's goal):

- The `JWTBearerAuth` scheme description (`open-api-agent-reviewer-guide-server.cs`) lists the
  issuance steps as today, then: the Principal bundle every new principal holds, that a token
  request names `permissions` and may name `onBehalfOf` for an Approved link, that an id outside
  the subject's ceiling returns 403 listing the missing ids, that
  `request-capability (POST /api/capability-requests)` files a request for them, and that
  `list-my-capability-requests` shows its status. All of it is generated from `PermissionIds`,
  the Principal bundle, and the contract types, so it cannot drift.
- Every protected operation's security requirement lists its permission id as the scope string
  (`OpenApiBearerSecurity` already does this through a scope map; the map becomes the identity
  function). The description sentence becomes "Requires permission feedback.read", with "Must
  act as its own holder" appended on acting-as-self operations.
- The 403 problem details from issuance carry `missingPermissions` and `requestOperationId`.
  The 403 from an endpoint keeps `WWW-Authenticate: Bearer error="insufficient_scope"` and names
  the permission id in `scope=`.

## 5. Effect on each existing piece

| Piece | Change |
|---|---|
| `AgentScopes` (`TimeWarp.Identity`) | Removed. `AgentTokenGrant.Scopes` (strings) becomes `AgentTokenGrant.Permissions` (`IReadOnlyList<string>` of permission ids) and the grant gains `SubjectPrincipalId` (equal to `PrincipalId` for a self token). `IAgentTokenStore.Issue(principalId, credentialId, subjectPrincipalId, permissions, lifetime)`. Public API break: `<Version>` and the `TimeWarp.Identity` pin bump in the same commit (`dev check-version`), and a release follows the merge (memory: release when libs change; never design around a release). `TimeWarp.402` is untouched by this design. The package has no permission catalog and should not get one; ids stay template code. |
| `AgentScopePermissionSeed` | Deleted after the migration has used it once: the migration copies every stored scope through the seed's expansion into grant or role-permission rows (section 7.2), including both ids of `identity:read`. A copied id that is also in the Principal bundle is harmless, because `effective(P)` is a union and an id held twice is held once. |
| Task 466 role scope store | `IRoleAgentScopeStore`, `RoleAgentScopeGrant`, its EF configuration, in-memory and EF stores, `SetRoleAgentScopes`, the SPA action, and the "Agent scopes" box on `RoleDetailPage.razor` are removed. The Member role-page warning text goes with them. |
| Task 466 principal scope store | Becomes the direct-grant store: `IPrincipalPermissionStore`, `PrincipalPermissionGrant` (PrincipalId, PermissionId, GrantedBy, GrantedAt, RequestId?), EF table `identity.principal_permission_grants`. `SetPrincipalAgentScopes` becomes `SetPrincipalPermissions` (replace-set, ceiling check, not-self check, acting-as-self, audit). The Principal page shows "Effective permissions" with a source chip per id (Principal, role name, grant) and a "Direct grants" editor. `AgentScopeApproval`, `AgentScopeOrder`, and `AgentScopeGrantKind` are replaced by an `EffectivePermissions` service that returns ids with sources, used by issuance, the Principal page, and `GetCurrentSession`. |
| `RoleIds` and `RolePermissionSeed` | New `RoleIds.Principal` with the section 2.3 bundle. `agent-chat.invoke` joins Member, Operator, Developer, and Administrator; `admin.audit.read` joins `AdminPermissions`. `PermissionIds` gains `capability.request.self`, `agent-chat.invoke`, `admin.audit.read`, and a `SessionOnly` set containing `agent-chat.invoke`. |
| EF migration | One migration: create `principal_permission_grants`, `capability_requests`, `authorization_audit`; add `DelegatedPermissions` (and `RevokedAt`) to `agent_links.agent_links`; copy `principal_agent_scopes` rows through the scope-to-permission map (`identity:read` to two rows) with `GrantedBy = null`, `GrantedAt = migration time`; copy `role_agent_scopes` rows into `identity.role_permissions` through the same map, skipping duplicates; insert a Member role row for every Human principal that has no role row (today's resolver default made explicit); drop the two agent-scope tables. Also the seed inserts for Principal and the two new ids. |
| Admin UI | Role page: one permissions editor. Principal page: effective permissions with sources, direct-grant editor, pending requests for this principal, audit tail (shown when the viewer holds `admin.audit.read`). New Capability requests page and Audit page. Agent links page: delegated list picker on approve, revoke button. All through TimeWarp.State actions (`tw-blazor`). |
| Token contracts | `CompleteAgentTokenIssuance.Command.Scopes` becomes `Permissions` (required, 1 to 32 ids, validated against `PermissionIds.All`; unknown is 400 `invalid_permission`; session-only is 403 `not_issuable`; a body that still sends `scopes` is 400 naming `permissions`, with no alias period). The command gains `OnBehalfOf` (nullable principal id; must name a human the caller has an Approved link to, else 403 `no_approved_link`). Response gains `subjectPrincipalId` and returns `permissions`. Remarks rewritten. `StartAgentTokenIssuance` unchanged. `GetAgentIdentity` returns actor, subject, and the token's list. |
| `JWTBearerAuth` text and reviewer guide | Section 4. `OpenApiAgentBearer.PolicyScopesFromGrants` and its "two scopes grant one permission" exception are deleted; the scope string is the permission id. `open-api-bearer-security-tests.cs` probe table is rewritten per permission. |
| `EffectiveRolesResolver` | `DefaultWhenUnassignedAsync` no longer reads the principal store; the resolver returns stored roles plus Principal for every kind. `AdminLockoutGuards.SimulateEffectiveRoles` drops its `kind` parameter. The IEffectiveRolesResolver Design SSOT is rewritten. Tests `RolelessAgent_Should_YieldNoRoles` and `RolelessService_Should_YieldNoRoles` invert. |
| `IPermissionEvaluator` | Signature becomes `(CallerContext)` where the context carries actor, subject, credential type, and, for a bearer, the token's permission list. One expansion path: `effective(subject)` from the three sources for a session, the token's list for a bearer. `IAgentCallerContext` and `IAgentPermissionScopeSource` are removed; `ICallerContext` (actor, subject, credential type) replaces them for every scheme so a handler never reads a scheme to find the subject. `PrincipalRoleClaimsTransformation` runs for every scheme. The external-PDP swap note survives. |
| Endpoint auth declarations | Every hosted contract loses its scheme list. The acting-as-self set (section 6.2) gains `Requires = ActingAsSelf`. The reference `tw-web-api-contracts/references/endpoint-auth.md` is rewritten: one policy per permission id, no scheme table, an acting-as-self section, a session-only ids note. |
| `endpoint-authorize-policy-agreement-analyzer` (TWA0024) | Unchanged in rule. The api host's registered set changes from `agent-scope:*` literals to `PermissionIds` through `AddPermissionPolicies`, so the agent-scope test cases in `endpoint-authorize-policy-agreement-analyzer-tests.cs` become plain permission cases. |
| New analyzer | A convention analyzer flags `AuthenticationSchemes` on any hosted `[EndpointAuthorize]`, and flags `Requires = ActingAsSelf` without a Design-region justification string. `AnalyzerReleases.Unshipped.md` gets the id. |
| `endpoint-auth-posture-analyzer` (TWA0013, TWA0014, TWA0020) | Unchanged. |
| Agent-link feature | Stays agent → human (section 10, Q7). `AgentHumanLink` gains `DelegatedPermissions` and a Revoked status reachable from Approved. `ApproveAgentHumanLink` takes the delegated list; approve, deny, and the new `RevokeAgentHumanLink` are acting-as-self. The three handlers' `PrincipalKind` checks (request: caller is an Agent; approve and deny: caller is a Human) stay, the revoke handler gets the Human check, and their Design region says they define the product shape of the link (who may vouch for whom) and are not a capability rule. They are the only kind checks left in the stack after the resolver change. `GetHumanUx` is unchanged. |
| `features/metered-capability` and `timewarp-402` | Unchanged. No purchase slice, no purchased-permission store, no purchasable catalog. `InvokeMeteredCapability` keeps `demo.invoke` and keeps billing per call. A future billing component grants through `IPrincipalPermissionStore` under the ceiling of its own service principal and writes audit rows like any grantor. |
| Feedback | `ReplyToFeedback` becomes permission-only on `admin.feedback.manage` (section 10, Q6). `DownloadFeedbackAttachment` keeps `[EndpointAuthorize]` with no `Policy` and loses its scheme list; the handler stays the whole rule (section 6.1, handler-level checks): an unlinked draft is owner-only, and a linked attachment opens for the owner or for a credential whose effective set holds `feedback.read` or `admin.feedback.read`. Owner means the subject. The per-scheme branch, `IAgentPermissionScopeSource`, and the scheme sentences in the handler's Design region go; the region states the rule above. A principal holding only `feedback.read` (today's feedback-reviewer agent after the section 7.2 copy) still passes. The `admin.profiles.read` gate in `FeedbackOwnerNames` and the principal reads stays and is evaluated against the credential. Notes and replies record the actor beside the author. |
| Agent chat | `CompleteAgentChat` and `GetAgentChatConfiguration` authorize on `agent-chat.invoke`, a session-only id (section 2.4). `XaiChatDefaults.AuthenticatedPolicy` is deleted. |
| api host | `agent-bearer-policies-server.cs` and `AgentTokenDefaults.IdentityReadPolicy`/`DemoInvokePolicy` are replaced by `AddPermissionPolicies` over claim type `timewarp:permission`. The bearer handler stamps `timewarp:principal_id` (actor), `timewarp:subject_id`, and one `timewarp:permission` claim per token id, so the acting-as-self check (`principal_id == subject_id`) and audit read claims only. |
| `GetCurrentSession` | Projects `effective(P)` for the session, with sources, so the SPA `AuthorizeView` names keep working. |
| Tests to invert or delete | `Role_Assigned_Agent_Should_Not_Inherit_Human_Grants`, `AgentToken_NeverGrantsAdmin_EvenWithAdminRolesInStore`, `AgentScopeSeed_Should_NotIntersectAdminPermissions`, `MemberScopes_Should_NotApproveARolelessAgent`, the 466 `AgentScopeApproval_` class, and the agent-only 401 pins. New pins: ceiling, not-self, delegation cap at issuance, on-behalf subject on resource checks, acting-as-self refusal, subset token, session-only refusal, Principal-for-every-kind, Member backfill. |
| Skills | `tw-web-api-contracts` (endpoint-auth reference), `tw-deploy` (durable token store note unchanged). No new documentation page (memory: documentation tree retired). |

## 6. Endpoint table

Legend: **Permission** means any authenticated credential holding the id passes.
**Acting-as-self** means the credential's actor and subject are the same principal: a browser
session, or a bearer token issued with `OnBehalfOf = null`. A token acting for a linked human is
refused there with 403 `acting_on_behalf`. Stated per operation with the reason. **Anonymous** is
unchanged. "Was" is today's scheme list: H human session (plus mock), A agent token, HA both.

### 6.1 Change from human-only or agent-only to permission-only

| Operation | Route | Permission | Was |
|---|---|---|---|
| ListPrincipals, GetPrincipal | `GET api/admin/principals[/{id}]` | `admin.principals.read` | H |
| SetPrincipalRoles | `PUT api/admin/principals/{id}/roles` | `admin.principals.manage` + ceiling + not-self, acting-as-self (6.2) | H |
| SetPrincipalPermissions (was SetPrincipalAgentScopes) | `PUT api/admin/principals/{id}/permissions` | `admin.principals.manage` + ceiling + not-self, acting-as-self (6.2) | H |
| GetRoles, GetRole | `GET api/Roles[/{id}]` | `admin.roles.read` | H |
| CreateRole, UpdateRole, DeleteRole | `api/Roles[/{id}]` | `admin.roles.manage` | H |
| SetRolePermissions | `PUT api/Roles/{id}/permissions` | `admin.roles.manage` + ceiling, acting-as-self (6.2) | H |
| SubmitFeedback, ListMyFeedback, GetFeedback, ReplyAsFiler, UploadFeedbackAttachment, RemoveFeedbackAttachment | `api/Feedback…` | `feedback.file.self` (filer is the subject) | H |
| ReplyToFeedback | `POST api/admin/feedback/{id}/replies` | `admin.feedback.manage` | H |
| UpdateProfile | `PUT api/Users/Current/Profile` | `profile.write` (subject's profile) | H |
| GetUserXaiApiKeyStatus / SetUserXaiApiKey / ClearUserXaiApiKey | `api/Users/Current/xai-api-key` | `profile.read` / `profile.write` | H |
| GetSiteSettings | `GET api/settings` | `settings.read` | H |
| UpdateSiteSettings, UpdateAssistantName, UpdateXaiByokMode, ResolveEntraTenant, GetXaiApiKeyStatus, SetXaiApiKey, ClearXaiApiKey | settings and admin/ai routes | `settings.write` | H |
| CompleteAgentChat, GetAgentChatConfiguration | `api/agent-chat/…` | `agent-chat.invoke` (session-only id, section 2.4) | H |
| ListAgentHumanLinks | `GET api/agent-links` | `agent-link.manage.self` | HA |
| RequestAgentHumanLink | `POST api/agent-links` | `agent-link.manage.self` (handler keeps: caller is an Agent) | A |
| GetHumanUx | `GET api/agent-links/{id}/human-ux` | `agent-link.manage.self` (link Approved, caller is the agent) | A |
| GetAgentIdentity | `GET api/identity/agent/me` | `identity.read` | A |
| InvokeMeteredCapability | `GET api/demo/metered-capability` | `demo.invoke`, then the ledger | A |
| GetCredentials | `GET api/identity/credentials` | `credential.manage.self` (a read of the subject's credentials; permission-only) | HA (no mock) |
| AddPasskey, StartAddExistingPasskey, CompleteAddExistingPasskey, AddAgentKey, RenameCredential, RevokeCredential | `api/identity/credentials…` | `credential.manage.self`, acting-as-self (6.2) | HA (no mock) |
| ListFeedback, GetFeedbackForAdmin, AddFeedbackNote, SetFeedbackSeverity, ChangeFeedbackStatus | `api/admin/feedback…` | `feedback.read` / `.note` / `.severity` / `.status` | HA |
| DownloadFeedbackAttachment | `GET api/Feedback/attachments/{id}` | none on the attribute: `[EndpointAuthorize]` with no `Policy`, today's shape minus the scheme list. The handler rule below is the whole rule | HA |
| api host GetAgentBearerIdentity | `GET api/agent/bearer/me` | `identity.read` | A |

Handler-level checks that survive the scheme split. These are resource rules inside a handler,
not endpoint policies, and they stay; what changes is that each is evaluated against the
credential's effective set (the token's list for a bearer) with no scheme branch, and "owner"
or "filer" means the credential's **subject**:

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
  owner, and a token acting for the filer passes as owner because the owner is the subject.
- `admin.profiles.read` inside `ListPrincipals`, `GetPrincipal`, and the feedback admin reads
  (`FeedbackOwnerNames`): the PII gate that nulls display names and emails, and 403s a profile
  search or sort, when the caller lacks it. It stays exactly as it is, evaluated against the
  credential. A bearer token that carries `admin.profiles.read` now sees names, which today's
  "an agent token is never granted `admin.profiles.read`" comment in `feedback-admin-view-contracts`
  rules out; that comment becomes a grant decision and is rewritten.
- `GetFeedback`, `ReplyAsFiler`, `ListMyFeedback`, and the attachment upload and remove: filer-only
  by resource ownership, unchanged, with the filer compared to the subject.

### 6.2 Acting-as-self, with the reason

These operations carry `Requires = CredentialRequirement.ActingAsSelf` and refuse a token
issued on behalf of a linked human. Each is still permission-gated; the requirement is in
addition to the permission. Steve named the first three groups (section 10, Q18); the
credential group was added by this revision and confirmed by Steve (section 10, Q20).

| Operation | Route | Permission | Reason the credential must act as its own holder |
|---|---|---|---|
| ApproveAgentHumanLink, DenyAgentHumanLink, RevokeAgentHumanLink | `api/agent-links/{id}/…` | `agent-link.manage.self` | Changes the delegation graph. If a token acting for the human could approve links on the human's behalf, the agent could widen its own delegation: self-escalation by one hop. |
| ApproveCapabilityRequest, DenyCapabilityRequest | `api/admin/capability-requests/{id}/…` | `admin.principals.manage` | Approval is the grant act. A token acting for an administrator would let the administrator's ceiling be spent by a principal the administrator never saw act, and a linked agent could approve its own request through its human. The not-self check also needs the real actor. |
| SetPrincipalPermissions, SetPrincipalRoles, SetRolePermissions | `api/admin/principals/{id}/…`, `api/Roles/{id}/permissions` | `admin.principals.manage` / `admin.roles.manage` | The same grant act by a different door. A linked agent must not be able to grant itself (or a role it holds) anything through its human's ceiling. |
| AddPasskey, StartAddExistingPasskey, CompleteAddExistingPasskey, AddAgentKey, RenameCredential, RevokeCredential | `api/identity/credentials…` | `credential.manage.self` | Adds or removes authentication material on the subject. A token acting for the human must not mint a key that becomes the human permanently, outliving the link and its delegated list, and must not lock the human out. Section 11, Q2. |

Acting-as-self is about which subject a credential acts as, not kind or credential type. An
agent approving a link on its own principal with its own self token passes. A human's own agent
key, issuing self tokens, passes.

### 6.3 Unchanged

Anonymous discovery and ceremony operations (`GetSignInMethods`, `GetCurrentSession`,
`GetProfile` dual mode, `GetAssistantIdentity`, `SubmitTip`, `Hello`, `TrackEvent`,
`ForwardBrowserLogs`, the passkey registration and authentication ceremonies, agent-key
registration and token issuance starts and completes, `EndBrowserSession`,
`SignOutBrowserSession`, `ChallengeEntra`, and the Entra bootstrap choice operations) keep
`[EndpointAllowAnonymous]` with their reasons. The WebAuthn ceremonies need a browser
authenticator because the protocol does, not because of a scheme rule. Client-only contracts
(todo items, `GetCurrentUser`) and the SignalR hub are out of scope.

### 6.4 New operations

| Operation | Route | Permission | Section |
|---|---|---|---|
| RequestCapability, ListMyCapabilityRequests | `api/capability-requests[/mine]` | `capability.request.self` | 4 |
| ListCapabilityRequests | `GET api/admin/capability-requests` | `admin.principals.manage` | 4 |
| ApproveCapabilityRequest, DenyCapabilityRequest | `api/admin/capability-requests/{id}/…` | `admin.principals.manage`, acting-as-self | 4, 6.2 |
| RevokeAgentHumanLink | `POST api/agent-links/{id}/revoke` | `agent-link.manage.self`, acting-as-self | 3.3, 6.2 |
| ListAuthorizationAudit | `GET api/admin/audit` | `admin.audit.read` | 2.6 |

## 7. Migration

### 7.1 Existing tokens

The token store is in memory on every host (331's note in `tw-deploy`). The deploy that ships this
change restarts the host, which invalidates every live token; agents reissue with their key as
they already do after a 401. No token migration is needed. If a durable token store lands first,
the new validator rejects any stored grant without a permission list and a subject (fail closed)
and the agent reissues.

### 7.2 Stored scope grants and existing principals

One EF migration (section 5) copies `identity.principal_agent_scopes` into
`identity.principal_permission_grants` and `identity.role_agent_scopes` into
`identity.role_permissions` through the 466 scope map, then drops both tables. Rows for
`identity:read` become `identity.read` plus `agent-link.manage.self`. The migration always
copies the full expansion. It skips a copied row only when the id is already present on that
principal or role (the natural key), never because the id is in the Principal bundle: a host may
trim the bundle later in the admin UI, and a dropped row would silently remove link capability
that today's scope map grants. A grant row that duplicates a default is harmless (effective is
a union) and the Principal page shows it as a grant chip beside the Principal chip.

The same migration inserts a Member role row for every Human principal that has no role row, so
every human who could file feedback yesterday can file it tomorrow; the resolver's kind
exception becomes stored data and is then deleted. Agents and services with no role row get
nothing inserted and hold Principal, which is wider than today's "no role" and is the intent.

In-memory stores need nothing. The `Down` migration recreates the two tables but cannot
reconstruct which permission rows came from scopes or which Member rows it inserted, so it
leaves `role_permissions` and `principal_roles` as they are; this is stated in the migration's
Design region.

### 7.3 Token requests from agents

The issuance body field changes from `scopes` (colon ids) to `permissions` (dot ids) at once,
with no alias period (section 10, Q14). A request that still sends `scopes` fails validation
with 400 naming `permissions`, and the OpenAPI document is the guide. `permissions` is required
(section 10, Q15).

### 7.4 Generated apps

The template ships this. A maintainer of an existing generated app:

1. Bumps `TimeWarp.Identity` to the released version in `Directory.Packages.props`.
2. Takes the template diff for `platform/authorization`, `features/identity`,
   `features/agent-links`, `features/admin`, `features/agent-chat`, the new slices, the analyzer
   and generator packages, and the SPA pages. There is no template upgrade tool; this is a
   manual merge as with every template change.
3. Adds the migration to their own migration history. The template's migration file compiles
   against the template snapshot; an app with its own snapshot runs `dev db add-migration` after
   taking the model changes and reviews the generated SQL against section 7.2, including the
   Member backfill.
4. Reviews every contract in their own slices for `AuthenticationSchemes` and removes it. The
   new analyzer makes the build fail until they do. Adds `Requires = ActingAsSelf` with a
   reason to any of their own approve or grant operations.
5. Tells any outside agent to request `permissions` instead of `scopes`, that `onBehalfOf`
   exists, and to read the new `JWTBearerAuth` text. Tokens last at most an hour, so there is no
   long tail.
6. Decides their default bundle. The seed applies the template's Principal bundle; a host that
   wants a tighter or wider default edits the Principal role's permissions in the admin UI.

## 8. Implementation task breakdown

Scope, dependencies, and proof gates only. Same-repo batches run serially through one
`ganda task work-set` (memory: never parallel workers). Each task is a kanban task created with
`ganda kanban create`; the numbers here are order, not ids.

| # | Task | Scope | Depends on | Proof gates |
|---|---|---|---|---|
| 1 | Identity package: permissions and subject on the token | `AgentTokenGrant.Scopes` becomes `Permissions` (`IReadOnlyList<string>`); the grant gains `SubjectPrincipalId`; `IAgentTokenStore.Issue` takes both; delete `AgentScopes`. Bump `<Version>` and the `TimeWarp.Identity` pin. | none | `dev build` 0/0; identity tests; `dev check-version`; release cut after merge (`tw-release`) |
| 2 | Principal role, direct grants, audit | `RoleIds.Principal` and its bundle; `capability.request.self`, `agent-chat.invoke` (in `SessionOnly`), `admin.audit.read` in `PermissionIds` and the seeds; resolver returns stored roles plus Principal for every kind, no kind parameter; `IPrincipalPermissionStore` with EF and in-memory stores; `authorization_audit` store and `ListAuthorizationAudit`; `EffectivePermissions` service with sources; `ICallerContext` (actor, subject, credential type) and the evaluator without scheme split; migration converting the 466 tables and backfilling Member for roleless humans; `SetPrincipalPermissions` with ceiling, not-self, audit; `SetPrincipalRoles` and `SetRolePermissions` ceiling and audit; remove role agent scopes and the two UI boxes; Principal page effective view, grant editor, audit tail; Audit page. | 1 | `dev build`; resolver, evaluator, set-permissions, get-principal co-located runfiles; ceiling pin (a grantor lacking `admin.roles.manage` cannot grant it); Member backfill pin on the migration; web-server integration suite; `dev template-smoke --matrix` (postgres-off excludes) |
| 3 | Delegation data on the link | `AgentHumanLink` gains `DelegatedPermissions` and Revoked; `ApproveAgentHumanLink` takes the list (validated against `PermissionIds.All`); `RevokeAgentHumanLink` with the Human kind check; audit rows with Actor and Subject; Agent links page picker and revoke button. Kind checks on the three handlers stay with a Design-region sentence. | 2 | Link co-located tests (approve stores the list, revoke from Approved only); `dev build`; integration suite |
| 4 | Token issuance by permission and subject | `CompleteAgentTokenIssuance` takes required `Permissions` and nullable `OnBehalfOf`; `AddAgentKey` and `CompleteAgentKeyRegistration` take an optional `Permissions` cap stored on the credential, and issuance intersects every ceiling with the key's cap; `scopes` is 400 naming `permissions`; self ceiling `effective(A)`, on-behalf ceiling `D ∩ effective(H)` over an Approved link; session-only ids 403 `not_issuable`; 403 with missing ids and the request hint; response and `GetAgentIdentity` carry actor, subject, permissions; bearer handler stamps `timewarp:principal_id`, `timewarp:subject_id`, `timewarp:permission`; OpenAPI security scopes are permission ids; reviewer guide text; delete `OpenApiAgentBearer` inversion. | 2, 3 | Issuance integration class: self subset, on-behalf cap, cap shrinks at next token after the human loses a role, a capped key cannot exceed its cap, revoked link refuses, `agent-chat.invoke` refused, `scopes` body is 400; `open-api-bearer-security-tests`, `open-api-agent-reviewer-guide-tests` rewritten; `dev build` |
| 5 | Endpoints authorize by permission | Attribute gains `Requires`; generator emits all schemes and the acting-as-self requirement; new analyzer id; every hosted contract updated per section 6; handlers compare owner and filer to the subject through `ICallerContext`; notes and replies record the actor; `DownloadFeedbackAttachment` keeps no policy and its handler keeps the section 6.1 order (an unlinked draft stays owner-only before any permission is consulted; only the linked cross-owner check in step 3 becomes one disjunction of the credential, `feedback.read` or `admin.feedback.read`, with no scheme branch); `endpoint-auth.md` rewritten; api host moves to permission claims and `AddPermissionPolicies`; the two authenticated policies become `agent-chat.invoke`; delete the agent-only and human-only pins and add permission-only pins. | 4 | Analyzer and generator tests (a scheme list is an error; `ActingAsSelf` without a reason is an error); `dev build`; full web and api integration suites; permission-only pins (an agent token on `ListPrincipals` with `admin.principals.read` passes; without it, 403); acting-as-self pins (an on-behalf token on `ApproveAgentHumanLink` and `SetPrincipalPermissions` is 403 `acting_on_behalf`; a self token holding the id passes); subject pins (an on-behalf token's `SubmitFeedback` files as the human and the row records the agent as actor); download pins (a bearer holding only `feedback.read` gets 200 on another owner's linked attachment, 404 on another owner's unlinked draft, and 200 on the bearer's own unlinked draft; a session holding `admin.feedback.read` without `feedback.file.self` gets 200 on the linked one); `dev template-smoke --matrix` |
| 6 | Capability requests | New slice: request (all-or-nothing validation, Actor and Subject on the row), mine, list, approve (all-or-nothing, ceiling, not-self, audit), deny; approve and deny carry `Requires = ActingAsSelf` with a Design-region reason; admin page with badge; 403 problem detail hint from issuance; guide text. | 5 | Slice co-located tests; integration test of the full loop by an agent token from OpenAPI text; an on-behalf token on approve and deny gets 403 while a self token holding `admin.principals.manage` and the ids passes; an approver lacking one requested id gets 403 naming it; new-analyzer test that approve and deny declare `Requires`; `dev build` |
| 7 | Skills and reference | `tw-web-api-contracts` endpoint-auth reference, `tw-deploy` token note, lint. | 5 | `dev lint-skills`; `dev check-client-names` |

Task 5 ships the `Requires` attribute and the analyzer with the section 6.2 set as it stands at
that point (links, grants, credentials); task 6 adds its own two operations and pins them. Task 7
rides with 5 if the reference is small.

## 9. Risks

- **Blast radius of task 5.** Every hosted contract changes. The analyzer makes a missed scheme
  list a build error, and the posture analyzer already fails a contract with no auth marker, so
  the risk is wrong permission ids rather than missed endpoints. TWA0024 catches those.
- **Subject versus actor in handlers.** Every ownership check must compare the subject, and every
  audit or author field must record the actor. A handler that reads the actor for ownership
  gives an on-behalf token the agent's resources instead of the human's. Mitigation: one
  `ICallerContext` type is the only way to learn who is calling, the actor is a separate,
  deliberately named member, and task 5 pins `SubmitFeedback` on an on-behalf token.
- **Admin surface reachable by tokens.** After task 5 a bearer token with `admin.roles.manage`
  edits roles. The ceiling, acting-as-self on the grant operations, and audit are what make that
  acceptable; task 2 must land them before task 5 opens the endpoints, which the dependency
  order enforces.
- **New humans cannot file feedback.** The Principal bundle has no `feedback.file.self`, so a
  self-signed-up human needs Member before the feedback page works. The migration backfills
  existing humans; a host that wants yesterday's behaviour adds the id to the Principal role in
  the admin UI. The SPA must hide the feedback entry point on the claim so a new user does not
  meet a 403.
- **Memory stores.** Token store, credit ledger, and role names are in memory. Grants and the
  audit log are in EF, which is the right way round.
- **Audit growth.** The log is never pruned by the template. A busy host adds its own retention
  outside the template; the Audit page pages by date.
- **Two hosts.** The api sample host validates bearers against an in-memory store it does not
  share with web; this design does not fix that, and the sample stays a sample.

## 10. Decided (Steve, 2026-11-03)

Each line records the answer the design now follows. The numbers are the first draft's open
questions; "Purchases" was raised across several of them.

- **Purchases.** Not a source. The access check never asks how a grant was obtained. Payment,
  when it comes, is an automated grantor in a billing component that creates and removes
  ordinary grants. x402 is pay-per-call through the credit ledger and creates no grant. Tiers are
  rate limits. Sources are roles, direct grants, and delegation (section 2.2). The purchase
  worked example is recast as an admin granting an unvouched agent (section 3.1). Trust-tier
  defaults and the purchasable set (old Q10 and Q11) are out of scope.
- **Q1 Default role shape.** A new default role named **Principal** holds the default bundle.
  Member stays an ordinary granted role (section 2.3).
- **Q2 Default bundle.** The section 2.3 proposal without `feedback.file.self` (sign-up spam).
  `credential.manage.self` and the rest stay.
- **Q3 Grant ceiling.** A grantor may grant only what it holds. No `grant.<prefix>` permissions
  (section 2.6).
- **Q4 Request-time evaluation.** Revocation takes effect at the next token. Tokens carry their
  list; no per-request re-check. Per-request evaluation is a later option (section 2.5).
- **Q5 Agent chat.** `CompleteAgentChat` and `GetAgentChatConfiguration` spend the site's xAI
  key. They get their own permission, `agent-chat.invoke`, outside the Principal bundle, and are
  not reachable with a bearer token from the API (session-only id, section 2.4). Bring-your-own-
  key use is unaffected.
- **Q6 Reply to the filer.** `admin.feedback.manage` for any principal that holds it. An agent
  holding it may reply (section 3.2).
- **Q7 Link shape.** Links stay agent → human. An agent that wants a sub-agent mints a key with
  a smaller permission set and hands it over; the key's cap (Q21) enforces the smaller set
  (section 3.3).
- **Q8 Acting as the delegator.** A linked agent **acts as the human** on the human's own
  resources. Each token request names who it acts as: as itself (its own grants) or on behalf
  of its linked human (the human's delegated subset, capped live by what the human holds). Every
  audited action records both the actor (the agent) and the subject (the human) (section 2.5).
- **Q9 Transitive delegation.** No (section 3.3).
- **Q12 Partial approval.** Requests are all-or-nothing. Asking for anything the requester may
  not ask for refuses the whole request (section 4).
- **Q13 Who approves.** An admin: `admin.principals.manage` plus the ceiling. No separate
  approver role for now (section 4).
- **Q14 Alias period.** None. The token request switches from `scopes` to `permissions` at once;
  the old field returns a 400 naming the new one (section 7.3).
- **Q15 Token request default.** `permissions` is required (section 2.5).
- **Q16 Session subset.** No subset for browser sessions. Subsets are for bearer tokens only
  (section 2.5).
- **Q17 Audit.** Add `admin.audit.read`. Keep the audit log forever; no pruning in the template
  (section 2.6).
- **Q18 Browser-session rule.** None. Ask-AI approvals need nothing special (agents do not hold
  those permissions), and confirm-before-delete is screen UX, not API. Approve and grant
  operations (link approve and deny, capability request approve and deny, permission grants)
  refuse a token acting on someone's behalf, so a linked agent cannot approve its own
  escalation. A principal acting as itself, human or agent, may approve within its ceiling
  (section 6.2).
- **Q19 Agent chat enforcement.** `agent-chat.invoke` is in `PermissionIds.SessionOnly`, which
  token issuance refuses for every principal. The chat endpoints stay permission-only; there is
  no browser-session requirement on an endpoint (section 2.4).
- **Q20 Credential operations on behalf.** `AddPasskey`, `StartAddExistingPasskey`,
  `CompleteAddExistingPasskey`, `AddAgentKey`, `RenameCredential`, and `RevokeCredential` refuse
  a token acting on someone's behalf, like approvals and grants. `GetCredentials` (a read) stays
  permission-only (section 6.2).
- **Q21 Per-key permission cap.** A key may carry an optional permission cap, set when it is
  added or registered and stored on the credential. Issuance intersects every ceiling with it, so
  a key handed to a sub-agent can never exceed its cap (task 4).
- **Q22 Chat roles.** `agent-chat.invoke` is seeded into Member, Operator, Developer, and
  Administrator, so no signed-in human loses chat. Only the Principal default lacks it.

## 11. Open questions

None. Q19–Q22 were raised by the 2026-11-03 revision and decided the same day (section 10).
