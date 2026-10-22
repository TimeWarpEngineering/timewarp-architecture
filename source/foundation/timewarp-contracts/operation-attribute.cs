#region Purpose
// Canonical name, description, purpose group, and title of a transport-neutral operation.
#endregion

#region Design
// Kind is absent on purpose: IQuery<T>, ICommand<T>, and IIdempotentCommand<T> already say it.
// Description is a named argument so the constructor stays the name alone.
// Group is purpose metadata for every transport. Null or whitespace means ungrouped, the same
// IsNullOrWhiteSpace rule as the name. Hosts read it from the catalog. It never prefixes a
// route pattern or a command name, so the CLI command stays the operation name.
// Title is the human-readable name. Null or whitespace means no title, the same blank rule.
// Destructive and open-world stay off this attribute. A generator can see an omitted named
// bool, but reflection reads it as false, so those two are DestructiveAttribute and
// OpenWorldAttribute, where presence means stated for every reader.
// Hosts copy Title and those attributes onto OperationDescriptor. They are not binding fields.
// HTTP route, CLI pattern, and MCP tool name are bindings, filled by each host, not this attribute.
#endregion

namespace TimeWarp.Contracts;

/// <summary>
/// Canonical name, description, purpose group, and title of an operation.
/// Apply to the request class (<c>Query</c> or <c>Command</c>).
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class OperationAttribute : Attribute
{
  /// <summary>
  /// Creates an operation marker with the canonical <paramref name="name"/>.
  /// </summary>
  /// <param name="name">Canonical operation name shared by every transport.</param>
  /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or whitespace.</exception>
  public OperationAttribute(string name)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(name);
    Name = name;
  }

  /// <summary>Canonical operation name shared by HTTP, CLI, and MCP.</summary>
  public string Name { get; }

  /// <summary>Operation summary hosts show in capabilities, OpenAPI, and MCP tool lists.</summary>
  public string? Description { get; set; }

  /// <summary>
  /// Purpose group shared by every transport. Segments are separated by <c>.</c> or space.
  /// Null or whitespace means ungrouped. Hosts read this as metadata and do not prefix the
  /// route pattern or the command name with it.
  /// </summary>
  public string? Group
  {
    get => field;
    set => field = string.IsNullOrWhiteSpace(value) ? null : value;
  }

  /// <summary>
  /// Human-readable title. Null or whitespace means the operation has no title.
  /// Hosts project it to MCP <c>title</c>. It is not the canonical name and not <see cref="Description"/>.
  /// </summary>
  public string? Title
  {
    get => field;
    set => field = string.IsNullOrWhiteSpace(value) ? null : value;
  }
}
