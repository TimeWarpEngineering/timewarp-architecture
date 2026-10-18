#region Purpose
// MCP binding of one operation: the tool name advertised by tools/list.
#endregion

namespace TimeWarp.Contracts;

/// <summary>MCP tool binding for one operation.</summary>
public sealed class McpOperationBinding
{
  /// <summary>Tool name advertised by MCP <c>tools/list</c>.</summary>
  public required string ToolName { get; init; }
}
