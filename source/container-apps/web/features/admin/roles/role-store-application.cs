#region Purpose
// In-memory role storage shared by the roles handlers.
#endregion

#region Design
// Stub until 147-004 principal/role persistence: ConcurrentDictionary so CRUD composes without
// EF. Seeded with product RoleIds (Member, Operator, Administrator, Developer) — task 147-002.
// Replace with a repository when roles become durable; contracts/endpoints stay stable.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles.Application;

using System.Collections.Concurrent;
using TimeWarp.Architecture.Features;

internal static class RoleStore
{
  internal static readonly ConcurrentDictionary<Guid, (string Name, string Description)> Roles = new
  (
    new[]
    {
      new KeyValuePair<Guid, (string, string)>(
        RoleIds.Member,
        (nameof(RoleIds.Member), "Default human principal after passkey login; self-service only.")),
      new KeyValuePair<Guid, (string, string)>(
        RoleIds.Operator,
        (nameof(RoleIds.Operator), "Marketplace and job oversight (agentic shop ops).")),
      new KeyValuePair<Guid, (string, string)>(
        RoleIds.Administrator,
        (nameof(RoleIds.Administrator), "Tenant admin: principals, roles, system settings.")),
      new KeyValuePair<Guid, (string, string)>(
        RoleIds.Developer,
        (nameof(RoleIds.Developer), "Template dogfood: demos and diagnostics.")),
    }
  );
}
