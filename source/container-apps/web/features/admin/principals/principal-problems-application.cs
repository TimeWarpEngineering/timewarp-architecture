#region Purpose
// Shared problem details for the admin Principals handlers.
#endregion

#region Design
// Task 328. NotFound is a missing principal (404). ProfileAccessRequired is 403 and is returned
// only when a caller without admin.profiles.read asks to search or sort by a profile field,
// which would otherwise leak the field through the ordering. Merely listing without the
// permission is not an error: the profile columns come back null.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Principals.Application;

using TimeWarp.Identity;

internal static class PrincipalProblems
{
  public static SharedProblemDetails NotFound(PrincipalId principalId) => new()
  {
    Title = "Principal not found",
    Status = 404,
    Detail = $"No principal exists with id '{principalId}'."
  };

  public static SharedProblemDetails ProfileAccessRequired() => new()
  {
    Title = "Profile access required",
    Status = 403,
    Detail = $"Searching or sorting by profile fields requires {PermissionIds.AdminProfilesRead}."
  };
}
