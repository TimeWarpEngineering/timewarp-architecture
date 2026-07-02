#region Purpose
// Well-known user ids so seed data, mocks, and contract handlers all reference the same sample identities.
#endregion

namespace TimeWarp.Architecture.Services;

public static class UserIds
{
  public static Guid SystemAdmin = Guid.Parse("24EEFBC1-54B5-42DF-895E-2C60C7423542");
  public static Guid Developer = Guid.Parse("C768C080-76C9-4045-962A-D0C07CDA4D82");
}
