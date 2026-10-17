#region Purpose
// Canonical name and description of a transport-neutral operation, applied to the request class.
#endregion

#region Design
// Kind is absent on purpose: IQuery<T>, ICommand<T>, and IIdempotentCommand<T> already say it.
// Description is a named argument so the constructor stays the name alone.
// HTTP route, CLI pattern, and MCP tool name are bindings, filled by each host, not this attribute.
#endregion

namespace TimeWarp.Contracts;

/// <summary>
/// Canonical name and description of an operation. Apply to the request class (<c>Query</c> or <c>Command</c>).
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
}
