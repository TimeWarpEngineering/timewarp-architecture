#region Purpose
// Root partial of the shared component base: TimeWarp.State access.
#endregion

#region Design
// Inherits TimeWarpStateComponent (Redux DevTools and its dev component were removed in
// TimeWarp.State 12.0.0-beta.11; observe state through the telemetry package instead).
// The class is split into partials by concern (auth, css, state accessors) so each aspect can
// be read and maintained in isolation; this file carries only cross-cutting members.
// No Send wrapper: components dispatch through the generated ActionSet methods, which wire
// cancellation tokens automatically. TWA0022 enforces that — a wrapper could not, because the
// inherited protected Mediator member stays reachable regardless.
#endregion

namespace TimeWarp.Architecture.Features;

/// <summary>
/// Makes access to the State a little easier by inheriting from TimeWarpStateComponent.
/// </summary>
/// <remarks>
/// One is not required to use this base component, but would then have to implement the
/// required interfaces.
/// </remarks>
public abstract partial class BaseComponent : TimeWarpStateComponent, IAttributeComponent
{
  protected bool IsAnyActive(params Type[] actions) => ActionTrackingState.IsAnyActive(actions);
}
