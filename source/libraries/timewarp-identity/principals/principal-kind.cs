#region Purpose
// Discriminates the class of principal: human user, autonomous agent, or service account.
#endregion

#region Design
// Reserved zero (None) so default/missing enum values fail closed at domain entry rather than becoming Human.
// Domain Create rejects None; contract validators can reject None again later as defense-in-depth.
#endregion

namespace TimeWarp.Identity;

/// <summary>
/// Class of principal: human user, autonomous agent, or service account.
/// </summary>
public enum PrincipalKind
{
  /// <summary>Uninitialized value; rejected by <see cref="Principal.Create"/>.</summary>
  None = 0,

  /// <summary>Human end-user principal.</summary>
  Human = 1,

  /// <summary>Autonomous agent principal (may authenticate via agent keys).</summary>
  Agent = 2,

  /// <summary>Non-human service account principal.</summary>
  Service = 3,
}
