# Aggregate inference from a contract: worked example (Roles + permissions)

Sources: TimeWarpEngineering/timewarp-architecture origin/HEAD `c12d08361` (read-only). Paths are relative to the repo root.
Legend: **[REAL]** is quoted verbatim from the repo. **[PROPOSED]** does not exist. Nothing was committed.

---

## 1. The contract [REAL]

`source/container-apps/web/features/admin/roles/set-role-permissions/set-role-permissions-contracts.cs`

```csharp
#region Purpose
// Endpoint-centric contract for replacing a role's permission membership (role as bundle).
#endregion

#region Design
// Task 182-004: PUT api/Roles/{RoleId}/permissions — full replace of IRolePermissionStore grants
// for one product role. RoleId from route (source-generated on partial Command). Validator
// requires every PermissionId ∈ PermissionIds.All (catalog only — no free-form strings).
// [EndpointAuthorize] admin.roles.manage; AuthenticationSchemes identity-session +
// mock-identity-session (same as Create/Update/Delete). Handler enforces protected-core on
// Administrator (RolePermissionSeed.AdminPermissions cannot be stripped) — store stays dumb.
// Response echoes stored permission ids after write.
// [Operation] names it roles.set-permissions for every transport. IIdempotentCommand: the PUT is
// a full replace, so a retry leaves the same grants.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles;

/// <summary>Replace stored permission ids for a role.</summary>
[ApiEndpoint]
[EndpointAuthorize
(
  Policy = PermissionIds.AdminRolesManage,
  AuthenticationSchemes = AuthenticationSchemeNames.IdentitySession + "," + AuthenticationSchemeNames.MockIdentitySession
)]
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
      RuleFor(x => x).SetValidator(new AuthApiRequestValidator());
      RuleFor(x => x.RoleId).NotEmpty();
      RuleFor(x => x.PermissionIds).NotNull();
      // Fully qualify product PermissionIds — Command.PermissionIds property would otherwise shadow.
      RuleForEach(x => x.PermissionIds)
        .Must(static id => global::TimeWarp.Architecture.Features.PermissionIds.All.Contains(id))
        .WithMessage("Each PermissionId must be a product permission (PermissionIds.All).");
    }
  }

  public sealed class Response
  {
    public List<string> PermissionIds { get; }

    public Response(List<string> permissionIds)
    {
      PermissionIds = permissionIds ?? [];
    }
  }

  public static MockResponseFactory<Response> GetMockResponseFactory()
  {
    return _ => new Response(
    [
      PermissionIds.AdminAccess,
      PermissionIds.AdminRolesRead,
      PermissionIds.AdminRolesManage,
      PermissionIds.AdminPrincipalsRead,
      PermissionIds.AdminPrincipalsManage,
      PermissionIds.AdminProfilesRead,
      PermissionIds.AdminFeedbackRead,
      PermissionIds.AdminFeedbackManage,
      PermissionIds.ProfileRead,
      PermissionIds.SettingsRead,
    ]);
  }
}
```

The sibling contracts (create-role, update-role, delete-role, get-role, get-roles) share
`role-details-contracts.cs` [REAL]:

```csharp
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

---

## 2. What exists today [REAL]

### 2a. A generated FastEndpoint

The generator is `source/analyzers/timewarp-architecture-analyzers/generators/fast-endpoint-source-generator.cs` (`GenerateEndpointClass`).
The snapshot below came from a local build of an older checkout:
`/workspace/askpanel/repo/artifacts/generated/web-server/.../FastEndpointSourceGenerator/SetRolePermissionsEndpoint.g.cs`
(repo at ee336d5, file dated 2026-10-29).

```csharp
using FastEndpoints;
using OneOf;
using System.Threading;
using System.Threading.Tasks;
using TimeWarp.Foundation.Features;

namespace TimeWarp.Architecture.Features.Admin.Roles;

/// <summary>
/// 
/// </summary>
/// <remarks>
/// 
/// </remarks>
public class SetRolePermissionsEndpoint : BaseFastEndpoint<SetRolePermissions.Command, SetRolePermissions.Response>
{
  public override void Configure()
  {
    Put("api/Roles/{RoleId:guid}/permissions");
    AuthSchemes("identity-session", "mock-identity-session");
         Policies("admin.roles.manage");
    
    Tags("Roles");
      Description(d => d.WithTags("Roles"));
  }
}
```

The summary is empty because `[Operation(Description=…)]` isn't fed into OpenAPI there. The generator reads the XML
`<summary>` on the request class, and that summary is on the outer class instead. That's a small fix on its own.

### 2b. How Roles are persisted today (no aggregate)

- `features/admin/roles/role-store-application.cs`: `internal static class RoleStore` is a `ConcurrentDictionary<Guid,(Name,Description)>`.
  Its Design note says: *"Replace with a repository when roles become durable; contracts/endpoints stay stable."*
- `platform/authorization/role-permission-grant-infrastructure.cs`: `RolePermissionGrant { Guid RoleId; string PermissionId }`, a plain join row with no navigation properties.
- `platform/authorization/role-permission-grant-entity-type-configuration-infrastructure.cs`: maps the grant to `identity.role_permissions` with a composite key and `PermissionId` max length 128.
- `platform/authorization/i-role-permission-store-application.cs`: the port `IRolePermissionStore` (Get/SetPermissionIdsForRoleAsync), with in-memory and EF implementations.

Today the "role owns its permissions" rule lives in the handler and in two stores. Roles are the right
candidate for an aggregate, and the code itself already says so.

### 2c. The golden aggregate (exemplar per skill `tw-aggregate-pattern`)

Profile is the exemplar:
- `features/profile/profile-id-domain.cs`: `[TypedId] public readonly partial record struct ProfileId;`
- `features/profile/profile-domain.cs`: `public sealed class Profile : Entity<ProfileId>, IAggregateRoot`, with a private constructor, fail-closed `static Create(...)`, named mutations (`Rename`, `SetTheme`…), `private set`, and a `private sealed class Invariants : AbstractValidator<Profile>` that TWA0011/TWA0012 enforce.
- `features/profile/profile-entity-type-configuration-infrastructure.cs`: `ProfileEntityTypeConfiguration`, with `SchemaName`/`TableName` consts, `ToTable`, `HasKey`, TypedId `HasConversion(id => id.Value, value => ProfileId.From(value)).ValueGeneratedNever()`, and no `IsConcurrencyToken` (the `AggregateVersionConvention` adds it).

Conventions observed:

| Concern | Convention |
|---|---|
| File names | kebab-case `<name>[-<function>]-<layer>.cs`; layers include `contracts`, `domain`, `application`, `infrastructure` |
| Namespaces | `…Features.<Slice>` for contracts; `.Domain`, `.Application`, `.Infrastructure` sub-namespaces (Profile, Roles.Application) |
| Typed id | `[TypedId]` partial record struct, generated by `typed-id-source-generator.cs`, which also emits EF ValueConverters |
| Base types | `Entity<TId>`, `IAggregateRoot` (`foundation/foundation-domain/entities/base/i-aggregate-root.cs`) |
| Save path | `AggregateDbContext` and `DomainInvariantsGuard` run `Invariants`; `Version` comes from convention |
| Ports | named `I<Name>Store` (`IRolePermissionStore`, `IPrincipalRoleStore`), with in-memory and EF implementations. There's no real `*Repository` type in the template. |

---

## 3. Proposal

### 3a. Contract-level declaration [PROPOSED]

New file `features/admin/roles/role-aggregate-contracts.cs`. Contracts can't reference domain types
("contracts must not reference domain", per the Profile Design note), so the declaration only uses
contract types, names, and attributes the generator reads by metadata name.

```csharp
#region Purpose
// Declares the Role aggregate the Roles contracts operate on, so domain, port and EF mapping are derived.
#endregion

#region Design
// PROPOSED. Fields come from IRoleDetails (Name, Description). The id comes from the {RoleId:guid}
// route segment the operations share. Children come from SetRolePermissions.Command.PermissionIds.
// Mutations are named after the [Operation] verbs that target this root. Validator rules on those
// contracts become the aggregate's Invariants. Nothing here references the domain assembly.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles;

[AggregateRoot("Role", Schema = "identity", Table = "roles")]
[AggregateFields(typeof(IRoleDetails), Validator = typeof(RoleDetailsValidator))]
[AggregateChildren
(
  "Permissions",
  ItemName = "PermissionId",
  Table = "role_permissions",
  MaxLength = 128,
  Source = typeof(SetRolePermissions)          // List<string> PermissionIds + RuleForEach(Must ∈ PermissionIds.All)
)]
[AggregateOperations
(
  Create = typeof(CreateRole),                 // roles.create  → Role.Create(name, description)
  Update = typeof(UpdateRole),                 // roles.update  → role.UpdateDetails(name, description)
  Delete = typeof(DeleteRole),                 // roles.delete  → IRoleStore.RemoveAsync
  SetChildren = typeof(SetRolePermissions)     // roles.set-permissions → role.SetPermissions(ids)
)]
public static partial class RoleAggregate;
```

What gets inferred, and from where:

| Output | Inferred from |
|---|---|
| `RoleId` typed id | `{RoleId:guid}` in `[ApiRoute]` on UpdateRole/SetRolePermissions |
| `Name`, `Description` (required) | `IRoleDetails` + `RoleDetailsValidator` NotEmpty |
| `Permissions` child set | `SetRolePermissions.Command.PermissionIds : List<string>` |
| "each permission ∈ PermissionIds.All" invariant | the `RuleForEach(...).Must(...)` on that validator |
| method names | `[Operation]` names `roles.create/update/set-permissions` |
| schema/table | attribute (default would be slice name: `roles`/`roles`) |

Not inferable, so it stays hand-written: the Administrator protected-core rule (`AdminLockoutGuards`).
The generator emits a `partial` hook, `OnSettingPermissions`, so a person adds that rule.

### 3b. Generated output [PROPOSED], mirroring real naming

**`role-id-domain.g.cs`** (Profile pattern; the existing TypedId generator fills the body)

```csharp
namespace TimeWarp.Architecture.Features.Admin.Roles.Domain;

[TypedId]
public readonly partial record struct RoleId;
```

**`role-domain.g.cs`**

```csharp
namespace TimeWarp.Architecture.Features.Admin.Roles.Domain;

using FluentValidation;

public sealed partial class Role : Entity<RoleId>, IAggregateRoot
{
  private readonly List<RolePermission> PermissionList = [];

  private Role(RoleId id, string name, string description)
    : base(id)
  {
    Name = name;
    Description = description;
  }

  public string Name { get; private set; }
  public string Description { get; private set; }
  public IReadOnlyList<RolePermission> Permissions => PermissionList;

  // roles.create
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

  // roles.update
  public void UpdateDetails(string name, string description)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(name);
    ArgumentException.ThrowIfNullOrWhiteSpace(description);
    Name = name;
    Description = description;
  }

  // roles.set-permissions (full replace, matches IIdempotentCommand)
  public void SetPermissions(IReadOnlyList<string> permissionIds)
  {
    ArgumentNullException.ThrowIfNull(permissionIds);
    OnSettingPermissions(permissionIds);   // hand-written partial: protected-core lives here
    PermissionList.Clear();
    PermissionList.AddRange(permissionIds.Distinct(StringComparer.Ordinal).Select(RolePermission.Create));
  }

  partial void OnSettingPermissions(IReadOnlyList<string> permissionIds);

  private sealed class Invariants : AbstractValidator<Role>
  {
    public Invariants()
    {
      RuleFor(role => role.Name).NotEmpty();          // from RoleDetailsValidator
      RuleFor(role => role.Description).NotEmpty();   // from RoleDetailsValidator
      RuleForEach(role => role.Permissions)           // from SetRolePermissions.Validator
        .Must(static p => global::TimeWarp.Architecture.Features.PermissionIds.All.Contains(p.PermissionId));
    }
  }
}

public sealed record RolePermission
{
  private RolePermission(string permissionId) => PermissionId = permissionId;

  public string PermissionId { get; private set; }

  public static RolePermission Create(string permissionId)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(permissionId);
    ArgumentOutOfRangeException.ThrowIfGreaterThan(permissionId.Length, 128, nameof(permissionId));
    return new RolePermission(permissionId);
  }
}
```

The existing TWA0011/TWA0012 analyzers would accept this shape: a nested `Invariants` that is private.

**`i-role-store-application.g.cs`**: the port. It follows the house `I<Name>Store` naming rather than "Repository".

```csharp
namespace TimeWarp.Architecture.Features.Admin.Roles.Application;

using TimeWarp.Architecture.Features.Admin.Roles.Domain;

/// <summary>Durable Role aggregates (generated from RoleAggregate).</summary>
public interface IRoleStore
{
  Task<Role?> GetAsync(RoleId id, CancellationToken cancellationToken = default);   // roles.get
  Task<IReadOnlyList<Role>> ListAsync(CancellationToken cancellationToken = default); // roles.list
  void Add(Role role);                                                              // roles.create
  void Remove(Role role);                                                           // roles.delete
  Task SaveChangesAsync(CancellationToken cancellationToken = default);             // runs DomainInvariantsGuard
}
```

The name clashes with today's `internal static class RoleStore`. The generated port would replace it,
or be named `IRoleRepository` as RoleStore's Design note suggests. That's Steven's call.

**`role-entity-type-configuration-infrastructure.g.cs`** (Profile pattern + grant mapping)

```csharp
namespace TimeWarp.Architecture.Features.Admin.Roles.Infrastructure;

using TimeWarp.Architecture.Features.Admin.Roles.Domain;

public sealed class RoleEntityTypeConfiguration : IEntityTypeConfiguration<Role>
{
  public const string SchemaName = "identity";
  public const string TableName = "roles";
  public const string PermissionsTableName = "role_permissions";

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

    builder.OwnsMany
    (
      role => role.Permissions,
      permissions =>
      {
        permissions.ToTable(PermissionsTableName, SchemaName);
        permissions.WithOwner().HasForeignKey("RoleId");
        permissions.HasKey("RoleId", nameof(RolePermission.PermissionId));
        permissions.Property(p => p.PermissionId).IsRequired().HasMaxLength(128);
      }
    );

    builder.Navigation(role => role.Permissions)
      .UsePropertyAccessMode(PropertyAccessMode.Field)
      .HasField("PermissionList");
    // Version: no IsConcurrencyToken — AggregateVersionConvention supplies it.
  }
}
```

It reuses the real table name and key (`identity.role_permissions`, `(RoleId, PermissionId)`, 128). That makes
`RolePermissionGrant` and its configuration redundant, so a migration and a retirement of `IRolePermissionStore`
are part of the change. Roles also become real rows: today `RoleIds` are compile-time Guids with no
roles table, so those four ids would need seed data.

---

## 4. Notes on effort and risk
- Typed id, entity shell, Invariants, EF mapping and port are mechanical (low–medium effort), because every piece maps to an existing generator or pattern.
- Mutation names derived from `[Operation]` verbs need a small, fixed vocabulary (create/update/delete/set-*), or an override.
- Cross-slice rules (protected-core, last admin) stay hand-written partials.
- Existing data: grants already live in `identity.role_permissions`, so the owned-collection mapping must match it, or a migration has to move them.
