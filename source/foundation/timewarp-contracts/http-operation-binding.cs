#region Purpose
// HTTP binding of one operation: route template and verb.
#endregion

#region Design
// Verb is text (Get, Post, Put, Delete) so this package does not reference the foundation HTTP enum.
// The template's [ApiRoute] generator is the writer. CLI and MCP hosts leave this null.
#endregion

namespace TimeWarp.Contracts;

/// <summary>HTTP route and verb for one operation.</summary>
public sealed class HttpOperationBinding
{
  /// <summary>Route template, in the host's route syntax.</summary>
  public required string Route { get; init; }

  /// <summary>HTTP method name (Get, Post, Put, Delete, Patch, Head, Options).</summary>
  public required string Verb { get; init; }
}
