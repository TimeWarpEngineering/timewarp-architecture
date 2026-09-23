#region Purpose
// Reasoned opt-out from TWA0025 (page-local message bar convention) for a Razor component.
#endregion

#region Design
// TWA0025 requires operation-outcome message bars (Error/Success intent) to be rendered only
// in the shell's MessageBars.razor host, fed by NotificationState. This attribute is the
// documented exception hatch: a non-empty reason is required so the skip is a stated decision,
// not a silent override. Empty or whitespace reason does not opt out — TWA0025 still fires;
// there is no second diagnostic id.
// Lives in TimeWarp.Architecture.Attributes (not the convention-analyzers assembly): convention-
// analyzers match opt-out attributes BY SIMPLE NAME and must not ProjectReference this assembly.
// Do not wire this package repo-wide; consumers reference it where they opt out (typically
// pages that are genuinely hosting their own outcome display, not the usual case).
#endregion

namespace TimeWarp.Architecture.Attributes;

/// <summary>
/// Declares that this Razor component deliberately renders its own operation-outcome message bars.
/// Suppresses TWA0025 only when the constructor reason is non-empty and non-whitespace.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class PageLocalMessageBarAttribute : Attribute
{
  /// <summary>Why this component renders its own outcome message bars instead of dispatching through NotificationState. Whitespace does not opt out of TWA0025.</summary>
  public string Reason { get; }

  /// <summary>Opts this component out of TWA0025 when <paramref name="reason"/> is non-empty.</summary>
  public PageLocalMessageBarAttribute(string reason)
  {
    Reason = reason;
  }
}
