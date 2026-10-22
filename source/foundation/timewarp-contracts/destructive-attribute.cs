#region Purpose
// Tri-state destructive hint on an operation request. Absence is unset, not false.
#endregion

#region Design
// Attribute arguments cannot be bool?, and a bool property would default to false, so an
// omitted hint and an explicit false would be the same value. Presence of this attribute is
// the unset test every reader can make: a generator matches
// TimeWarp.Contracts.DestructiveAttribute by metadata name, and reflection finds the attribute
// or finds nothing. The constructor argument defaults to true, so [Destructive] and
// [Destructive(true)] are the same claim. [Destructive(false)] is the explicit false.
// readOnlyHint and idempotentHint stay on the mediator kind. Hosts copy IsDestructive onto
// OperationDescriptor.DestructiveHint and leave that null when this attribute is absent.
#endregion

namespace TimeWarp.Contracts;

/// <summary>
/// States whether an operation may delete or overwrite data that cannot be recovered.
/// </summary>
/// <remarks>
/// <para>
/// A request without this attribute leaves <see cref="OperationDescriptor.DestructiveHint"/> unset.
/// That is not <see langword="false"/>. <c>[Destructive]</c> and <c>[Destructive(true)]</c> are true.
/// <c>[Destructive(false)]</c> states that the operation only adds data.
/// </para>
/// <para>
/// <c>readOnlyHint</c> and <c>idempotentHint</c> come from the mediator kind, not from this attribute.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class DestructiveAttribute : Attribute
{
  /// <summary>Creates a destructive hint. The default is true.</summary>
  /// <param name="isDestructive">False to state that the operation is not destructive.</param>
  public DestructiveAttribute(bool isDestructive = true)
  {
    IsDestructive = isDestructive;
  }

  /// <summary>True when the operation may delete or overwrite data.</summary>
  public bool IsDestructive { get; }
}
