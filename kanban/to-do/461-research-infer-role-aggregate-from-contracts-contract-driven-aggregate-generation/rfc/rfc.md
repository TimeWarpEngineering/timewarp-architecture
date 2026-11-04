# RFC: Generating the Role aggregate from its contracts

**Status:** RESOLVED 2026-11-05. Direction changed from entity generation to an agreement analyzer; see Final resolution at the end. Ballots and tallies above are the record of how we got there.
here is a rule until it is folded into Design regions, skills, generators, or code on this task.
Reviewers: do not treat the author lean as decided.

Host task: `kanban/*/461-research-infer-role-aggregate-from-contracts-contract-driven-aggregate-generation`.
Worked example with real code: `../contracts-aggregate-example.md` (the kitchen draft). Read it first.
Sibling RFC to come: task 464 (SPA pieces from the same contracts). Decision 1 and 2 here bind 464.

## Why this exists

The Roles slice has six endpoint contracts (`create-role`, `update-role`, `delete-role`, `get-role`,
`get-roles`, `set-role-permissions`) and, since task 466, a seventh (`set-role-agent-scopes`).
Persistence today is an in-memory `ConcurrentDictionary` (`role-store-application.cs`, called from
seven handlers) plus two join-row stores (`IRolePermissionStore` on `identity.role_permissions`,
`IRoleAgentScopeStore` on the agent-scope grant table from migration `20261102105501`). The rule
"a role owns its permissions and scopes" lives in handlers and stores, not in a domain type. The
draft proposes generating the aggregate root, its children, the store port, and the EF mapping from
the contracts, following the Profile exemplar in `tw-aggregate-pattern`.

Out of scope: the SPA side (464), the FastEndpoint generator's OpenAPI summary bug (objective bug,
below), any change to the Profile exemplar, and generating handlers.

## Evidence matrix

| Claim | Where | Verified 2026-11-04 |
|---|---|---|
| `web-domain` references `foundation-domain` and the analyzers project only. It does **not** reference `web-contracts`. | `projects/web-domain/web-domain.csproj` | yes |
| `web-contracts` does not reference `web-domain`. | `projects/web-contracts/web-contracts.csproj` | yes |
| `web-application` references both `web-contracts` and `web-domain`. | `projects/web-application/web-application.csproj` | yes |
| Roslyn source generators see only the syntax of the compilation they run in, plus referenced assemblies as metadata. Validator lambda bodies (`RuleFor(...).NotEmpty()`) are IL in a referenced assembly, not metadata. | Roslyn | by construction |
| `RoleStore` (dictionary) has 7 handler callers: create, update, delete, get, get-roles, set-permissions, set-agent-scopes. | `features/admin/roles/**/*-handler-application.cs` | yes |
| `RoleIds` is a compile-time static class in contracts, referenced 172 times. There is no roles table, no migration, and no `HasData` anywhere. | `role-ids-contracts.cs`; `platform/postgres/migrations/` | yes |
| The draft's declaration has one child set (`Permissions`). The slice now has two: permission ids and agent scopes (`SetRoleAgentScopes.Command.Scopes : List<string>`). | `set-role-agent-scopes-contracts.cs` | yes, draft is stale |
| Existing generators: `typed-id-*`, `fast-endpoint-*`, `page-*`, `state-access`, `action-roster`, `mock-response-factory-registry`, `resource-reference-registry`, `ingress-route-prefix`. Contracts have their own generator (`foundation-contracts-generators`) that emits the marker attributes and per-contract member text. | `source/analyzers/timewarp-architecture-analyzers/generators/`, `source/foundation/foundation-contracts-generators/` | yes |
| `AggregateDbContext`, `DomainInvariantsGuard`, and `AggregateVersionConvention` exist and run `Invariants` on save. | `foundation-infrastructure/persistence/aggregate-db-context.cs`, `foundation-application/services/domain-invariants-guard.cs` | yes |
| House port naming is `I<Name>Store`, never `*Repository`. | `i-profile-store-application.cs`, `i-role-permission-store-application.cs` | yes |

## Objective items (no vote)

- **OpenAPI summary bug.** `fast-endpoint-emitter.cs` emits an empty `<summary>` for
  `SetRolePermissions` because the XML summary sits on the outer static class and
  `[Operation(Description=...)]` is ignored. Fix regardless of this RFC; the author will file it as
  its own task.
- **Decided by Steve 2026-11-02:** the port is `IRoleStore`. The dictionary `RoleStore` is retired
  once the generated store replaces it. The seven callers above are the retirement list.
- **Rule already in the task spec:** every hand-written piece gets a generated partial hook or a
  stub that fails the build until written. Not up for vote; reviewers may comment on mechanism.

## Decisions

### Decision 1 (generator home): where does the generator run, and what can it see?

The draft puts the declaration in `web-contracts` and generates domain, port, and EF types. A
generator cannot emit into another project, and `web-domain` cannot see contract syntax. So the
draft as written cannot be built without choosing one of these.

| Option | Shape | Trade-offs |
|---|---|---|
| **1A. Domain references contracts (metadata)** | Add `web-contracts` as a ProjectReference of `web-domain`. The generator runs in `web-domain`, reads contract **attributes and property types** through metadata (`[ApiRoute]`, `[Operation]`, `IRoleDetails` members, `Command.PermissionIds` type). | Cheapest. Reverses nothing the skill forbids (the skill forbids contracts referencing domain). But validator rule bodies are invisible, so invariants cannot be inferred this way (see Decision 4). Domain now depends on FluentValidation-bearing contracts assembly; the dependency is one-way and already true for `web-application`. |
| **1B. Domain-side declaration, agreement analyzer** | The declaration is a hand-written `partial class Role` in `web-domain` with attributes naming fields, children, and operations by string. The generator runs in `web-domain` on that syntax alone. A separate analyzer in `web-application` (sees both assemblies) fails the build when the declaration and the contracts disagree (a field not in `IRoleDetails`, an `[Operation]` with no mutation, a child type mismatch). | Honors "when two things must agree, generate one from the other or add a build check" via the check, not generation. Two sources of truth kept in sync by a gate. More ceremony per aggregate, but the domain stays free of contract references. |
| **1C. Two-stage: contracts generator emits metadata, domain generator consumes it** | Extend `foundation-contracts-generators` to emit a `[ContractShape]` assembly-level attribute per contract (route params, operation name, request members, and a **flattened rule list** read from the validator syntax it *can* see). `web-domain` references `web-contracts` as in 1A and the domain generator reads `[ContractShape]` metadata, including rules. | Only option where invariants are truly inferred. Most build: a rule-flattening pass over FluentValidation syntax (bounded vocabulary: NotEmpty, NotNull, MaxLength, Must-with-named-set). Two generators to maintain; the metadata attribute becomes a public surface of the template. |

| **1D. Contracts source as additional files (added 2026-11-04 after round 1)** | `web-domain` lists the Roles contracts `.cs` files as `<AdditionalFiles>` (no ProjectReference). The domain generator parses that text with `CSharpSyntaxTree.ParseText` and emits the entity body, with full syntax visibility including validator rule bodies. The same generator runs in `web-application` (sees contracts and domain as metadata; emits `IRoleStore` and the agreement check) and in `web-infrastructure` (emits the EF configuration), the way the TypedId generator runs in several compilations. No new assembly edge; the runtime dependency graph is unchanged. | Keeps Steve's layering exactly (Domain references nothing but foundation). Build-time coupling only: the additional-file list is a second thing that must agree with the slice's contracts (a glob per feature folder, or an MSBuild item the file-name grammar already derives, avoids hand-listing). Parsed text has no semantic model, so the generator resolves names by convention and the application-side pass verifies them against real symbols. Incremental caching works on AdditionalTexts. |

**Author lean (round 1):** 1C if Decision 4 chooses to infer invariants; otherwise 1A. 1B is the fallback
if reviewers think a domain-to-contracts reference is a layering smell worth avoiding.

**Round 2 (2026-11-04):** Steve confirmed Domain must not reference Contracts and asked whether that
forbids contracts driving generation in other projects. It does not: a generator reads its own
syntax, referenced metadata, or additional files. 1D is the additional-files path. Reviewers
re-ballot Decision 1 only, against 1A–1D plus B's "no generator yet". Decisions 2–6 stand as tallied.

### Decision 2 (declaration shape): what does a person write?

| Option | Shape | Trade-offs |
|---|---|---|
| **2A. Marker class with several attributes** (draft) | `[AggregateRoot("Role", Schema=..., Table=...)] [AggregateFields(typeof(IRoleDetails), Validator=...)] [AggregateChildren("Permissions", Source=typeof(SetRolePermissions), ...)] [AggregateChildren("AgentScopes", Source=typeof(SetRoleAgentScopes), ...)] [AggregateOperations(Create=typeof(CreateRole), ...)] public static partial class RoleAggregate;` | Everything in one place, reads like a manifest. Attribute arguments are `typeof` so renames refactor. Verbose: five attributes, and each new child adds one. |
| **2B. One `[Aggregate]` attribute, convention for the rest** | `[Aggregate(typeof(IRoleDetails))] public static partial class RoleAggregate;` Root name from the class name minus `Aggregate`; id from the shared `{RoleId:guid}` route param; children from every `[Operation]` in the same `Group` whose command carries exactly one `List<T>` member; operations from `[Operation]` names in the group. Overrides only when convention fails. | Least to write. Convention is implicit, so a new contract in the group silently changes the aggregate; an analyzer must report what was inferred (an info diagnostic listing the inferred shape would make it visible). Harder to read later. |
| **2C. Partial domain class with member attributes** | `public sealed partial class Role : Entity<RoleId>, IAggregateRoot { [FromContract(typeof(IRoleDetails))] partial string Name; [Child(typeof(SetRolePermissions))] partial IReadOnlyList<RolePermission> Permissions; }` with mutations generated from `[Operation]`. | Declaration is the domain type itself, so there is no marker class and no name translation. Only works with 1A/1C (domain must see contract types) or 1B with strings instead of `typeof`. Members declared `partial` need C# 13 partial properties, which the stack has. |

**Author lean:** 2A, with 2B's convention as defaults so most attributes are omitted when the
slice follows the house pattern. Record every inferred value in the generated file's Design region.

### Decision 3 (mutation names): how do `[Operation]` names become methods?

| Option | Shape | Trade-offs |
|---|---|---|
| **3A. Fixed verb vocabulary** | `create` → `static Create`, `update` → `UpdateDetails`, `delete` → store `Remove`, `set-<child>` → `Set<Child>`. Any other verb is an error until mapped. | Predictable. Small vocabulary is also the house rule for operations today. Unknown verbs block, which is the intent. |
| **3B. Explicit map on the declaration** | `[AggregateOperations(Create=typeof(CreateRole), SetChildren=[typeof(SetRolePermissions), typeof(SetRoleAgentScopes)])]` with method names given per entry. | No vocabulary to learn; every mutation is spelled out. More to write, and the map can drift from the contracts without a check. |
| **3C. Method name from the command class** | `SetRolePermissions` → `Role.SetPermissions`, `UpdateRole` → `Role.Update`, by stripping the root name. | Zero config. Fragile for names that do not contain the root (`AssignPermissions`). |

**Author lean:** 3A with 3B's per-entry override for the exceptions.

### Decision 4 (invariants): where do the aggregate's `Invariants` come from?

| Option | Shape | Trade-offs |
|---|---|---|
| **4A. Infer from contract validators** | Needs 1C. `NotEmpty` on `IRoleDetails.Name` becomes `RuleFor(r => r.Name).NotEmpty()`; the `RuleForEach(...).Must(PermissionIds.All.Contains)` becomes the child invariant. | One source of truth. Only a bounded rule vocabulary survives flattening; anything else must be hand-written in the partial. |
| **4B. Hand-written `Invariants`, agreement analyzer** | The generator emits a `partial class Invariants` stub that fails the build until written. An analyzer in `web-application` reports a contract validator rule with no matching domain invariant (by member name and rule kind). | Honors the skill rule that `Invariants` is the aggregate's full rule set and may exceed the contract's. Build check instead of generation. Works with 1A or 1B. |
| **4C. Shared rule attributes on the contract interface** | `[Required] [MaxLength(128)]` on `IRoleDetails` members; both the contract validator and the domain `Invariants` are generated from the attributes. | Changes how contracts express validation today (FluentValidation classes). Larger blast radius than this task. |

**Author lean:** 4B. The skill already says contract validators are a subset of `Invariants`, and
the cross-slice rules (protected core, last admin) are hand-written anyway. 4A is attractive but
forces 1C and a rule-flattening pass for a slice with three rules.

### Decision 5 (children and tables): owned collections on the existing tables, or new tables?

Two children now: permission ids (`identity.role_permissions`, key `(RoleId, PermissionId)`,
max length 128) and agent scopes (role agent-scope grant table from migration `20261102105501`).

| Option | Shape | Trade-offs |
|---|---|---|
| **5A. Owned collections mapped onto the existing grant tables** | `OwnsMany(role => role.Permissions)` to `identity.role_permissions`, same for scopes. `RolePermissionGrant`, `RoleAgentScopeGrant`, their configurations, and both `I*Store` ports retire. The migration adds `identity.roles` and foreign keys; existing grant rows stay. | No data move. Retires two stores and two row types. The FK from grants to the new roles table requires the roles rows to exist first (Decision 6). |
| **5B. New tables, data migration copies grants** | Fresh `identity.role_permission` and `identity.role_agent_scope` owned tables; a migration copies rows and drops the old tables. | Clean names chosen by the generator's convention. A data migration in the template for a slice that few generated apps have populated. |
| **5C. Keep the grant stores as they are; aggregate holds fields only** | The aggregate owns `Name` and `Description`. Children stay in the two grant stores behind their ports. | Smallest change, but it does not model "role owns its permissions", which is the point of the task. |

**Author lean:** 5A.

### Decision 6 (seed): what happens to compile-time `RoleIds`?

`RoleIds` holds four product role Guids referenced 172 times (policies, seeds, tests). With a roles
table and FKs, those four rows must exist in every database.

| Option | Shape | Trade-offs |
|---|---|---|
| **6A. Migration seeds the four rows (`HasData` or raw insert in the migration)** | The generator emits `HasData` for every `[SeedRole]`-marked id in `RoleIds`; the next migration inserts them. `RoleIds` stays compile-time. | Standard EF. `HasData` makes every later change to a seeded row a migration, which is right for product roles. First `HasData` use in the template. |
| **6B. Startup seeder** | A hosted service inserts missing product roles on boot, as the permission seed does today (`RolePermissionSeed`). | Matches the existing permission seed. Rows appear at runtime, not at migration time, so a fresh database has FK targets only after first boot; `dev db` flows must run it. |
| **6C. Product roles stay table-less; only custom roles are rows** | The aggregate covers admin-created roles. The four product roles remain compile-time with their grants in the existing tables. | Two kinds of role with different persistence. Rejects the "roles are rows" premise. |

**Author lean:** 6A, with the seed ids declared once on `RoleIds` so the 172 references do not move.

## Task split (author plan, not balloted)

Derived from the answers above; reviewers may comment under "Anything the author missed".

1. OpenAPI summary fix in the FastEndpoint emitter (independent, file now).
2. Aggregate generator in the analyzers project, with its declaration attributes, tests against the
   Roles contracts, and the agreement analyzer if 1B or 4B wins.
3. Roles slice adoption: declaration, hand-written partials (`OnSettingPermissions`,
   `OnSettingAgentScopes`, `Invariants` if 4B), EF and in-memory `IRoleStore`, handler rewrites for
   the seven callers, retirement of the dictionary and the two grant stores.
4. Migration and seed (Decisions 5 and 6).
5. Skill update: `tw-aggregate-pattern` gains the generated path; `tw-web-api-contracts` notes the
   declaration.

## Ballot template

Append under **Reviewer opinions** (or write `rfc/<reviewer>-ballot.md` and the author merges it).

```markdown
### <agent/model name> — <date>
**Overall:** <one line>
**Decision 1 (generator home):** <1A|1B|1C|other> — <why>
**Decision 2 (declaration shape):** <2A|2B|2C|other> — <why>
**Decision 3 (mutation names):** <3A|3B|3C|other> — <why>
**Decision 4 (invariants):** <4A|4B|4C|other> — <why>
**Decision 5 (children and tables):** <5A|5B|5C|other> — <why>
**Decision 6 (seed):** <6A|6B|6C|other> — <why>
**Anything the author missed:** <…>
```

## Reviewer opinions

### Claude Opus 5.5 (reviewer A) — 2026-11-04
**Overall:** Generate the aggregate's EF mapping, port, and agreement checks, and keep the domain free of a contracts reference. The draft also has three blocking gaps that no ballot option fixes: the protected-core hook cannot live in the domain, `partial void` does not fail the build, and retiring the two grant ports breaks the platform permission evaluator.
**Decision 1 (generator home):** other (1B, built the way the TypedId generator already works) — Do not add the domain-to-contracts edge. `features/profile/profile-domain.cs` (Design region) states "Domain cannot reference the contracts assembly" and duplicates contract literals for that reason. So the matrix line that 1A "reverses nothing the skill forbids" holds for SKILL.md but contradicts the exemplar. The edge would also pull ASP.NET Authorization, protobuf-net.Grpc, OneOf, Mediator and TimeWarp.Identity into `web-domain` (see `projects/web-contracts/web-contracts.csproj`). Instead, run one generator in several compilations, as `generators/typed-id-source-generator.cs` does. That generator emits the type surface where the type is declared and the EF converters where EF is referenced, and it reads referenced-assembly attribute metadata through an assembly marker. The domain pass emits the entity body. The `web-application` pass emits `IRoleStore` and runs the agreement check, because it sees contract and domain *metadata* (attributes, member names, member types). The `web-infrastructure` pass emits the EF configuration.
**Decision 2 (declaration shape):** 2C (with an operation-group string, not `typeof`) — The declaration should be the domain type itself: `[Aggregate(OperationGroup = "admin.roles")] public sealed partial class Role : Entity<RoleId>, IAggregateRoot` with partial properties `Name`, `Description`, `Permissions`, `AgentScopes`. There is no marker class and no name translation. Under 1B the domain cannot write `typeof(IRoleDetails)`, but the group string already exists on every Roles `[Operation]`. The application-side check then reports drift, such as an `IRoleDetails` member with no matching property or a `set-*` operation with no matching child. 2A's five-attribute manifest lives in contracts, which cannot name domain types, so it can never be checked against the type it describes. 2B's silent convention is the wrong default for a template that teaches the pattern.
**Decision 3 (mutation names):** 3A (plus 3B override per member) — A fixed vocabulary matches how the slice already names operations. As written, though, 3A covers only five of the seven verbs. `roles.get` and `roles.list` (`get-roles-contracts.cs`) must map to port reads (`FindAsync` and `ListAsync`), not domain methods. `set-<child>` should resolve by kebab-to-Pascal against the declared child property (`set-agent-scopes` to `AgentScopes`, then `SetAgentScopes`), and a missing property becomes a build error under 2C. For an exception, the override goes on the property, for example `[Child(Operation = "roles.assign-permissions")]`, not in a separate map that can drift.
**Decision 4 (invariants):** 4B (member-level check only in this task) — Hand-written `Invariants` is correct, because the skill says `Invariants` is "the aggregate's full rule set" and the cross-slice rules are hand-written anyway. The 4B check as described cannot be built where the RFC puts it. In `web-application`, both the contract validators and the domain `Invariants` come from referenced assemblies, so their rule bodies are IL. This is the same visibility limit the matrix cites against 1A. Ship the member-level check now (names and types, visible as metadata). Rule-kind matching would need the contracts generator to emit a rule summary as metadata, which is 1C's `[ContractShape]` feeding a check instead of a generator. Defer that until a slice has more than three rules.
**Decision 5 (children and tables):** other (5A mapping, keep the read-side grant ports) — Mapping owned collections onto `identity.role_permissions` and `identity.role_agent_scopes` with no data move is right. But "both `I*Store` ports retire" is wrong. `IRolePermissionStore` is read by `platform/authorization/permission-evaluator-application.cs`, `admin-lockout-guards-application.cs`, `agent-scope-permission-seed-application.cs`, and the principals slice (`set-principal-roles-handler-application.cs`). `IRoleAgentScopeStore` is read by `platform/authorization/agent-scope-approval-application.cs`. Retire only the `Set*` write methods, `RolePermissionGrant`/`RoleAgentScopeGrant`, and their configurations. Keep the read ports defined in platform and implemented inside the Roles slice over the aggregate's tables, so platform never references the slice (TWA0009). EF will not map a grant row type and an owned collection to the same table, so the old read implementations must move onto the owned mapping.
**Decision 6 (seed):** 6A (raw `InsertData` in the migration, not `HasData`) — The four rows must exist before the new foreign keys, in the same migration, because every database already holds `role_permissions` rows for those ids. The RFC misdescribes 6B. No startup seeder exists. `RolePermissionSeed` is a compile-time map that `in-memory-role-permission-store-application.cs` copies, and Postgres gets its rows through migration `InsertData` (`20260812102254_AddRolePermissions.cs` and five later migrations). So `InsertData` is the existing house precedent. `HasData` would be its first use, and it would turn every later edit to a seeded row into a migration fought out in the model snapshot. Keep `RoleIds` compile-time and generate the insert from `RoleIds.All`, so the 4-id list has one source.
**Anything the author missed:**
- **Protected core cannot be a domain hook.** `AdminLockoutGuards` and `RolePermissionSeed.AdminPermissions` are `*-application.cs` files in `web-application`, and `web-domain` cannot reference that project under any Decision 1 option. The guard also returns a 409 `SharedProblemDetails`, while a domain hook can only throw. Keep protected core in the `SetRolePermissions` handler, or move `AdminPermissions` into the domain and add a typed domain exception that the handler maps to 409.
- **`partial void` breaks the objective "fails the build" rule.** An unimplemented `partial void OnSettingPermissions(...)` compiles silently and is removed. Required hooks must be extended partial methods with an access modifier, such as `private partial void OnSettingPermissions(...)`, which raise CS8795 until implemented. A generated empty `Invariants` stub would also satisfy TWA0011 and pass silently. Generate `private partial void ConfigureInvariants(Invariants rules)` instead.
- **Port shape.** The draft's `IRoleStore` (`GetAsync`, sync `Add`/`Remove`, `SaveChangesAsync`) is a unit-of-work shape. The house exemplar `features/profile/i-profile-store-application.cs` is `FindAsync`/`AddAsync`/`UpdateAsync` with in-memory and EF snapshot and compare-and-swap semantics (skill section 6). Generate the house shape.
- **Delete semantics.** `delete-role-handler-application.cs` has no guard, so a product role can be deleted today, and grants are orphaned. With seeded rows and foreign keys, deleting Administrator would cascade its grants and never be re-seeded. `identity.principal_roles` would also keep dangling role ids. Decide on a "product roles are not deletable" invariant and on principal_roles foreign-key behavior before the migration task.
- **Matrix nits.** The seven `RoleStore` callers are confirmed. The 3 hits in `admin/principals/effective-roles-resolver-application.cs` are a field named `RoleStore` of type `IPrincipalRoleStore`, not the dictionary. The `RoleIds.` reference count is 177 in `source/` by a plain grep, so the 172 figure is close but not exact.

### Claude Sonnet 5.5 (reviewer B, adversarial) — 2026-11-04
**Overall:** Wrong direction as framed: it makes the domain a function of transport shape, and the evidence matrix hides that the real prize (role owns its permissions and scopes) needs no generator. Hand-write `Role` like `Profile`, add a build check, and generate only when a second aggregate proves the pattern.
**Decision 1 (generator home):** other (1B-lite, and not yet) — 1A/1C invert the dependency. Today the domain is the stable core and contracts are the edge (`web-contracts.csproj` and `web-domain.csproj` do not reference each other; `web-application` joins them). 1A makes `web-domain` depend on an assembly carrying FluentValidation, OneOf, TimeWarp.Mediator, protobuf-net.Grpc, and `Microsoft.AspNetCore.Authorization` (all in `web-contracts.csproj`). A rename of a contract property then reshapes an aggregate and its EF mapping and its migration. Also `timewarp-identity` already follows "foundation-domain only, independent of the rest" (its csproj comment); 1A sets the opposite precedent for every generated app. 1C is worse: it publishes `[ContractShape]` as template public surface to enable a rule-flattener for three rules. The matrix claim "Reverses nothing the skill forbids" is true only because the skill never imagined this; the skill's own layering (domain below contracts) is the thing being reversed.
**Decision 2 (declaration shape):** 2C if anything, but only under 1B — the declaration should be the domain type itself (`partial class Role` with string-named attributes), so the domain is authored in the domain and contracts are checked against it, not the reverse. 2A introduces a `RoleAggregate` marker class that is a third name for one concept (Role, IRoleDetails, RoleAggregate). 2B's "any new `[Operation]` in the Group silently changes the aggregate" is a schema migration triggered by adding an endpoint; the info-diagnostic mitigation does not stop it.
**Decision 3 (mutation names):** 3B — explicit. 3A's fixed vocabulary quietly encodes that every contract verb is a domain verb. `update` -> `UpdateDetails` and `delete` -> store `Remove` are already not the same thing. With one aggregate in play, a vocabulary is a guess about future aggregates.
**Decision 4 (invariants):** 4B — and it should be stated as the reason against Decision 1's lean. The skill says `Invariants` is the aggregate's full rule set and contract validators are a subset. Inferring domain rules from the transport subset is exactly the wrong arrow. The Roles case proves it: the protected-core and last-admin rules live in handlers/guards (`admin-lockout-guards-application.cs`), not in any validator.
**Decision 5 (children and tables):** 5A, but split from generation. Mapping `OwnsMany` onto the existing grant tables and retiring the two `I*Store` ports is the part that delivers "role owns its permissions". Caveat the RFC skips: `RolePermissionGrant` and `RoleAgentScopeGrant` have their own entity configs and in-memory twins (`in-memory-role-*-store-application.cs`), and the permission evaluator reads grants through the ports. Owned collections remove the read path unless the evaluator gets a query port; check `permission-evaluator-application.cs` before promising retirement. Aggregate loading the whole role to answer "does this principal have permission X" is a cost on the hot authorization path.
**Decision 6 (seed):** other (6A, but the claim is overstated) — 6A does not leave "172 references untouched" for free. `RoleIds` lives in `*-contracts.cs`, so `web-domain` cannot see it without 1A, and the `[SeedRole]` marker puts a persistence concern on a contracts class that the SPA (WASM, via `web-spa.csproj`) also compiles. The seed rows then exist in two places (RoleIds Guids plus a `HasData` migration); that is "two things that must agree" with no check named. Also `RoleIds.GetRoleNameByGuid` reflects over public Guid fields, so adding any attribute-bearing or non-Guid member changes its behavior. Add a test that migration seed ids equal the `RoleIds` set, or declare the ids once in the domain and have `RoleIds` forward to them.
**Anything the author missed:**
- **Third option: no generator now.** Hand-write `Role : Entity<RoleId>, IAggregateRoot` in `features/admin/roles/role-domain.cs` (the file grammar already routes `*-domain.cs` to `web-domain`; see `msbuild/feature-filename-grammar.g.props` line 39), with `Permissions` and `AgentScopes` as owned children, `Invariants`, and `IRoleStore`. That is Profile verbatim. Then add one analyzer in `web-application` (sees both assemblies) that fails when `IRoleDetails` members have no matching `Role` member. This satisfies AGENTS.md "generate or add a build check" with the check, costs one analyzer, and ships the entire user-visible outcome. It is the smaller first step the task asks for.
- **Rule of three.** There is one slice (Roles) and one exemplar (Profile). A generator designed from one worked example encodes that example's accidents (one `Guid` route id, `List<string>` children, the `Group` string). `Principal` and `Credential` in `timewarp-identity` are existing aggregates with a different shape; the RFC never tests the declaration against them.
- **Per-slice cost.** Every future slice pays: learn five attributes plus inferred-value conventions, debug through `.g.cs` files, and carry a new analyzer rule set. Generated domain code also hides the `Create`/mutation guard clauses that the skill calls out as the complement to save-time invariants; the RFC does not say where those guards come from (they are not in the contracts).
- **Evidence matrix gaps.** The row "Roslyn generators see only syntax of own compilation plus metadata" is correct but is used to dismiss validators in 1A while 1C then claims to read "validator syntax it can see" in `web-contracts`; the contracts generator in `foundation-contracts-generators` collects per-contract text today (`contracts-collector.cs`), but the matrix does not say it reads `Validator` bodies. Verify before relying on 1C. The "draft is stale" row also shows the declaration drifts one task after it is written; 2A would have needed an edit for task 466 at once.
- **Task split risk.** Item 2 (generator) precedes item 3 (adoption) so the generator ships without a consumer test beyond Roles. Reverse it: land item 3 by hand first, then extract.

### Grok (reviewer C) — 2026-11-04
**Overall:** One contracts-side manifest should drive three layer files, with hand-written invariants, the existing grant tables, and an InsertData seed; the authorization ports stay.
**Decision 1 (generator home):** other — Keep the marker in `web-contracts` and emit `*-domain.cs`, `*-application.cs`, and `*-infrastructure.cs` from it without a `web-domain` → `web-contracts` `ProjectReference`. `web-spa` already references `web-contracts` and not `web-domain`, and this decision binds task 464, while `feature-filename-grammar.g.props` sends those three suffixes to three projects, so a generator that runs only inside `web-domain` cannot emit the port or the EF configuration. `web-domain` references `foundation-domain`, the analyzers project, and the FluentValidation package, and `profile-domain.cs` states that domain cannot reference the contracts assembly; a direct reference would also pull Mediator, protobuf-net.Grpc, Authorization, and TimeWarp.Identity into every aggregate. `foundation-contracts-generators` already runs in the contracts compilation and can write a shape file those three generators consume as additional files; `web-application` does not reference the architecture generators today, while `web-domain` and `web-infrastructure` do.
**Decision 2 (declaration shape):** other — Require the full 2A attribute manifest on that contracts marker, including both children, with no omit-by-convention defaults. `SetRolePermissions.Command.PermissionIds` and `SetRoleAgentScopes.Command.Scopes` are the two `List<string>` members, and the names a reader expects on the aggregate are `Permissions` and `AgentScopes`. `GetRoles.RoleDto` already carries both lists, so a rule that infers a child from "the command with exactly one `List<T>`" will mis-name these two or, if it also walks the read model, invent a table from a DTO. A generated Design region that records an inference does not fail the build; a missing attribute should.
**Decision 3 (mutation names):** other — Close the verb table over the seven current operations and fail the build on any other verb. `roles.create` is `Role.Create` plus `IRoleStore.AddAsync`, `roles.update` is `Role.UpdateDetails` because the command replaces `IRoleDetails` as a pair, `roles.delete` is a store remove, `roles.set-permissions` is `SetPermissions`, and `roles.set-agent-scopes` is `SetAgentScopes`. `roles.get` is `FindAsync`, matching `IProfileStore`, and `roles.list` stays a handler because `GetRoles` is a paged `Search` plus OpenData parameters. Stripping the root from the class name turns `GetRoles` into `Gets` and never sees `roles.list`.
**Decision 4 (invariants):** 4B — Generate a private `Invariants` stub that fails the build until it is written, and check only the rules that are actually shared. `RoleDetailsValidator` is `NotEmpty` on `Name` and `Description`, and `UpdateRole.Validator` reaches it through `SetValidator`, while the catalog rules are `RuleForEach` `Must` against `PermissionIds.All` and `AgentScopes.All`. Protected-core is `AdminLockoutGuards` in the set-permissions handler, which no contract validator contains, and the aggregate skill already treats `Invariants` as a superset of the contract. The agreement analyzer should follow `SetValidator` for `NotEmpty` and should treat each `Must` as a required hand-written hook, because a metadata-level kind match cannot see a lambda body.
**Decision 5 (children and tables):** other — Map owned collections onto the existing tables and keep the two authorization ports as facades over the aggregate. `identity.role_permissions` is `(RoleId, PermissionId)` with `PermissionId` max length 128, and `identity.role_agent_scopes` is `(RoleId, Scope)` with `Scope` max length 64; `RolePermissionGrant` and `RoleAgentScopeGrant` are the mappings that retire so two entity types do not claim those tables. `IRolePermissionStore` and `IRoleAgentScopeStore` live in `TimeWarp.Architecture.Authorization` and are what the permission evaluator, agent-scope approval, and `Features.Admin.Principals` call; moving that read onto `Features.Admin.Roles.Domain` would make another product slice depend on Admin.Roles, which TWA0009 forbids. The in-memory `RoleStore` still retires: the seven callers are the roles handlers (create, update, delete, get, get-roles, set-permissions, set-agent-scopes).
**Decision 6 (seed):** other — Insert parent rows in the same migration that adds the foreign keys, using `migrationBuilder.InsertData` and Guid literals, the way `20260812102254_AddRolePermissions` already seeds grants without referencing `RoleIds`. There is no `HasData` call in the repo, and names and descriptions are mutable through `roles.update`, so model `HasData` would let a later migration rewrite an admin edit. A startup seeder cannot do this: tests and `dev db` apply schema with `Migrate`, and the foreign key has to be valid when that migration finishes. `RolePermissionSeed` is a static map the in-memory store copies in its constructor, not a hosted service; also insert a parent for every distinct `RoleId` already stored in the grant tables, because `SetRolePermissions` can persist a custom id that exists only in the dictionary.
**Anything the author missed:** The draft's `IRoleStore.SaveChangesAsync` is a second save path. `EfProfileStore` calls `SaveChangesAsync` inside `AddAsync` and `UpdateAsync`, and `AggregateDbContext` runs the guard there. Owned collections have to keep the current composite keys and column names, with no surrogate id. Scope length is 64, which the draft's single max length of 128 does not cover. Last-admin lives on `SetPrincipalRoles`, and the set-agent-scopes handler has no protected-core, so the only generated partial hook for that rule is the permissions mutation. The matrix line that `web-domain` references foundation-domain and the analyzers project only skips the FluentValidation package reference that is already there. I did not re-count the "172 references" figure; migrations already copy the four Guid literals so they stay free of the `RoleIds` type.

## Tally

Verified by the author 2026-11-04 against the worktree before counting: the Profile Design region
says "Domain cannot reference the contracts assembly"; `IRolePermissionStore` is read by the
permission evaluator, the lockout guards, the agent-scope seed, and the principals slice;
`IRoleAgentScopeStore` is read by agent-scope approval; eight migrations use `InsertData` and none
use `HasData`; `delete-role` has no product-role guard. All reviewer claims above hold.

| # | Topic | A (Opus) | B (Sonnet, adversarial) | C (Grok) | Status |
|---|-------|----------|-------------------------|----------|--------|
| 1 | generator home | other: 1B via multi-compilation generator (TypedId style) | other: no generator yet; hand-write Role, add agreement analyzer | other: contracts marker emits a shape file that layer generators read as AdditionalFiles | 3–0 against a domain→contracts reference (1A, 1C as written); 3-way dissent on mechanism and on generating now |
| 2 | declaration shape | 2C with operation-group string | 2C, only under 1B | 2A full manifest, no convention defaults | 2–1 for 2C; C's point that convention mis-names the two children stands |
| 3 | mutation names | 3A + per-member override | 3B explicit | closed verb table over the seven operations, build error otherwise | dissent; moot until a generator ships |
| 4 | invariants | 4B, member-level check only now | 4B | 4B; Must rules become required hooks | 3–0 for 4B |
| 5 | children and tables | 5A mapping, keep read-side grant ports | 5A, keep evaluator path | 5A, keep both authorization ports as facades over the aggregate (TWA0009) | 3–0 for 5A with the authorization ports kept |
| 6 | seed | 6A via InsertData from RoleIds.All | 6A, ids must agree by test or single declaration | 6A via InsertData with Guid literals, plus a parent row for any stray RoleId in the grant tables | 3–0 for 6A-InsertData; agreement check on the id list still open |

Also verified for C: `web-application` has no reference to the architecture generators; `GetRoles.RoleDto` carries both lists (`PermissionIds`, `AgentScopes`); the agent-scope table key is `(RoleId, Scope)` with length 64.

Blocking gaps both reviewers raised, outside the six decisions: the protected-core hook cannot live
in the domain (it is an application concern and returns a 409); `partial void` hooks compile silently
so required hooks must be extended partials or a required `ConfigureInvariants`; the port must follow
the Profile `FindAsync/AddAsync/UpdateAsync` shape; `delete-role` needs a product-role invariant before
foreign keys arrive; and the task split should land Roles by hand first, then extract the generator.

Additional gaps from C: the draft's `SaveChangesAsync` on the port is a second save path (the house
store saves inside `AddAsync`/`UpdateAsync`); scope length is 64 not 128; `set-agent-scopes` has no
protected-core rule, so the only hook is on the permissions mutation; last-admin lives on
`SetPrincipalRoles`.

## Reviewer opinions, round 2 (Decision 1 only)

### Claude Opus 5.5 (reviewer A, round 2) — 2026-11-04
**Decision 1 (generator home):** 1D, with the hand-written Roles aggregate built first as the generator's golden — 1D keeps Steve's layering with no new assembly edge and gives the domain pass the validator syntax that 1B lacked, so it replaces my round-1 "other". The "second list" cost is close to zero if the item is derived, not authored. `source/container-apps/web/msbuild/feature-filename-grammar.g.props` already generates each layer's `Compile` glob (`$(WebFeatureTreeRoot)/**/*-contracts.cs` for `web-contracts`). One more generated line there gives `web-domain` the same set as `AdditionalFiles`, whole tree rather than per slice, so the list cannot drift. The repo also already reads additional files in generators and analyzers. `generators/fast-endpoint-collector.cs` uses `AdditionalTextsProvider` for XML docs, and `template-flag-constants-analyzer.cs` reads `template.json`. The real costs are elsewhere. First, the parsed text lacks members that `foundation-contracts-generators/contracts-generator.cs` emits, for example `RoleId` on `SetRolePermissions.Command`, which comes from `[ApiRoute]`, so the domain pass must re-derive them from the route template. Second, with no semantic model, names resolve by convention and only the `web-application` pass checks them against symbols. Third, every contracts edit reruns the domain pass, so filter texts cheaply, for example by the `Group = "…"` string, before calling `ParseText`. Generate now, but write `Role` by hand first, in the same task family. It becomes the expected output that the generator must reproduce exactly, under the same regenerate-and-diff gate as `goldens/ingress/WebServerApiRoutePrefixes.g.cs`. That gives the generator a concrete target instead of a spec to argue over.
**Does 1D change your Decision 2 or 4 vote:** Decision 2 no, Decision 4 yes in where the check runs, not the choice. For Decision 2, the domain type is still the declaration (2C), and the group string is still the key, because `web-domain` still cannot write `typeof(IRoleDetails)`. 1D only lets the domain build report a bad group string earlier. For Decision 4, my round-1 objection was that a rule-level check in `web-application` sees only IL. Under 1D, the `web-domain` pass sees its own `Invariants` syntax and the contracts' validator syntax, so rule-kind agreement such as `NotEmpty` and `MaxLength` per member can ship now in the domain pass. It does not need a deferred `[ContractShape]` attribute. I still vote 4B (hand-written `Invariants`) over 4A. Syntax visibility is not reachability. The set rules call `PermissionIds.All` (in `features/authorization/permission-ids-contracts.cs`, compiled into `web-contracts`) and `AgentScopes.All` (in `libraries/timewarp-identity/tokens/agent-scopes.cs`). `web-domain.csproj` references neither, so a generated domain invariant cannot call either one without copying the set.

### Claude Sonnet 5.5 (reviewer B, adversarial, round 2) — 2026-11-04

**Decision 1 (generator home):** other — a narrowed 1D, only after the hand-written `Role` lands (my round-1 "no generator yet" stands, but 1D is now the form the later generator should take, not 1A or 1C).
1D fixes the assembly edge, and I withdraw that objection. It does not fix the authoring direction. If `web-domain` lists `features/admin/roles/**/*-contracts.cs` as AdditionalFiles, then renaming `IRoleDetails.Name` or adding a `List<T>` command in the `admin.roles` group still reshapes the entity, the EF configuration, and the next migration. The build-time edge is a real dependency even with no ProjectReference, and it is invisible in the csproj graph. Verified: there is no `AdditionalFiles` use anywhere under `source/` (csproj, props, targets), so 1D sets a new mechanism rather than reusing one, and the RFC's "the way the TypedId generator runs in several compilations" is unproven by anything I could find.
Parsing without a semantic model is fragile in this slice specifically. The set-role-permissions validator reads `global::TimeWarp.Architecture.Features.PermissionIds.All.Contains(id)` (the contract's own comment says the property `PermissionIds` shadows the class), and the invariant "id is in the catalog" cannot be flattened to a rule kind without resolving a static `HashSet` in a file that is not in the additional-file set. The `Must(static id => ...)` lambda is an arbitrary expression, so 4A-style inference degrades to "copy the lambda text", which is not name-safe in the domain's namespaces and usings (web-domain does not compile `PermissionIds`). Types such as `OneOf<Response, SharedProblemDetails>`, `IAuthApiRequest`, and `HttpVerb` are unresolved text in the domain compilation, so the generator must ignore them by convention; the application-side verification pass then becomes the real source of truth and the domain pass is a guess it validates.
The coupling surface is also wider than "the list": the contracts live under `features/` and are already globbed by file-name grammar (`msbuild/feature-filename-grammar.g.props`, `*-contracts.cs` into `web-contracts`, `*-domain.cs` into `web-domain`). Adding `*-contracts.cs` as additional files to `web-domain` makes the grammar file the single place that breaks slice isolation (TWA0009's premise) at build level, with no analyzer watching it.
Rule of three: it does not hold yet. One slice and one exemplar (`Profile`); I found no other aggregate that is contract-shaped (`Principal`/`Credential` in `source/libraries/timewarp-identity` are not in contracts files at all). The smallest 1D-shaped step worth doing now: (1) hand-write `role-domain.cs` and `IRoleStore`, and (2) ship only the agreement check in `web-application` (metadata on both sides, no text parsing, no additional files) that fails when `IRoleDetails` members, `[Operation]` children, or `Validator` rule targets have no counterpart on `Role`. That delivers "role owns its permissions" and the "generate or add a build check" rule. Revisit generation (1D, parsing only attribute-free shapes: fields and `List<T>` children, never validator bodies) when a second contract-shaped aggregate exists and the diff between the two hand-written ones shows what is mechanical.

**Does 1D change your Decision 2 or 4 vote:** No on 4, a partial yes on 2. Decision 4 stays 4B because 1D's unlocked "invariants from validator bodies" is the weakest part of 1D (unresolvable catalog names and lambdas above), and the skill (`skills/tw-aggregate-pattern/SKILL.md`) still says `Invariants` is the full rule set and contract validators are a subset, so inferring the superset from the subset is the wrong arrow however the text arrives. On Decision 2, 2C is now workable without `typeof` (strings, or the domain class as the declaration, with 1D reading contracts as text), which removes my round-1 objection that 2C needs a domain-to-contracts reference. My vote moves from "2C under 1B only" to "2C, string-named, with the agreement check", for the same reason as before: the declaration should live in the domain and the contracts should be checked against it.

### Grok (reviewer C, round 2) — 2026-11-04

**Decision 1 (generator home):** 1D — 1D is better than the round-1 shape file as a build input and worse as a compiler input, and the build constraint decides it. `ContractsGenerator` only calls `AddSource` inside the contracts compilation (`source/foundation/foundation-contracts-generators/contracts-generator.cs`), so a shape file has no legal producer for `web-domain` to consume, while the hand-written contract sources are already on disk and `FastEndpointCollector.ContractDocumentationProvider` already reads `AdditionalTextsProvider` (`source/analyzers/timewarp-architecture-analyzers/generators/fast-endpoint-collector.cs`, included from `msbuild/api-endpoint-contract-documentation.targets`). `source/container-apps/web/projects/web-domain/web-domain.csproj` has no `ProjectReference` to `web-contracts`, which is the layering `source/container-apps/web/features/profile/profile-domain.cs` states and Steve confirmed, so 1A and 1C stay out: `web-contracts.csproj` references Mediator, protobuf-net.Grpc, `Microsoft.AspNetCore.Authorization`, OneOf, and TimeWarp.Identity. The additional-file list has to be the same whole-tree `**/*-contracts.cs` glob `source/container-apps/web/msbuild/feature-filename-grammar.g.props` already compiles into `web-contracts` (that file gives `web-domain` only `*-domain.cs`), plus `source/container-apps/web/projects/web-contracts/global-usings.cs`, which the glob skips; a per-slice hand list is a second inventory and would have missed `set-role-agent-scopes-contracts.cs`. `CSharpSyntaxTree.ParseText` supplies no semantic model and skips the generated partial: `set-role-permissions-contracts.cs` declares `UserId` and `PermissionIds`, `contracts-emitter.cs` emits `RoleId` from `{RoleId:guid}` into the contracts `obj` output, and that file's `Must` body spells `global::TimeWarp.Architecture.Features.PermissionIds.All` because the property shadows the catalog. The domain pass should read the 2A attribute syntax and the route string; `web-application.csproj` does not reference `timewarp-architecture-analyzers` today while `web-domain.csproj` and `web-infrastructure.csproj` do, and once it does, that pass still sees contracts and domain as metadata only, the limit `typed-id-source-generator.cs` already documents for its EF pass.

**Does 1D change your Decision 2 or 4 vote:** No — Decision 2 stays the full 2A manifest on the contracts marker, because 1D's "resolve names by convention" is the inference that mis-names `SetRolePermissions.Command.PermissionIds` and `SetRoleAgentScopes.Command.Scopes` and that also sees both lists on `GetRoles.RoleDto`. Decision 4 stays 4B: parsed text can follow `SetValidator(new RoleDetailsValidator())` to the two `NotEmpty` calls in `role-details-contracts.cs`, and each `Must` stays a hand-written hook because the lambda is not a semantic model of the catalog. The agreement check in `web-application` still needs these additional files, because referenced validator bodies arrive there as metadata.

## Tally, round 2 (Decision 1)

Verified by the author 2026-11-04: `fast-endpoint-collector.cs` and `template-flag-constants-analyzer.cs`
already read additional texts (B's "no AdditionalFiles use under source/" is wrong); the TypedId
generator's Design region documents a per-compilation declaration pass plus an EF pass over marked
referenced assemblies (B's "nothing backs multi-compilation" is wrong); the grammar props globs
`**/*-contracts.cs` into web-contracts and `**/*-domain.cs` into web-domain, so a derived
AdditionalFiles line is one generated line (A and C agree); the generated `RoleId` member is absent
from the parsed text (A and C agree). All three agree 1A and 1C are out.

| # | Topic | A (Opus) | B (Sonnet, adversarial) | C (Grok) | Status |
|---|-------|----------|-------------------------|----------|--------|
| 1 | generator home | 1D; hand-write Role first as the generator's golden under a regenerate-and-diff gate | 1D is the right form if a generator comes; still hand-write first, ship only the application-side agreement check now, revisit generation at the second aggregate | 1D; whole-tree glob as AdditionalFiles, generator reads the manifest attributes and route string, never lambda bodies | 3–0 for 1D as the mechanism. 3–0 that Role is hand-written first. Dissent only on whether the generator is built in this task family (A, C) or deferred to the second aggregate (B). |

Round 2 side effects on earlier decisions: Decision 2 is now 2C by A and B (B drops its "only under 1B"
condition), 2A full manifest by C. Decision 4 stays 4B 3–0; under 1D the rule-kind agreement check can run in
the domain pass now (A), and every `Must` rule is a required hand-written hook because `PermissionIds.All` and
`AgentScopes.All` are unreachable from web-domain (A, C).

**Resolved by Steve, 2026-11-05: Decision 1 = 1D, generator now, proven against three goldens.**
Reasoning: B's rule-of-three objection is met inside the task family instead of by deferral. The
goldens, in order, each add a shape the previous one lacks:

1. **Roles** (hand-written first, this task family): fields plus two string-set children, catalog
   `Must` rules as hand-written hooks, protected-core in the application layer.
2. **FeedbackItem** (existing aggregate, `features/feedback/feedback-item-domain.cs`): entity
   children with sequence invariants, a status transition table already in a contracts file,
   attachments. Its contracts gain `[Operation]` and a details interface first.
3. **Order** under backlog task 118 (real-domain showcase, marketplace layer): order lines with
   money arithmetic across children, guarded state transitions with side effects, cross-aggregate
   references by id (buyer principal, catalog item, credit-ledger entry from timewarp-402), agent
   and human principals. Steve: "we need the 118 to validate that we can actually do this
   correctly." Order lives under the real-domain flag from day one, so 118's flag gating becomes
   its first child and Order its second.

Each golden is hand-written, then the generator must reproduce it under a regenerate-and-diff
gate (the `goldens/ingress` precedent). The generator is declared stable after Order; tasks 329
and 464 build on its manifest after that.

## Final resolution (Steve, 2026-11-05)

Generating the entity from the contracts inverts how the back end is modeled. The domain is built
domain-first (UML, aggregates, Profile style); contracts are built UI-first, as the front end's
request. The back end's obligation is to **satisfy** the contract, not to be shaped by it. The
details interface (`IRoleDetails`) is the DTO's definition, not the entity's. What sits between
them is the mapping in the handler, which today is hand-written and was absent from every draft.

Decisions as resolved:

- **Decision 1 and 2 are superseded.** No aggregate generator, no declaration on the entity, no
  additional-files machinery. The domain is hand-written. Sonnet's round-1 position stands.
- **Build check, not generation (AGENTS.md rule).** A convention analyzer in `web-application`,
  the one project that references both `web-contracts` and `web-domain`, verifies that the
  aggregate has what the contracts want: for each operation group, the details members exist on
  the aggregate with compatible types, each set-style command has a mutation, each query response
  is projectable from aggregate members. Build error on drift. Links by operation group and
  member name; metadata is enough, no validator bodies.
- **Step two, evaluated after the analyzer lands on Roles and FeedbackItem:** generate the glue in
  `web-application`: the entity-to-response projection and the command-to-mutation call, with
  required hooks for application rules (protected core). This is the AutoMapper niche done at
  compile time, and it is only worth it if the analyzer shows that contracts and entities stay
  name-aligned; when they drift, the mapping is hand-written and the analyzer says so.
- **Decision 3 is moot.** Decisions 4, 5, 6 stand: hand-written `Invariants`; owned collections
  on the existing grant tables with the authorization read ports kept; `InsertData` seed in the
  migration that adds the foreign keys, plus a product-role delete guard.
- **Goldens become analyzer targets:** Roles (hand-written in this family), FeedbackItem
  (contracts gain `[Operation]` and a details interface), Order under task 118 later.
- **464 is unchanged:** the SPA is generated from the contracts; that direction is correct because
  the contract is the front end's request.
