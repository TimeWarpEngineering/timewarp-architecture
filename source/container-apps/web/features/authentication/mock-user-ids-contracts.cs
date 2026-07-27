#region Purpose
// Well-known mock-mode user ids so the SPA's mock authentication and the contracts' mock response
// factories agree on the same sample identities.
#endregion

namespace TimeWarp.Architecture.Features.Authentication;

public static class MockUserIds
{
  public static readonly Guid SystemAdmin = Guid.Parse("24EEFBC1-54B5-42DF-895E-2C60C7423542");
  public static readonly Guid Developer = Guid.Parse("C768C080-76C9-4045-962A-D0C07CDA4D82");
}
