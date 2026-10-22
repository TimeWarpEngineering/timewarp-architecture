#region Purpose
// Tri-state destructive hint on an operation request. Absence is unset, not false.
#endregion

#region Design
// A bool named argument on [Operation] would be tri-state for a generator, which sees
// whether the argument was written, but reflection fills an omitted property with its
// default, so a runtime reader would see false for a hint nobody stated. Presence of this
// attribute is the unset test both readers can make: a generator matches
// TimeWarp.Contracts.DestructiveAttribute by metadata name, and reflection finds the attribute
// or finds nothing. The constructor argument defaults to true, so [Destructive] and
// [Destructive(true)] are the same claim. [Destructive(false)] means the operation only adds
// data: it does not delete or overwrite.
// MCP clients treat an omitted destructiveHint as true, and they read it only when the tool
// is not read-only. This package stores null and does not substitute that client default.
// readOnlyHint and idempotentHint stay on the mediator kind. Hosts copy IsDestructive onto
// OperationDescriptor.DestructiveHint and leave that null when this attribute is absent.
#endregion

namespace TimeWarp.Contracts;

/// <summary>
/// States whether an operation may perform destructive updates (delete or overwrite) rather than only additive ones.
/// </summary>
/// <remarks>
/// <para>
/// A request without this attribute leaves <see cref="OperationDescriptor.DestructiveHint"/> unset.
/// That is not <see langword="false"/>. <c>[Destructive]</c> and <c>[Destructive(true)]</c> are true.
/// <c>[Destructive(false)]</c> states that the operation only adds data.
/// </para>
/// <para>
/// MCP clients treat an omitted <c>destructiveHint</c> as true. They read the hint only when the tool
/// is not read-only, so it does not apply to an <c>IQuery</c>. This package stores null and does not
/// substitute that client default.
/// </para>
/// <para>
/// <c>readOnlyHint</c> and <c>idempotentHint</c> come from the mediator kind, not from this attribute.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class DestructiveAttribute : Attribute
{
  /// <summary>Creates a destructive hint. The default is true.</summary>
  /// <param name="isDestructive">False to state that the operation only adds data.</param>
  public DestructiveAttribute(bool isDestructive = true)
  {
    IsDestructive = isDestructive;
  }

  /// <summary>True when the operation may delete or overwrite, rather than only add.</summary>
  public bool IsDestructive { get; }
}
