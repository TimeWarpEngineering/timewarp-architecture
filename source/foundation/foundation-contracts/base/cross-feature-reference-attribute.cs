#region Purpose
// Marks a type as deliberately referencing another feature's namespace (demo/aggregation code).
#endregion

#region Design
// The feature-isolation analyzer (TWPA0009) flags feature-folder code that references a namespace
// owned by a different feature — this attribute is the explicit, reasoned opt-out for the rare
// legitimate cross-feature reference (e.g. the Style Guide page deliberately exercising other
// features' pipelines). Reason is required: an unexplained opt-out is just coupling with paperwork.
// Shared code belongs in components/ or contracts, not in an opt-out — reach for those first.
#endregion

namespace TimeWarp.Foundation.Features;

/// <summary>
/// Declares that this type intentionally references another feature's namespace.
/// Suppresses TWPA0009 for references within the type.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class CrossFeatureReferenceAttribute : Attribute
{
  public string Reason { get; }

  public CrossFeatureReferenceAttribute(string reason)
  {
    Reason = reason;
  }
}
