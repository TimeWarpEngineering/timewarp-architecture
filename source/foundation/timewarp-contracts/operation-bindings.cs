#region Purpose
// Transports that expose one operation. Each host fills the binding it owns.
#endregion

#region Design
// A null binding means that transport does not expose the operation.
// HTTP, CLI, and MCP stay siblings so a projection can read one without the others.
#endregion

namespace TimeWarp.Contracts;

/// <summary>Transport bindings for one operation. A null property means that transport is unbound.</summary>
public sealed class OperationBindings
{
  /// <summary>HTTP route and verb.</summary>
  public HttpOperationBinding? Http { get; init; }

  /// <summary>CLI route pattern.</summary>
  public CliOperationBinding? Cli { get; init; }

  /// <summary>MCP tool name.</summary>
  public McpOperationBinding? Mcp { get; init; }
}
