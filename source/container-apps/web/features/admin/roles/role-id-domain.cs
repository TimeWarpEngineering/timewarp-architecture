#region Purpose
// Stable Role identity: a non-empty Guid that never recycles and identifies a product or custom role.
#endregion

#region Design
// [TypedId] generates the house BCL surface (Value/New/From, STJ converter, parsing, comparison),
// as ProfileId does, so call sites cannot mix Role Guids with other aggregates' ids. Contracts and
// the authorization read ports still carry raw Guids (RoleIds, IRolePermissionStore); handlers
// and stores convert at the edge with RoleId.From. Empty is unguardable for default(T), so use
// IsEmpty at edges.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles.Domain;

// TypedId attribute namespace: dual-mode MSBuild <Using> in web-domain.csproj (task 115).

[TypedId]
public readonly partial record struct RoleId;
