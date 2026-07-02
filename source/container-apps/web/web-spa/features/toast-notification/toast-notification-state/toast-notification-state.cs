#region Purpose
// Root partial for ToastNotificationState: a deliberately empty state anchoring toast actions.
#endregion

#region Design
// FluentUI's toast service owns display and lifetime, so this state stores nothing; it exists
// so toast operations run as dispatched actions (pipeline logging, DevTools) and so the feature
// follows the same state-per-feature layout as every other feature.
// Empty Initialize is intentional — there is no data to reset.
#endregion

namespace TimeWarp.Architecture.Features.ToastNotifications;

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
