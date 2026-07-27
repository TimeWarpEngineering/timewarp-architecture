#region Purpose
// Root partial for ToastNotificationState: a deliberately empty state anchoring toast actions.
#endregion

#region Design
// FluentUI's toast service owns display and lifetime, so this state stores nothing; it exists
// so toast operations run as dispatched actions (pipeline logging, DevTools).
// Namespace is Features substrate (not a product slice): DefaultApiHandler and other base
// handlers toast failures, so every product may depend on it without TWA0009 opt-outs.
// Empty Initialize is intentional — there is no data to reset.
#endregion

namespace TimeWarp.Architecture.Features;

[StateAccess]
public sealed partial class ToastNotificationState : State<ToastNotificationState>
{
  // Currently we use the FluentUI ToastService to manage all the state and display of toast notifications.
  // This state is here to provide a place to store notifications if we need to.
  // and to maintain a consistent pattern.

  public ToastNotificationState()
  {
    Initialize();
  }

  public sealed override void Initialize() {}
};
