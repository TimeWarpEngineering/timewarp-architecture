# Decision 2: the two declaration shapes, as code

Same slice (Roles), same generator mechanism (1D: domain reads the contracts source as additional
files; application and infrastructure passes read metadata). Only the declaration differs.
Real names throughout: `IRoleDetails`, `RoleDetailsValidator`, `SetRolePermissions`,
`SetRoleAgentScopes`, `CreateRole`, `UpdateRole`, `DeleteRole`, `GetRole`, `GetRoles`.

Contract-side facts both options read (unchanged, real):

```csharp
// role-details-contracts.cs
public interface IRoleDetails { string Name { get; set; } string Description { get; set; } }

// set-role-permissions-contracts.cs
[Operation("roles.set-permissions", Group = "admin.roles")]
public sealed partial class Command : IIdempotentCommand<...> { public List<string> PermissionIds { get; set; } = []; }

// set-role-agent-scopes-contracts.cs
[Operation("roles.set-agent-scopes", Group = "admin.roles")]
public sealed partial class Command : IIdempotentCommand<...> { public List<string> Scopes { get; set; } = []; }

// create-role, update-role, delete-role, get-role, get-roles: [Operation("roles.create" | "roles.update" | "roles.delete" | "roles.get" | "roles.list", Group = "admin.roles")]
```

---

## Option 2C: the domain type is the declaration

### What a person writes

`features/admin/roles/role-domain.cs` (compiles into `web-domain`)

```csharp
namespace TimeWarp.Architecture.Features.Admin.Roles.Domain;

[Aggregate(OperationGroup = "admin.roles", Schema = "identity", Table = "roles")]
public sealed partial class Role : Entity<RoleId>, IAggregateRoot
{
  [FromContract("IRoleDetails")]            public partial string Name { get; }
  [FromContract("IRoleDetails")]            public partial string Description { get; }

  [Child(Operation = "roles.set-permissions", Member = "PermissionIds", Table = "role_permissions", MaxLength = 128)]
  public partial IReadOnlyList<RolePermission> Permissions { get; }

  [Child(Operation = "roles.set-agent-scopes", Member = "Scopes", Table = "role_agent_scopes", MaxLength = 64)]
  public partial IReadOnlyList<RoleAgentScope> AgentScopes { get; }

  // Required hooks: generated as `private partial`, so the build fails (CS8795) until these exist.
  private partial void ConfigureInvariants(Invariants rules)
  {
    rules.RuleForEach(r => r.Permissions).Must(p => PermissionCatalog.Contains(p.Value));  // catalog copied in: PermissionIds.All is unreachable from web-domain
    rules.RuleForEach(r => r.AgentScopes).Must(s => ScopeCatalog.Contains(s.Value));
  }
}
```

`features/admin/roles/role-id-domain.cs`: `[TypedId] public readonly partial record struct RoleId;` (exists in pattern today).

Strings, not `typeof`, because `web-domain` cannot name contract types. The domain pass resolves
`"IRoleDetails"` and `"roles.set-permissions"` in the parsed contracts text; the application pass
confirms them against real symbols.

### What is generated, and where

| Project | File | Content |
|---|---|---|
| web-domain | `role.g.cs` | partial property bodies with `private set`; `private Role(RoleId, string, string)`; `static Role Create(RoleId, string, string)` with guard clauses; `UpdateDetails(string, string)`; `SetPermissions(IReadOnlyList<string>)`, `SetAgentScopes(IReadOnlyList<string>)` with distinct + replace; `RolePermission`, `RoleAgentScope` value records with max-length guards; `private sealed partial class Invariants : AbstractValidator<Role>` with `NotEmpty` on Name and Description (from `RoleDetailsValidator`, read from the additional-file text) and a call to `ConfigureInvariants(this)`; the `private partial void ConfigureInvariants(Invariants)` declaration |
| web-application | `i-role-store.g.cs` | `IRoleStore` in the Profile shape: `FindAsync`, `ListAsync`, `AddAsync`, `UpdateAsync`, `RemoveAsync` (save inside each) |
| web-application | agreement analyzer (not a file) | errors below |
| web-infrastructure | `role-entity-type-configuration.g.cs` | `ToTable("roles","identity")`, typed-id conversion, `OwnsMany` onto `identity.role_permissions` `(RoleId, PermissionId)` and `identity.role_agent_scopes` `(RoleId, Scope)`, field access to the backing lists |

### What a contract change does

| Change in contracts | Result |
|---|---|
| Rename `IRoleDetails.Description` to `Summary` | **Build error** in web-application: `Role.Description` says `[FromContract("IRoleDetails")]` but no such member. The entity does not change. |
| Add `List<string> Tags` to a new `roles.set-tags` command | **Nothing happens** until a person adds a `[Child(Operation = "roles.set-tags")]` property. Analyzer **warning**: operation `roles.set-tags` in group `admin.roles` has no mutation on `Role`. |
| Add `[Operation("roles.archive")]` | Analyzer warning: no mutation. A person adds `[Mutation("roles.archive")] public partial void Archive();` and writes the body hook. |
| Change `RuleFor(Name).NotEmpty()` to `.MaximumLength(64)` | Regenerated `Invariants` picks up `MaximumLength(64)`; the golden diff gate shows it. |

### How 464 (SPA generator) reads it

The SPA references contracts, not domain. It reads `[Operation]` and the command members directly
and never needs this declaration. Child naming on the SPA side comes from the command member
(`PermissionIds`), not from `Role.Permissions`.

---

## Option 2A: full manifest on a contracts marker

### What a person writes

`features/admin/roles/role-aggregate-contracts.cs` (compiles into `web-contracts`)

```csharp
namespace TimeWarp.Architecture.Features.Admin.Roles;

[AggregateRoot("Role", Schema = "identity", Table = "roles")]
[AggregateFields(typeof(IRoleDetails), Validator = typeof(RoleDetailsValidator))]
[AggregateChild("Permissions", ItemName = "PermissionId", Source = typeof(SetRolePermissions), Member = nameof(SetRolePermissions.Command.PermissionIds), Table = "role_permissions", MaxLength = 128)]
[AggregateChild("AgentScopes", ItemName = "Scope", Source = typeof(SetRoleAgentScopes), Member = nameof(SetRoleAgentScopes.Command.Scopes), Table = "role_agent_scopes", MaxLength = 64)]
[AggregateOperations
(
  Create = typeof(CreateRole),
  Update = typeof(UpdateRole),
  Delete = typeof(DeleteRole),
  Get = typeof(GetRole),
  List = typeof(GetRoles),
  Set = [typeof(SetRolePermissions), typeof(SetRoleAgentScopes)]
)]
public static partial class RoleAggregate;
```

Plus, in the domain project, the hand-written half (the generator emits the shell, a person fills
the required hooks):

`features/admin/roles/role-domain.cs`

```csharp
namespace TimeWarp.Architecture.Features.Admin.Roles.Domain;

public sealed partial class Role
{
  private partial void ConfigureInvariants(Invariants rules)
  {
    rules.RuleForEach(r => r.Permissions).Must(p => PermissionCatalog.Contains(p.Value));
    rules.RuleForEach(r => r.AgentScopes).Must(s => ScopeCatalog.Contains(s.Value));
  }
}
```

`typeof` and `nameof` work here because the manifest sits beside the contracts. The domain pass
finds `[AggregateRoot]` in the parsed text and emits `Role` whole; the person's partial only
adds hooks.

### What is generated, and where

Same four outputs as 2C, with one difference: in web-domain the generator emits the **entire**
`Role` class (properties, constructor, mutations, `Invariants`), not just bodies for declared
partial properties. The hand-written file is hooks only.

### What a contract change does

| Change in contracts | Result |
|---|---|
| Rename `IRoleDetails.Description` to `Summary` | **Compiles.** `Role.Description` becomes `Role.Summary`; the EF mapping renames the column; the next `dev db migrate` produces a rename migration. The golden diff gate is the only thing that notices. |
| Add `List<string> Tags` to a new `roles.set-tags` command | **Nothing happens** until a person adds an `[AggregateChild]` line. Analyzer warning in web-contracts: operation `roles.set-tags` in group `admin.roles` is not in `RoleAggregate`. |
| Add `[Operation("roles.archive")]` | Analyzer warning: not in the manifest. A person adds `Archive = typeof(ArchiveRole)` to `[AggregateOperations]`; the generator emits `Archive()` calling a required `OnArchiving` hook. |
| Change `RuleFor(Name).NotEmpty()` to `.MaximumLength(64)` | Same as 2C. |

### How 464 (SPA generator) reads it

The SPA references contracts, so it can read `RoleAggregate` as metadata: which operations form
one aggregate, which command member is a child, what the root is called. 464's "opt-in shape"
decision could point at this same manifest instead of inventing `[ContractState]`.

---

## The difference in one table

| | 2C domain type | 2A contracts manifest |
|---|---|---|
| Number of names for the concept | 2 (`Role`, `IRoleDetails`) | 3 (`Role`, `IRoleDetails`, `RoleAggregate`) |
| Attribute arguments | strings (`"IRoleDetails"`, `"roles.set-permissions"`) | `typeof` / `nameof`, refactor-safe |
| Who owns the entity's shape | the domain file; contracts are checked against it | the manifest; the entity is derived |
| Contract rename | build error, entity unchanged | entity and column rename, migration follows |
| Where the person's hooks go | same file as the declaration | a second file in the domain project |
| What 464 reads | nothing from this; contracts only | the manifest, as metadata |
| Direction of truth | domain-first, contracts must agree | contract-first, domain follows |

That last row is the actual decision. Steve's stated goal is contract-driven: "the contract is the
agreement between front end and back end." 2A is the literal reading of that. 2C is the reading
where the contract drives the API and the domain is declared once, with a build check that they
agree.
