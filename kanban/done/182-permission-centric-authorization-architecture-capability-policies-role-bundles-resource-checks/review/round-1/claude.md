# Round-1 review — permission-centric authorization (task 182)

**Reviewer:** Claude (Fable 5), 2026-08-12.
**Inputs:** `task.md`, `research/decision-brief.md`, and a full code sweep of every hotspot
(RolePolicyGrants, ModuleRequirement, web-server/api-server policy registration, RoleIds,
AuthorizationPolicyNames, EffectiveRolesResolver, claims transformation, first-admin, role
stores, agent scopes, mock auth, all `[Page]`/`[EndpointAuthorize]` policy consumers, tests).

---

## 1. Verdict

**Accept with amendments.**

The permission-centric target (enforcement names capabilities; roles are mutable bundles;
in-process PDP default; resource checks first-class; external PDP behind a port) is the right
architecture for this template, and the current code makes the case empirically, not
theoretically. Blocking and non-blocking amendments are listed in §5; the blocking ones are
about *how* Phase 1 is cut and *where* drift is killed, not about the target model.

## 2. Architecture assessment — does this beat RequireRole and the COPIC ceiling?

Yes, and the repo's own history is the strongest evidence:

**The codebase already wants capability enforcement — it just lacks the mechanism.** Every
policy name is already capability-shaped (`CanViewRolesPage`, not `RequireAdministrator`);
the Design regions say "roles are composed at registration, not in the name." But every one of
those capability names resolves to `RequireRole(<Administrator Guid>)`. The naming is a
convention held up by comments; the proposal turns it into a mechanism. That is a completion of
the existing design intent, not a foreign graft.

**Role-identity enforcement has already drifted at 12-policy scale.** Concretely, today:

- Each admin policy is defined in **three hand-maintained copies**: SPA
  (`RolePolicyGrants.Grants` → `RequireRole`), server (`program.cs` inline
  `AddPolicy…RequireRole`), and the contract (task 158 forced the full
  `AuthenticationSchemes` list to be repeated on every `[EndpointAuthorize]` because
  `Policies(...)` alone never invoked the mock handler). Nothing enforces parity — the only
  safeguard is a Design-region sentence.
- The two policy-name constant classes (`AuthorizationConstants.Policies`, SPA;
  `AuthorizationPolicyNames`, contracts substrate) mirror each other by hand; their
  intersection is exactly the two admin policies.
- Two policies (`CanViewAdminPage`, `CanViewUserClaims`) are declared, granted, and consumed by
  **nothing**.
- Non-admin contracts hardcode policy strings (`"credential-management"`,
  `"agent-scope:identity:read"`) with a comment as the only link to the registration.

If a 12-policy template with one product role in play already exhibits duplication, orphans, and
comment-enforced parity, a consumer app at 50 policies has no chance. The brief's drift argument
is understated if anything.

**The COPIC-shaped module layer is confirmed dead code**, which settles the "ceiling" question.
`ModuleRequirementHandler` is never registered in DI (the `// Register the custom requirements
handlers` comment in `web-spa/program.cs` is followed by `AddFluentUIComponents()`); no policy
constructs a `ModuleRequirement`; `ModuleIds` (16 ERP Guids) is consumed only by
`GetCurrentUser`, which is `[ClientOnlyContract]` with no server endpoint. Nothing has ever been
gated by it. It is not a lesser architecture to build on — it is an abandoned sketch.

**One assumption to challenge (and my resolution):** a template could argue the *simpler*
teaching story is RequireRole. I reject that: the template already outgrew it (three copies,
orphan policies, scheme duplication), and the proposal's registry actually *shrinks* the
consumer-facing surface — one registry file instead of two constant classes plus a grants map
plus inline server registration. The indirection cost is real but is paid once, in the
template, not per consumer.

**One conceptual gap the brief should make explicit:** current policy names are
**per-surface** (`CanViewRolesPage` gates two SPA pages *and* all five role CRUD endpoints —
read and write undifferentiated). Permissions should name **capabilities**
(`admin.roles.read` / `admin.roles.manage`), and surfaces then require a capability. The brief
says this implicitly ("atomic capabilities") but the seed vocabulary must not just rename the
current surface policies 1:1, or write access rides read forever. See §3 Q2.

## 3. Answers to decision-brief §7 review questions

### Q1 — Permission id format: Guid vs dotted string vs dual?

**Dotted string. Not Guid, not dual.**

- Permissions are **code vocabulary** (compile-time registry), unlike roles, which are
  admin-created data rows and correctly keep opaque Guid ids. The registry entry is the
  authority; the string is self-describing in 403 logs, grant rows, test assertions, and admin
  UI without a lookup.
- Policy name = permission id (Q2) only works readably with strings; a Guid policy name in an
  `[EndpointAuthorize]` attribute or a log line is hostile.
- Every external PDP on the seam roadmap (OpenFGA, SpiceDB, Cedar) uses string identifiers.
  Guids would force a mapping table at exactly the seam the epic wants clean.
- Agent scopes are already strings (`identity:read`); one vocabulary (Q7) argues strings.
- The stability argument for Guids is weaker than it looks: a renamed dotted string invalidates
  stored grants exactly as a regenerated Guid would. Stability is a *policy* ("ids are contract
  data; never change once issued" — the same rule `RoleIds` already documents), not a property
  of the id type.
- **Dual is the worst option**: a permanent Guid↔string mapping to maintain, for no consumer.

Format detail: pick `<area>.<concern>.<verb>` dotted lowercase (`admin.roles.manage`,
`profile.read`). Keep agent scopes' existing `:` wire format as-is; the scope→permission
mapping (Q7) absorbs the delimiter difference. Registry members are `const string` so
`nameof`-style refactor safety is replaced by compile-time reference safety plus an analyzer
(§5 non-blocking).

### Q2 — Policy name 1:1 with permission, or keep `CanView*` aliases?

**1:1: the policy name IS the permission id string.** Retire `CanView*` for every
permission-backed policy. The alias layer is precisely what produced today's dual constant
classes and hand-mirrored names; keeping aliases rebuilds the drift surface on day one.

Documented exceptions (not permission-backed, keep their names): `Authenticated`, `Anonymous`,
and scheme-composition policies (`credential-management`, `agent-scope:*` until Q7's mapping
lands). The registry helper should register permission policies; the exceptions stay explicit
and few.

Seed vocabulary (concrete proposal, replacing the current 12 policies):

| Permission | Replaces |
|---|---|
| `admin.access` | `CanViewAdminSidebarNavSection`, `CanViewAdminPage` (orphan — delete) |
| `admin.roles.read` | `CanViewRolesPage` (pages, GetRoles, GetRole) |
| `admin.roles.manage` | `CanViewRolesPage` on Create/Update/DeleteRole |
| `admin.principals.read` | `CanViewPrincipalsPage` (page, ListPrincipals) |
| `admin.principals.manage` | `CanViewPrincipalsPage` on SetPrincipalRoles |
| `developer.access` | `CanViewDeveloperSidebarNavSection`, `CanViewDeveloperPage` |
| `developer.claims.read` | `CanViewUserClaimsPage`, `CanViewUserClaims` (orphan — merge) |
| `profile.read` | `CanViewOwnProfile` |
| `settings.read` | `CanViewSettings` |

The read/manage split for admin is the one place I insist on more granularity than today:
undifferentiated `CanViewRolesPage` on write endpoints is exactly the coarseness the epic
exists to remove, and demonstrating the split is part of the teaching value. Everywhere else,
resist permission explosion — nav visibility rides the page permission (`admin.access` for the
sidebar section), not separate `nav.*` permissions.

### Q3 — Expand to claims at session issue vs evaluate store every request?

**Evaluate per-request. Never bake permissions (or roles) into the cookie.** This preserves
147-004 D8, which is already the right call: the identity-session cookie stays
PrincipalId-only, so rebundling a role takes effect on the next request with no cookie
reissue, no stale-grant window, and no logout ceremony after an admin edits a role.

Mechanics — and this shapes the seam, so be precise:

- The **authorization handler always routes through `IPermissionEvaluator`**. The evaluator is
  the port; nothing else may be. If the handler checked a projected claim directly, swapping in
  OpenFGA/Cedar (whose decisions are per-check, often per-resource, and not expressible as a
  claim set) would require rewriting the handler — the seam would be decorative.
- The **default in-process evaluator** expands principal → roles → permissions from the grant
  store, memoized per request (scoped service, same lifetime pattern as
  `EffectiveRolesResolver`, which it subsumes or wraps). Whether it additionally projects
  permission claims via the existing `PrincipalRoleClaimsTransformation` seam is an
  implementation detail of that evaluator — allowed as an optimization, forbidden as the
  contract.
- **SPA parity**: `GetCurrentSession.Response` grows an expanded `Permissions` list (its
  handler calls the same evaluator), and the SPA auth-state provider projects those as claims;
  SPA policies check the permission claim. Same semantics, one server-side expansion source —
  this is what makes invariant #2 ("single decision path") true rather than aspirational.
- A pleasant consequence of per-request expansion: the closed-box mock principal handler
  (`X-TimeWarp-Mock-Principal-Id`) keeps working with **zero changes** — the mock principal
  flows through the same transformation/evaluator as a real one. Claims-at-issue would have
  forked the mock path.

### Q4 — Type placement under TWA0009?

**Follow the proven substrate pattern; no new mechanism needed.** `RoleIds`,
`AuthorizationPolicyNames`, `IPrincipalRoleStore`, `EffectiveRolesResolver` et al. already live
in the bare `TimeWarp.Architecture.Features` namespace with documented TWA0009 justifications;
the permission types are the same kind of cross-slice vocabulary. Concretely:

- `permission-ids-contracts.cs` — the registry (`const string` members + `All`), substrate
  namespace. Contracts layer because `[EndpointAuthorize(Policy = PermissionIds.…)]` must see it.
- `i-permission-evaluator-application.cs`, `i-role-permission-store-application.cs` (+
  in-memory impl) — substrate namespace, application layer.
- `role-permission-grant-infrastructure.cs` + EF store — infrastructure layer, mirroring
  `PrincipalRoleAssignment`.
- `permission-requirement-server.cs` / handler — server layer; slice-or-substrate per the same
  litmus the claims transformation used (it landed in `…Features.Admin.Principals` — fine).
- **Folder home:** the current authz substrate files are scattered under
  `features/admin/principals/`. Task 132 (auth folder naming) is the coordination point —
  recommend a `features/authorization/` concern folder for the new types rather than deepening
  the `admin/principals/` pile, and fold that into 132's vocabulary decision rather than
  deciding it here.

One consolidation this epic must do (blocking, §5): the new registry **replaces both**
`AuthorizationConstants.Policies` and `AuthorizationPolicyNames`. One class, substrate
namespace, referenced by SPA pages, nav, contracts, and both hosts' registration. The dual
declaration is the disease; do not carry it into the cure.

### Q5 — Fate of ModuleRequirement + ERP ModuleIds?

**Delete, in Phase 1 — not Phase 4, and not "keep for demos."** The sweep verified it is
unreachable three independent ways (handler never registered; requirement never constructed;
`ModuleIds` consumed only by a client-only mock contract). It cannot be "left for demos"
because it demonstrates nothing — it has never gated anything — and its presence directly
contradicts invariant #2 (single decision path) for anyone reading the template. Scope of the
delete: `module-requirement*.cs`, `module-ids-contracts.cs`, `AuthorizationState.Modules` (+
its fetch action's module projection), the `Modules` field of `GetCurrentUser.Response`, and
the stale registration comment in `web-spa/program.cs`. The permission registry is the
replacement vocabulary; nothing to adapt. (Repo standing rule applies: no churn arguments —
carrying known-dead code through a rearchitecture to save a small diff is the wrong trade.)

### Q6 — Phase 1 boundary vs more children?

**Phase 1 as written is too big for one child; split it.** The sweep found a fact the brief
doesn't state: **there is no role aggregate and no role-permission storage at all.** A role is
`Guid + Name + Description` in an in-memory `ConcurrentDictionary` stub ("Stub until 147-004");
the only durable authz data is the `PrincipalRoleAssignment` join row. So "registry + seed +
evaluator + replace enforcement" is not a swap — it includes standing up new persistence.
Recommended children (each independently green, `dev build` 0/0):

1. **182-A Model:** permission registry; role→permission grant store (dual-mode in-memory/EF,
   mirroring the principal-role store pattern); seed (Administrator → all `admin.*` + rest,
   Member → self-service, Developer → developer set, Operator → reserved-empty);
   `IPermissionEvaluator` + default implementation; co-located Jaribu tests for expansion
   semantics (empty store, bootstrap union, ordering). **No enforcement change** — RequireRole
   still in force; nothing user-visible moves.
2. **182-B Server enforcement swap:** permission policies registered from the registry via one
   shared helper; `PermissionRequirement` handler; the 7 admin contracts move to
   `PermissionIds.*`; delete the two inline `RequireRole` policies; update/extend the existing
   integration suites (`roles-authorization-tests`, `principals-authorization-tests`) —
   including a new case proving read≠manage (member-with-read forbidden from Create).
3. **182-C SPA swap + dead-code delete:** `GetCurrentSession` returns permissions; auth-state
   provider projects them; SPA policy registration iterates the same registry helper; retire
   `RolePolicyGrants`, `AuthorizationConstants.Policies`, `AuthorizationPolicyNames`, the inert
   `PagePolicyRegistration`/`NavigationPolicyRegistration` placeholders, and the Q5 module
   deletions; update mock SPA provider to carry permission claims; **first SPA-side authz
   tests** (registry-composition round-trip at minimum — SPA authorization is currently
   entirely untested).
4. Phases 2–4 as spec'd, one child each, with the §4 lockout amendment attached to Phase 2.
   Q7's scope mapping is its own child (Phase-4-adjacent), not a Phase 1 blocker.

Ordering note: A→B→C is strictly sequential; B and C must not interleave with other work, since
between B and C the SPA still evaluates roles while the server evaluates permissions — the seed
must keep them observably equivalent during that window (it does, if the seed mirrors today's
grants).

### Q7 — Agent scope ↔ permission mapping shape?

**Scopes become named permission bundles for agents — structurally the same thing roles are for
humans.** Registry maps `identity:read` → `{identity.read}`, `credential:manage` →
`{credential.manage.self}`, `demo:invoke` → `{demo.invoke}`. The evaluator, for an
`agent-token` principal, grants P iff some held scope's bundle contains P. This:

- keeps one vocabulary (invariant: agents map into permission ids) while scopes stay
  wire-stable strings on tokens;
- rationalizes the `credential-management` two-arm `RequireAssertion` into one rule — cookie
  principals hold `credential.manage.self` via their role bundle, agents via the scope bundle —
  instead of an asymmetric special case;
- gives defense in depth for admin surfaces: `admin.*` appears in **no** scope bundle, so even
  if the scheme restriction were misconfigured, no agent token can satisfy an admin policy.
  **Keep the scheme restriction too** — the `Unauthorized_Given_Agent_Bearer_Token_No_Cookie`
  pinned boundary stays.

Do this as its own child after Phase 1 (the three existing agent-scope policies keep their
current shape until then). One caution for that child: `PrincipalRoleClaimsTransformation`
already fires for agent principals (they carry `timewarp:principal_id`), silently projecting
`Member` effective roles onto agent tokens today. Harmless now (no agent-reachable policy
checks roles), but the permission evaluator must be **scheme-aware from day one** so agent
principals never inherit human self-service permissions by accident.

## 4. Risks

**Privilege escalation / lockout via Phase 2 UI (the biggest one, and the brief is silent on
it).** Today there is **no last-admin protection**: `SetPrincipalRoles` will happily strip the
last Administrator, including your own (only guards are principal-404 and role-id validity).
The proposal adds a second, worse lockout lever: once role bundles are editable, removing
`admin.roles.manage` / `admin.principals.manage` from the last role that grants them bricks
administration for the whole deployment even though an "Administrator" still exists. Greenfield
recovery (`dev db reset`) makes this survivable in dev, but the template teaches production
shape. **Amendment (blocking on Phase 2, not Phase 1):** ship lockout guards *with* the
editing surfaces — (a) last-admin guard on `SetPrincipalRoles` (409 when removing the last
principal holding a role that grants `admin.principals.manage`), (b) protected-core guard on
role editing (the permissions governing permission/role administration cannot be removed from
the last role granting them — or simpler: Administrator is a system role whose `admin.*` core
is not removable). These are also the natural Phase 3 resource-check exemplars — fold Phase 3's
exemplar into the Phase 2 child or land them together; do not ship the editing UI in a state
where the exemplar "will come later."

**Dual-evaluation drift (SPA vs server).** The proposal fixes role-composition drift but can
reproduce it one level up if SPA and server each hand-register permission policies — exactly
how we got two constant classes and three copies per policy. The cure must be structural:
**one registry, one registration helper consumed by both hosts** (blocking), plus an analyzer
making `[EndpointAuthorize(Policy=…)]` accept only registry constants (non-blocking, but this
repo's standing directive — prefer analyzers over convention-by-memory — points straight at
it; today's hardcoded `"credential-management"` literals are the existing violation it would
catch). The task-158 scheme-list duplication on every contract is a third copy of each policy
that this epic should at least not worsen — a candidate follow-up is the FastEndpoint generator
emitting schemes from policy metadata (non-blocking child).

**SPA trust.** Invariant #5 already right. Two specifics: `GetCurrentSession` must return only
the *caller's* expanded permissions (it's `[EndpointAllowAnonymous]`); and the read-only
Permissions catalog page (Phase 2) must be gated `admin.*` — the registry is code, not secret,
but a catalog endpoint enumerating every capability to anonymous callers is free recon.

**TWA0009.** Low risk — the substrate pattern is established and every existing authz substrate
type carries a documented justification; the new types are the same category. Coordinate the
folder home with task 132.

**Migration cost.** Bounded and mostly mechanical. `RoleIds` has ~30 call sites but **survives
this epic** (roles remain first-class as bundles; only enforcement stops naming them) — the
swap touches the 7 admin contracts, two inline server policies, `RolePolicyGrants` and its
registration chain, the session endpoint, mock providers, and ~9 test files. The genuinely new
work is the grant store (§3 Q6). `api-server` needs nothing in Phase 1: it has no role concept
at all, its only policies are agent-scope, and its one endpoint is anonymous — leave it until
Q7's child or a real api surface needs permissions.

**Test surface.** Server-side coverage is good and pins the right boundaries (anonymous 401,
agent 401, member 403, admin 200). SPA-side authorization has **zero tests** today —
Phase 1c must not inherit that (see child 182-C).

## 5. Amendments

**Blocking (disposition must adopt before children are cut):**

1. **Split Phase 1 into three sequential children** (model / server swap / SPA swap +
   dead-code delete) per §3 Q6 — the brief's Phase 1 hides a new persistence surface.
2. **Single registry + single registration helper** consumed by both SPA
   `AddAuthorizationCore` and server `AddAuthorizationBuilder`; the registry **replaces both**
   `AuthorizationConstants.Policies` and `AuthorizationPolicyNames`. No second constants class
   survives.
3. **`IPermissionEvaluator` is the only decision seam** — the authorization handler routes
   through it unconditionally; claim projection is permitted only as an internal optimization
   of the default evaluator (§3 Q3). Write this into the ADR as the swap contract.
4. **ModuleRequirement / ModuleIds / AuthorizationState.Modules deleted in Phase 1** (child
   182-C), not Phase 4 (§3 Q5).
5. **Lockout guards ship with Phase 2's editing UI** (last-admin + protected-core), doubling as
   the Phase 3 resource-check exemplar (§4).
6. **Admin read/manage split in the seed vocabulary** (§3 Q2) — enforcement granularity is the
   point of the epic; pure 1:1 renames of surface policies would fossilize read=write.

**Non-blocking (record in disposition; schedule as children or fold into phases):**

7. Analyzer: `[EndpointAuthorize(Policy=…)]` must reference a registry constant (kills the
   hardcoded-literal pattern on the credential/agent contracts).
8. Agent scope → permission-bundle mapping child per §3 Q7, including making the evaluator
   scheme-aware and removing the accidental role projection onto agent principals.
9. Scheme-list duplication (task 158 debt): investigate generator-emitted `AuthSchemes` from
   policy metadata.
10. Delete orphan policies `CanViewAdminPage` (fold into `admin.access`) and `CanViewUserClaims`
    (merge with `developer.claims.read`) during 182-C.
11. Id naming convention: dotted lowercase `<area>.<concern>.<verb>`; scopes keep `:` on the
    wire; document both in the ADR.
12. `api-server` explicitly out of Phase 1 scope (no role concept exists there; agent-scope
    policies unchanged until the Q7 child).
13. The Entra branch (`AccountClaimsPrincipalFactoryWithRoles` / `AuthorizationState.Roles`)
    should migrate to the same session-permissions source when touched; not a Phase 1 gate, but
    note it in the ADR so the branch doesn't fossilize on roles.

## 6. Phase cuts (charge item 5)

Phase 1 is the right first vertical *slice of value* but the wrong *unit of work* — see
blocking amendment 1 and §3 Q6 for the three-child split and sequencing constraint. Phases 2
and 3 should partially merge (lockout exemplars land with the editing UI, amendment 5); Phase 4
shrinks (the debt retirement it lists mostly moves into 182-C; what remains is the ADR + seam
docs, and the ADR should be *drafted* alongside 182-A while decisions are hot, accepted in
Phase 4). Phase 5 stays optional children; correctly non-blocking.

## 7. Non-goals (charge item 6)

All four confirmed:

- **Mandatory OpenFGA/SpiceDB in AppHost** — right to exclude. The port + docs is the correct
  template posture; an always-on sidecar PDP would tax every consumer for a need most don't
  have (and the brief's own engine table says as much).
- **COPIC parity** — right to exclude, now with proof: the module layer was never wired to
  anything; there is no working COPIC-shaped baseline to preserve.
- **Sign-in backfill** — right to exclude; greenfield recovery via `dev db reset` and
  first-Create-account is already the established bootstrap posture (task 180), and
  per-request evaluation (§3 Q3) means there is nothing to backfill anyway.
- **RequireRole "good enough"** — right to reject as product architecture. One nuance so the
  purge doesn't overshoot: `RoleIds` and role assignment remain first-class (roles are the
  composition layer, and first-admin correctly keeps assigning the Administrator *role*);
  what dies is enforcement *naming* roles.
