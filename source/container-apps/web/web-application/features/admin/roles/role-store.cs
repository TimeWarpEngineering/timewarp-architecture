#region Purpose
// In-memory role storage shared by the roles handlers.
#endregion

#region Design
// Deliberate stub: roles are a template demonstration feature with no domain persistence. A
// static ConcurrentDictionary gives the handlers one coherent store so create/read/update/delete
// compose (the demo works end to end) without introducing repository plumbing the feature does
// not yet earn. Seeded with the well-known RoleIds so list/get return data on first run, matching
// the SPA's mock-mode responses. Replace with a repository when roles become a real feature; the
// contracts and endpoints do not change.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles.Application;

using System.Collections.Concurrent;
using TimeWarp.Architecture.Features.Authorization;

internal static class RoleStore
{
  internal static readonly ConcurrentDictionary<Guid, (string Name, string Description)> Roles = new
  (
    new[]
    {
      new KeyValuePair<Guid, (string, string)>(RoleIds.Administrator,
        (nameof(RoleIds.Administrator), "The Administrator role has access to all modules.")),
      new KeyValuePair<Guid, (string, string)>(RoleIds.Accountant,
        (nameof(RoleIds.Accountant), "The Accountant role has access to the accounting module."))
    }
  );
}
