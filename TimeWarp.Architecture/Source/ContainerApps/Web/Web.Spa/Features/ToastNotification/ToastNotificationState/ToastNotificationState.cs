namespace TimeWarp.Architecture.Features.ToastNotifications;

[StateAccessMixin]
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
