#region Purpose
// One catalog entry: canonical name, purpose group, kind, members, success type, and bindings.
#endregion

#region Design
// ReturnType is the success type's display name. Hosts that return OneOf of that type and
// SharedProblemDetails record the success type; the problem type is shared and is not repeated
// on each entry. Bindings are optional and are filled by the host that owns each transport.
// Group is copied from OperationAttribute.Group. Null means ungrouped. It is metadata: a host
// does not prefix OperationDescriptor.Name, an HTTP route, or a CLI pattern with it.
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
