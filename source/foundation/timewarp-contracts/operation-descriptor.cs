#region Purpose
// One catalog entry: canonical name, purpose group, tool hints, kind, members, success type, and bindings.
#endregion

#region Design
// ReturnType is the success type's display name. Hosts that return OneOf of that type and
// SharedProblemDetails record the success type; the problem type is shared and is not repeated
// on each entry. Bindings are optional and are filled by the host that owns each transport.
// Group is copied from OperationAttribute.Group. Null means ungrouped. It is metadata: a host
// does not prefix OperationDescriptor.Name, an HTTP route, or a CLI pattern with it.
// Title, DestructiveHint, and OpenWorldHint are copied from the request: Title from
// OperationAttribute.Title, the two bools from DestructiveAttribute and OpenWorldAttribute.
// Null means the request did not state that hint. False is a stated false, so hosts omit null
// and still write false. The hints sit on the operation, not on McpOperationBinding, because a
// CLI catalog projects them when the MCP binding is null. They never change the route or the tool name.
#endregion

namespace TimeWarp.Contracts;

/// <summary>One operation in an <see cref="OperationCatalog"/>.</summary>
public sealed class OperationDescriptor
{
  /// <summary>Canonical name from <see cref="OperationAttribute"/>.</summary>
  public required string Name { get; init; }

  /// <summary>Operation summary from <see cref="OperationAttribute.Description"/>.</summary>
  public string? Description { get; init; }

  /// <summary>
  /// Purpose group from <see cref="OperationAttribute.Group"/>. Null when the operation is ungrouped.
  /// Metadata only: hosts do not prefix the route or the command name with this value.
  /// </summary>
  public string? Group { get; init; }

  /// <summary>
  /// Human-readable title from <see cref="OperationAttribute.Title"/>. Null when the operation has no title.
  /// </summary>
  public string? Title { get; init; }

  /// <summary>
  /// MCP <c>destructiveHint</c> from <see cref="DestructiveAttribute"/>.
  /// Null when the request has no <see cref="DestructiveAttribute"/>.
  /// False means the operation only adds data.
  /// MCP clients treat an omitted hint as true and read it only when the tool is not read-only.
  /// </summary>
  public bool? DestructiveHint { get; init; }

  /// <summary>
  /// MCP <c>openWorldHint</c> from <see cref="OpenWorldAttribute"/>.
  /// Null when the request has no <see cref="OpenWorldAttribute"/>.
  /// False is a stated claim that the operation only touches local state.
  /// </summary>
  public bool? OpenWorldHint { get; init; }

  /// <summary>Kind derived from the request's mediator interface.</summary>
  public required OperationKind Kind { get; init; }

  /// <summary>Derived request members. Empty when the request has no public properties.</summary>
  public required IReadOnlyList<OperationMember> Members { get; init; }

  /// <summary>
  /// Display name of the success type. For <c>OneOf&lt;TSuccess, SharedProblemDetails&gt;</c> this is <c>TSuccess</c>.
  /// </summary>
  public required string ReturnType { get; init; }

  /// <summary>Transports that expose this operation.</summary>
  public OperationBindings? Bindings { get; init; }
}
