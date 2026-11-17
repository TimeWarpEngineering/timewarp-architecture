# Option 2C, end to end, as code

Three parts: **input** (the contracts, real, unchanged), **what a person writes** (two domain
files and one MSBuild line), **what the generator emits** (three files, one per layer) plus the
build checks. Then one handler before and after, so the call site is visible.

Generated code mirrors the Profile exemplar line for line where a counterpart exists
(`profile-domain.cs`, `i-profile-store-application.cs`,
`profile-entity-type-configuration-infrastructure.cs`).

---

## Part 1: input. The contracts (real, in `web-contracts`, unchanged)

`features/admin/roles/role-details-contracts.cs`

```csharp
namespace TimeWarp.Architecture.Features.Admin.Roles;

public interface IRoleDetails
{
  public string Name { get; set; }
  public string Description { get; set; }
}

public sealed class RoleDetailsValidator : AbstractValidator<IRoleDetails>
{
  public RoleDetailsValidator()
  {
    RuleFor(r => r.Name).NotEmpty();
    RuleFor(r => r.Description).NotEmpty();
  }
}
```

`features/admin/roles/set-role-permissions/set-role-permissions-contracts.cs` (trimmed to what the generator reads)

```csharp
[ApiEndpoint]
public static partial class SetRolePermissions
{
  [ApiRoute("api/Roles/{RoleId:guid}/permissions", HttpVerb.Put)]
  [Operation("roles.set-permissions", Description = "Replace a role's permission ids.", Group = "admin.roles")]
  public sealed partial class Command : IAuthApiRequest, IIdempotentCommand<OneOf<Response, SharedProblemDetails>>
  {
    public Guid UserId { get; set; }
    public List<string> PermissionIds { get; set; } = [];
  }

  public sealed class Validator : AbstractValidator<Command>
  {
    public Validator()
    {
      RuleFor(x => x.RoleId).NotEmpty();
      RuleFor(x => x.PermissionIds).NotNull();
      RuleForEach(x => x.PermissionIds)
        .Must(static id => global::TimeWarp.Architecture.Features.PermissionIds.All.Contains(id));
    }
  }
}
```

`features/admin/roles/set-role-agent-scopes/set-role-agent-scopes-contracts.cs` (same shape)

```csharp
[ApiEndpoint]
public static partial class SetRoleAgentScopes
{
  [ApiRoute("api/Roles/{RoleId:guid}/agent-scopes", HttpVerb.Put)]
  [Operation("roles.set-agent-scopes", Group = "admin.roles")]
  public sealed partial class Command : IAuthApiRequest, IIdempotentCommand<OneOf<Response, SharedProblemDetails>>
  {
    public Guid UserId { get; set; }
    public List<string> Scopes { get; set; } = [];
  }
}
```

The other five: `CreateRole.Command : IRoleDetails` with `[Operation("roles.create")]`,
`UpdateRole.Command : IRoleDetails` with `[ApiRoute("api/Roles/{RoleId:guid}")]` and
`[Operation("roles.update")]`, `DeleteRole` (`roles.delete`), `GetRole` (`roles.get`),
`GetRoles` (`roles.list`). All `Group = "admin.roles"`.

---

## Part 2: what a person writes

### 2a. `features/admin/roles/role-id-domain.cs` (compiles into `web-domain`; same as Profile today)

```csharp
namespace TimeWarp.Architecture.Features.Admin.Roles.Domain;

[TypedId]
public readonly partial record struct RoleId;
```

### 2b. `features/admin/roles/role-domain.cs` (compiles into `web-domain`)

```csharp
#region Purpose
// The Role aggregate: a product role, its permission ids, and its agent scopes.
#endregion

#region Design
// Declared here, generated against the admin.roles contracts (role.g.cs). Each member names the
// contract it agrees with; the application-layer check fails the build when they drift.
// Catalog rules are hand-written because PermissionIds.All and AgentScopes.All are not visible
// from this project; the catalogs are copied in as domain constants (RoleCatalogs).
// Protected core (Administrator keeps its core permissions) is NOT here: it is an application
// rule in the SetRolePermissions handler and returns a 409.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles.Domain;

using FluentValidation;

[Aggregate(OperationGroup = "admin.roles", Schema = "identity", Table = "roles")]
public sealed partial class Role : Entity<RoleId>, IAggregateRoot
{
  [FromContract(Details = "IRoleDetails")]
  public partial string Name { get; }

  [FromContract(Details = "IRoleDetails")]
  public partial string Description { get; }

  [Child(Operation = "roles.set-permissions", Member = "PermissionIds", Table = "role_permissions", ItemColumn = "PermissionId", MaxLength = 128)]
  public partial IReadOnlyList<RolePermission> Permissions { get; }

  [Child(Operation = "roles.set-agent-scopes", Member = "Scopes", Table = "role_agent_scopes", ItemColumn = "Scope", MaxLength = 64)]
  public partial IReadOnlyList<RoleAgentScope> AgentScopes { get; }

  // Generated as `private partial void` with no body: CS8795 until this exists.
  private partial void ConfigureInvariants(Invariants rules)
  {
    rules.RuleForEach(role => role.Permissions)
      .Must(static p => RoleCatalogs.PermissionIds.Contains(p.Value))
      .WithMessage("Each permission must be a product permission.");

    rules.RuleForEach(role => role.AgentScopes)
      .Must(static s => RoleCatalogs.AgentScopes.Contains(s.Value))
      .WithMessage("Each scope must be a known agent scope.");
  }
}
```

That is the whole hand-written aggregate: four partial property declarations and one hook body.

### 2c. One generated line in `msbuild/feature-filename-grammar.g.props` (the grammar generator adds it; nobody hand-edits)

```xml
<!-- web-domain: read every contracts file as text so the aggregate generator can see it. No reference. -->
<AdditionalFiles Include="$(WebFeatureTreeRoot)/**/*-contracts.cs" Condition="'$(MSBuildProjectName)' == 'web-domain'" />
```

---

## Part 3: what the generator emits

### 3a. `web-domain`: `obj/.../Role.g.cs`

```csharp
// <auto-generated/>
#nullable enable
namespace TimeWarp.Architecture.Features.Admin.Roles.Domain;

using FluentValidation;

public sealed partial class Role
{
  private readonly List<RolePermission> PermissionList = [];
  private readonly List<RoleAgentScope> AgentScopeList = [];

  private Role(RoleId id, string name, string description)
    : base(id)
  {
    Name = name;
    Description = description;
  }

  // [FromContract(Details = "IRoleDetails")]
  public partial string Name { get; private set; }
  public partial string Description { get; private set; }

  // [Child(Operation = "roles.set-permissions")]
  public partial IReadOnlyList<RolePermission> Permissions => PermissionList;

  // [Child(Operation = "roles.set-agent-scopes")]
  public partial IReadOnlyList<RoleAgentScope> AgentScopes => AgentScopeList;

  // roles.create  (CreateRole.Command : IRoleDetails)
  public static Role Create(string name, string description)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(name);
    ArgumentException.ThrowIfNullOrWhiteSpace(description);
    return new Role(RoleId.New(), name, description);
  }

  public static Role Create(RoleId id, string name, string description)
  {
    if (id.IsEmpty)
    {
      throw new ArgumentException("RoleId must be non-empty.", nameof(id));
    }

    ArgumentException.ThrowIfNullOrWhiteSpace(name);
    ArgumentException.ThrowIfNullOrWhiteSpace(description);
    return new Role(id, name, description);
  }

  // roles.update  (UpdateRole.Command : IRoleDetails)
  public void UpdateDetails(string name, string description)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(name);
    ArgumentException.ThrowIfNullOrWhiteSpace(description);
    Name = name;
    Description = description;
  }

  // roles.set-permissions  (SetRolePermissions.Command.PermissionIds, IIdempotentCommand: full replace)
  public void SetPermissions(IReadOnlyCollection<string> permissionIds)
  {
    ArgumentNullException.ThrowIfNull(permissionIds);
    PermissionList.Clear();
    PermissionList.AddRange(permissionIds.Distinct(StringComparer.Ordinal).Select(RolePermission.Create));
  }

  // roles.set-agent-scopes  (SetRoleAgentScopes.Command.Scopes, IIdempotentCommand: full replace)
  public void SetAgentScopes(IReadOnlyCollection<string> scopes)
  {
    ArgumentNullException.ThrowIfNull(scopes);
    AgentScopeList.Clear();
    AgentScopeList.AddRange(scopes.Distinct(StringComparer.Ordinal).Select(RoleAgentScope.Create));
  }

  // Hook. No body here: the hand-written partial must supply one or the build fails (CS8795).
  private partial void ConfigureInvariants(Invariants rules);

  private sealed class Invariants : AbstractValidator<Role>
  {
    public Invariants(Role owner)
    {
      // From RoleDetailsValidator (role-details-contracts.cs)
      RuleFor(role => role.Name).NotEmpty();
      RuleFor(role => role.Description).NotEmpty();

      // From SetRolePermissions.Validator / SetRoleAgentScopes.Validator: NotNull is satisfied
      // by construction (lists are never null). The Must(...) rules are catalog lookups the
      // domain cannot see, so they are the owner's responsibility:
      owner.ConfigureInvariants(this);
    }
  }
}

public sealed record RolePermission
{
  public const int MaxLength = 128;

  private RolePermission(string value) => Value = value;

  public string Value { get; }

  public static RolePermission Create(string value)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(value);
    ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, MaxLength, nameof(value));
    return new RolePermission(value);
  }
}

public sealed record RoleAgentScope
{
  public const int MaxLength = 64;

  private RoleAgentScope(string value) => Value = value;

  public string Value { get; }

  public static RoleAgentScope Create(string value)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(value);
    ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, MaxLength, nameof(value));
    return new RoleAgentScope(value);
  }
}
```

Note on `Invariants(Role owner)`: TWA0011/TWA0012 and `DomainInvariantsGuard` expect a private
nested `Invariants` with a parameterless constructor today (`missing-invariants-validator-exception.cs`).
Either the guard learns an owner-taking constructor, or the generator emits a parameterless
`Invariants` that calls a `static partial void ConfigureInvariants(Invariants)`. That is a
generator-task detail, not a Decision 2 difference; the hook stays required either way.

### 3b. `web-application`: `obj/.../IRoleStore.g.cs` (Profile shape, plus remove because `roles.delete` exists)

```csharp
// <auto-generated/>
#nullable enable
namespace TimeWarp.Architecture.Features.Admin.Roles.Application;

using TimeWarp.Architecture.Features.Admin.Roles.Domain;

/// <summary>Durable Role aggregates. Generated from the admin.roles operation group.</summary>
public interface IRoleStore
{
  Task<Role?> FindAsync(RoleId id, CancellationToken cancellationToken = default);                 // roles.get
  Task<IReadOnlyList<Role>> FindManyAsync(IReadOnlyCollection<RoleId> ids, CancellationToken cancellationToken = default);
  Task<IReadOnlyList<Role>> ListAsync(CancellationToken cancellationToken = default);             // roles.list
  Task AddAsync(Role role, CancellationToken cancellationToken = default);                        // roles.create
  Task UpdateAsync(Role role, CancellationToken cancellationToken = default);                     // roles.update, set-*
  Task RemoveAsync(Role role, CancellationToken cancellationToken = default);                     // roles.delete
}
```

Implementations are **not** generated: `EfRoleStore` and `InMemoryRoleStore` are hand-written
like `EfProfileStore` / `InMemoryProfileStore`. The generator emits two stubs whose methods throw
`NotImplementedException` so the module registration compiles and the first test fails loudly.

### 3c. `web-infrastructure`: `obj/.../RoleEntityTypeConfiguration.g.cs`

```csharp
// <auto-generated/>
#nullable enable
namespace TimeWarp.Architecture.Features.Admin.Roles.Infrastructure;

using TimeWarp.Architecture.Features.Admin.Roles.Domain;

public sealed class RoleEntityTypeConfiguration : IEntityTypeConfiguration<Role>
{
  public const string SchemaName = "identity";
  public const string TableName = "roles";
  public const string PermissionsTableName = "role_permissions";
  public const string AgentScopesTableName = "role_agent_scopes";

  public void Configure(EntityTypeBuilder<Role> builder)
  {
    builder.ToTable(TableName, SchemaName);

    builder.HasKey(role => role.Id);

    builder.Property(role => role.Id)
      .HasConversion
      (
        id => id.Value,
        value => RoleId.From(value)
      )
      .ValueGeneratedNever();

    builder.Property(role => role.Name).IsRequired();
    builder.Property(role => role.Description).IsRequired();

    // [Child] Permissions -> identity.role_permissions (RoleId, PermissionId), the existing grant table
    builder.OwnsMany
    (
      role => role.Permissions,
      permissions =>
      {
        permissions.ToTable(PermissionsTableName, SchemaName);
        permissions.WithOwner().HasForeignKey("RoleId");
        permissions.Property(p => p.Value).HasColumnName("PermissionId").HasMaxLength(RolePermission.MaxLength).IsRequired();
        permissions.HasKey("RoleId", "Value");
      }
    );
    builder.Navigation(role => role.Permissions).UsePropertyAccessMode(PropertyAccessMode.Field).HasField("PermissionList");

    // [Child] AgentScopes -> identity.role_agent_scopes (RoleId, Scope), the existing grant table
    builder.OwnsMany
    (
      role => role.AgentScopes,
      scopes =>
      {
        scopes.ToTable(AgentScopesTableName, SchemaName);
        scopes.WithOwner().HasForeignKey("RoleId");
        scopes.Property(s => s.Value).HasColumnName("Scope").HasMaxLength(RoleAgentScope.MaxLength).IsRequired();
        scopes.HasKey("RoleId", "Value");
      }
    );
    builder.Navigation(role => role.AgentScopes).UsePropertyAccessMode(PropertyAccessMode.Field).HasField("AgentScopeList");

    builder.Property(role => role.Version)
      .UsePropertyAccessMode(PropertyAccessMode.Property);
  }
}
```

### 3d. The build checks (analyzer in `web-application`, which sees contracts and domain as metadata)

```text
TWA0040 error   Role.Description: [FromContract(Details = "IRoleDetails")] but IRoleDetails has no member 'Description'.
TWA0041 error   Role.Permissions: [Child(Operation = "roles.set-permissions", Member = "PermissionIds")] but the command for that operation has no List<string> member 'PermissionIds'.
TWA0042 warning Operation 'roles.set-tags' in group 'admin.roles' has no mutation on Role. Add a [Child] or [Mutation] member, or mark the operation [NotAggregate].
TWA0043 error   Role declares [Aggregate(OperationGroup = "admin.roles")] but no contract in web-contracts carries that group.
```

And in `web-domain` itself, the generator reports when the additional-file text does not contain
what a member names (same ids, earlier, lower confidence because there is no semantic model).

### 3e. The golden gate (CI, same as `goldens/ingress`)

`goldens/aggregates/Role.g.cs`, `IRoleStore.g.cs`, `RoleEntityTypeConfiguration.g.cs` are checked
in. `dev regenerate-generated` rewrites them; CI runs it and `git diff --exit-code -- goldens/aggregates`.
The hand-written Roles aggregate from the first task is what these goldens are copied from.

---

## Part 4: one handler, before and after

`features/admin/roles/set-role-permissions/set-role-permissions-handler-application.cs`

Before (real, today):

```csharp
public override async Task<OneOf<Response, SharedProblemDetails>> Handle(Command command, CancellationToken cancellationToken)
{
  if (!RoleStore.Roles.ContainsKey(command.RoleId))
  {
    return RoleProblems.NotFound(command.RoleId);
  }

  OneOf<Success, SharedProblemDetails> guard = await AdminLockoutGuards.EnsureAdminCoreAsync(command.RoleId, command.PermissionIds, RolePermissionStore, cancellationToken);
  if (guard.IsT1) return guard.AsT1;

  await RolePermissionStore.SetPermissionIdsForRoleAsync(command.RoleId, command.PermissionIds, cancellationToken);
  IReadOnlyList<string> stored = await RolePermissionStore.GetPermissionIdsForRoleAsync(command.RoleId, cancellationToken);
  return new Response([.. stored]);
}
```

After:

```csharp
public override async Task<OneOf<Response, SharedProblemDetails>> Handle(Command command, CancellationToken cancellationToken)
{
  Role? role = await RoleStore.FindAsync(RoleId.From(command.RoleId), cancellationToken);
  if (role is null)
  {
    return RoleProblems.NotFound(command.RoleId);
  }

  // Application rule, unchanged: protected core stays here and still returns 409.
  OneOf<Success, SharedProblemDetails> guard = AdminLockoutGuards.EnsureAdminCore(role, command.PermissionIds);
  if (guard.IsT1) return guard.AsT1;

  role.SetPermissions(command.PermissionIds);
  await RoleStore.UpdateAsync(role, cancellationToken);   // DomainInvariantsGuard runs Invariants here
  return new Response([.. role.Permissions.Select(p => p.Value)]);
}
```

`IRolePermissionStore` keeps its read methods for the permission evaluator; its EF implementation
now queries `identity.role_permissions` through the owned mapping. Its `Set...` method and
`RolePermissionGrant` go away.
