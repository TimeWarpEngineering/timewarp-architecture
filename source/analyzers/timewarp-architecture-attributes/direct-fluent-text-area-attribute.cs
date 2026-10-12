#region Purpose
// Reasoned opt-out from TWA0032 (multi-line text goes through the shared TextArea) for a Razor component.
#endregion

#region Design
// TWA0032 requires every FluentTextArea in the SPA to render inside components/forms/TextArea.razor,
// which owns the shadow-root sizing that makes the box fill its field and resize. This attribute
// is the documented exception hatch for a component that genuinely needs the raw control. A
// non-empty reason is required so the skip is a stated decision; an empty or whitespace reason
// does not opt out (TWA0032 still fires; there is no second diagnostic id).
// Lives in TimeWarp.Architecture.Attributes (not the convention-analyzers assembly): convention
// analyzers match opt-out attributes BY SIMPLE NAME and must not ProjectReference this assembly.
#endregion

namespace TimeWarp.Architecture.Attributes;

/// <summary>
/// Declares that this Razor component deliberately renders a raw <c>FluentTextArea</c> instead of the shared
/// <c>TextArea</c>. Suppresses TWA0032 only when the constructor reason is non-empty and non-whitespace.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class DirectFluentTextAreaAttribute : Attribute
{
  /// <summary>Why this component renders FluentTextArea directly. Whitespace does not opt out of TWA0032.</summary>
  public string Reason { get; }

  /// <summary>Opts this component out of TWA0032 when <paramref name="reason"/> is non-empty.</summary>
  public DirectFluentTextAreaAttribute(string reason)
  {
    Reason = reason;
  }
}
