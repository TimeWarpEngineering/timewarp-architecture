#region Purpose
// MCP binding of one operation: the tool name advertised by tools/list.
#endregion

#region Design
// Title, destructiveHint, and openWorldHint are operation facts. They live on OperationDescriptor,
// copied from the request. This binding stays the tool name the host fills. A null MCP binding
// means MCP does not expose the operation; the hints remain on the operation for a CLI catalog.
#endregion

namespace TimeWarp.Contracts;

/// <summary>MCP tool binding for one operation.</summary>
public sealed class McpOperationBinding
{
  /// <summary>Tool name advertised by MCP <c>tools/list</c>.</summary>
  public required string ToolName { get; init; }
}
