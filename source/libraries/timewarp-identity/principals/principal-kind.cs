#region Purpose
// Discriminates the class of principal: human user, autonomous agent, or service account.
#endregion

#region Design
// Reserved zero (None) so default/missing enum values fail closed at domain entry rather than becoming Human.
// Domain Create rejects None; contract validators can reject None again later as defense-in-depth.
#endregion

namespace TimeWarp.Identity;

public enum PrincipalKind
{
  None = 0,
  Human = 1,
  Agent = 2,
  Service = 3,
}
