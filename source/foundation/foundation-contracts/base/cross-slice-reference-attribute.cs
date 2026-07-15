#region Purpose
// Marks a type as deliberately referencing another product slice, bound to a real target type.
#endregion

#region Design
// TWPA0009 flags product/platform/substrate code that reaches into a different product slice.
// This attribute is the explicit, reasoned opt-out: TargetType is compile-checked and scopes
// suppression to edges into that type's slice only (AllowMultiple for multiple edges). Free-form
// feature/folder name mutes are rejected — the namespace tree is the catalog, and the typeof
// argument is the edge. Reason is required human paperwork; empty reasons are not coupling
// with a blank form. Shared code belongs in Components or contracts first.
#endregion

namespace TimeWarp.Foundation.Features;

/// <summary>
/// Declares that this type intentionally references types owned by the same product slice as
/// <paramref name="targetType"/>. Suppresses TWPA0009 only for edges into that target slice.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class CrossSliceReferenceAttribute : Attribute
{
  public Type TargetType { get; }
  public string Reason { get; }

  public CrossSliceReferenceAttribute(Type targetType, string reason)
  {
    TargetType = Guard.Against.Null(targetType);
    Reason = Guard.Against.NullOrWhiteSpace(reason);
  }
}
