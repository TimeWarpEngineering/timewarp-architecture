#region Purpose
// CLI binding of one operation: the Nuru route pattern.
#endregion

#region Design
// The purpose group lives on OperationAttribute, not on this binding. Pattern is the route
// alone. A host copies the catalog group to its own groupPath and does not prefix Pattern.
#endregion

namespace TimeWarp.Contracts;

/// <summary>CLI route pattern for one operation.</summary>
public sealed class CliOperationBinding
{
  /// <summary>Nuru route pattern.</summary>
  public required string Pattern { get; init; }
}
