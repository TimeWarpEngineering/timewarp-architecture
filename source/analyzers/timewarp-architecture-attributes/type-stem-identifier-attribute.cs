#region Purpose
// Reasoned opt-out from TWA0023 (type-stem identifier convention) for a field, property, or parameter.
#endregion

#region Design
// TWA0023 requires the identifier to end with the declared type's stem (interfaces drop a leading
// I). This attribute is the documented exception hatch: a non-empty reason is required so the
// skip is a stated decision, not a silent rename. Empty or whitespace reason does not opt out —
// TWA0023 still fires; there is no second diagnostic id.
// Vendor-prefix clipping (TimeWarpTerminal → Terminal) is attribute-only; the analyzer never
// infers it. Locals, foreach, and catch variables have no AttributeTargets.Local — their hatch
// is #pragma warning disable TWA0023 or an editorconfig exclusion.
// Lives in TimeWarp.Architecture.Attributes (not Foundation): convention-analyzers match by
// simple name and must not ProjectReference this assembly. Do not wire this package repo-wide;
// consumers reference it where they opt out.
#endregion

namespace TimeWarp.Architecture.Attributes;

/// <summary>
/// Declares that this field, property, or parameter deliberately does not end with its type stem.
/// Suppresses TWA0023 only when the constructor reason is non-empty and non-whitespace.
/// </summary>
[AttributeUsage(
  AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter,
  AllowMultiple = false,
  Inherited = false)]
public sealed class TypeStemIdentifierAttribute : Attribute
{
  public string Reason { get; }

  public TypeStemIdentifierAttribute(string reason)
  {
    Reason = reason;
  }
}
