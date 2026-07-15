#region Purpose
// Marks a type as deliberately referencing another product slice, bound to a real target type.
#endregion

#region Design
// TWPA0009 flags product/platform/substrate code that reaches into a different product slice.
// This attribute is the reasoned opt-out for a single deliberate edge:
// - TargetType (typeof) identifies the foreign type; the analyzer maps it to that type's slice
//   (namespace under SliceRoot) and suppresses only references into that slice.
// - AllowMultiple = true so a type that needs several foreign slices lists one attribute each.
// - Reason documents why the coupling is intentional; empty/whitespace is rejected at construction.
// Prefer sharing via Components or contracts when the dependency is not truly slice-local.
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
