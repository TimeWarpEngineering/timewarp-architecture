#region Purpose
// CLI binding of one operation: the Nuru route pattern.
#endregion

namespace TimeWarp.Contracts;

/// <summary>CLI route pattern for one operation.</summary>
public sealed class CliOperationBinding
{
  /// <summary>Nuru route pattern.</summary>
  public required string Pattern { get; init; }
}
