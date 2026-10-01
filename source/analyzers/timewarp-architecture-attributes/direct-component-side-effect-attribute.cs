#region Purpose
// Reasoned opt-out from TWA0026 (components only dispatch TimeWarp.State actions) for a component or member.
#endregion

#region Design
// TWA0026 flags navigation, JS interop, API/HttpClient calls, browser-storage writes and
// [SideEffectService] calls made directly from a ComponentBase member. This attribute is the
// documented exception hatch for side effects that are genuinely presentational (focus, scroll,
// hotkey registration) or that no action can express. A non-empty reason is required: an empty or
// whitespace reason does not opt out and is itself reported as TWA0027.
// Lives in TimeWarp.Architecture.Attributes (not the convention-analyzers assembly): convention
// analyzers match opt-out attributes BY SIMPLE NAME and must not ProjectReference this assembly.
#endregion

namespace TimeWarp.Architecture.Attributes;

/// <summary>
/// Declares that this component (or member) deliberately performs a side effect itself instead of
/// dispatching a TimeWarp.State action. Suppresses TWA0026 only when the reason is non-empty.
/// </summary>
[AttributeUsage
(
  AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Constructor,
  AllowMultiple = false,
  Inherited = false
)]
public sealed class DirectComponentSideEffectAttribute : Attribute
{
  /// <summary>Why the component performs the side effect directly. Empty or whitespace is reported as TWA0027 and does not opt out.</summary>
  public string Reason { get; }

  /// <summary>Opts this component or member out of TWA0026 when <paramref name="reason"/> is non-empty.</summary>
  public DirectComponentSideEffectAttribute(string reason)
  {
    Reason = reason;
  }
}
