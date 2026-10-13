#region Purpose
// The generated @ resource types, filtered by what the signed-in user may list.
#endregion

#region Design
// Task 345. All is GeneratedResourceReferenceTypes (one entry per [ReferenceableResource]
// contract); nothing here lists types by hand. PermittedAsync checks each type's list policy
// with the SPA's IAuthorizationService, the same claim policies the palette and nav use, so a
// Member never sees an admin-only type. A type with no policy needs a signed-in user. This is
// a display filter: the list query's [EndpointAuthorize] is the boundary, and the server
// re-authorizes any action that later receives the id.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>Registry of resource types the Ask composer can reference.</summary>
public static class ResourceReferenceRegistry
{
  public static IReadOnlyList<ResourceReferenceType> All { get; } = GeneratedResourceReferenceTypes.Create();

  public static ResourceReferenceType? Find(string? prefix)
  {
    foreach (ResourceReferenceType type in All)
    {
      if (string.Equals(type.Prefix, prefix, StringComparison.Ordinal))
      {
        return type;
      }
    }

    return null;
  }

  /// <summary>The types <paramref name="user"/> may list, in registry order.</summary>
  public static async Task<IReadOnlyList<ResourceReferenceType>> PermittedAsync
  (
    ClaimsPrincipal user,
    IAuthorizationService authorizationService
  )
  {
    ArgumentNullException.ThrowIfNull(user);
    ArgumentNullException.ThrowIfNull(authorizationService);
    if (user.Identity?.IsAuthenticated != true)
    {
      return [];
    }

    List<ResourceReferenceType> permitted = [];
    foreach (ResourceReferenceType type in All)
    {
      if (type.Policy is null || (await authorizationService.AuthorizeAsync(user, type.Policy)).Succeeded)
      {
        permitted.Add(type);
      }
    }

    return permitted;
  }
}
