#region Purpose
// Non-permission SPA policy names (Anonymous / Authenticated) shared by registration and consumers.
#endregion

#region Design
// Task 182-003: permission-backed gates use PermissionIds (policy name == permission id).
// Only scheme-composition / always-true exceptions remain here — disposition keeps Anonymous
// and Authenticated outside the permission registry. CanView* role-policy names deleted with
// RolePolicyGrants.
#endregion

namespace TimeWarp.Architecture;

public static class AuthorizationConstants
{
  public static class Policies
  {
    public const string Anonymous = nameof(Anonymous);

    /// <summary>Any signed-in principal (identity-session, mock, or Entra). Not permission-gated.</summary>
    public const string Authenticated = nameof(Authenticated);
  }
}
