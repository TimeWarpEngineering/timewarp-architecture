#region Purpose
// Root partial of the shared component base: TimeWarp.State access plus Redux DevTools support.
#endregion

#region Design
// Inherits the DevTools-enabled state component so the template demonstrates time-travel
// debugging out of the box; a production app would swap to the plain base (see remarks).
// The class is split into partials by concern (auth, css, state accessors) so each aspect can
// be read and maintained in isolation; this file carries only cross-cutting members.
// No Send wrapper: components dispatch through the generated ActionSet methods, which wire
// cancellation tokens automatically. TWA0022 enforces that — a wrapper could not, because the
// inherited protected Mediator member stays reachable regardless.
#endregion

namespace TimeWarp.Architecture.Features;

/// <summary>
/// Makes access to the State a little easier and by inheriting from
/// TimeWarpStateDevToolsComponent it allows for ReduxDevTools operation.
/// </summary>
/// <remarks>
/// In production one would NOT be required to use these base components
/// But would be required to properly implement the required interfaces.
/// one could conditionally inherit from BaseComponent for production build.
/// </remarks>
public abstract partial class BaseComponent : TimeWarpStateDevComponent, IAttributeComponent
{
    protected bool IsAnyActive(params Type[] actions) => ActionTrackingState.IsAnyActive(actions);
}
