#region Purpose
// Catalog document whose projections are --capabilities, OpenAPI, and MCP tools/list.
#endregion

#region Design
// Generalizes a CLI capabilities document: the same entries project to HTTP, CLI, and MCP.
// Name, version, and description identify the catalog (the tool or app), not one operation.
// Serialization policy (camelCase, null omission) belongs to the projecting host.
#endregion

namespace TimeWarp.Contracts;

/// <summary>
/// Transport-neutral catalog of operations. Hosts project it to <c>--capabilities</c>, OpenAPI, and MCP <c>tools/list</c>.
/// </summary>
public sealed class OperationCatalog
{
  /// <summary>Catalog name, typically the tool or application name.</summary>
  public string? Name { get; init; }

  /// <summary>Catalog version.</summary>
  public string? Version { get; init; }

  /// <summary>Catalog summary.</summary>
  public string? Description { get; init; }

  /// <summary>Operations in this catalog.</summary>
  public required IReadOnlyList<OperationDescriptor> Operations { get; init; }
}
