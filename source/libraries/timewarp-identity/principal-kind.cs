#region Purpose
// Discriminates the class of principal: human user, autonomous agent, or service account.
#endregion

namespace TimeWarp.Identity;

public enum PrincipalKind
{
  Human = 0,
  Agent = 1,
  Service = 2,
}
