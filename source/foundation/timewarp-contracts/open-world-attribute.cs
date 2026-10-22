#region Purpose
// Tri-state open-world hint on an operation request. Absence is unset, not false.
#endregion

#region Design
// Same tri-state encoding as DestructiveAttribute: presence is the unset test for generator
// and reflection readers alike. [OpenWorld] and [OpenWorld(true)] mean the operation
// reaches outside the app. [OpenWorld(false)] means it only touches local state. A generator
// matches TimeWarp.Contracts.OpenWorldAttribute by metadata name. The MCP specification tells
// clients to treat an omitted openWorldHint as true, so the catalog stores null and lets the
// projecting host omit the field. This package does not substitute that client default.
#endregion

namespace TimeWarp.Contracts;

/// <summary>
/// States whether an operation reaches outside the app: the web, a remote service, or other people's data.
/// </summary>
/// <remarks>
/// A request without this attribute leaves <see cref="OperationDescriptor.OpenWorldHint"/> unset.
/// Clients that follow the MCP specification treat an omitted <c>openWorldHint</c> as true.
/// <c>[OpenWorld]</c> and <c>[OpenWorld(true)]</c> state that the operation reaches outside the app.
/// <c>[OpenWorld(false)]</c> states that it only reads or changes local state.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class OpenWorldAttribute : Attribute
{
  /// <summary>Creates an open-world hint. The default is true.</summary>
  /// <param name="isOpenWorld">False for an operation that only touches local state.</param>
  public OpenWorldAttribute(bool isOpenWorld = true)
  {
    IsOpenWorld = isOpenWorld;
  }

  /// <summary>True when the operation reaches outside the app.</summary>
  public bool IsOpenWorld { get; }
}
