namespace TimeWarp.Architecture.Features.Notifications.Spa;

[StateAccessMixin]
internal partial class NotificationState : State<NotificationState>
{
  private List<Notification> _Notifications;

  public IReadOnlyList<Notification> Notifications => _Notifications.AsReadOnly();

  public NotificationState()
  {
    Initialize();
  }

  public override void Initialize() => _Notifications = new List<Notification>();

  public class Notification
  {
    public string Message { get; init; }

    public string Title { get; init; }
    public enum NotificationType
    {
      Success,
      Error,
      Warning,
      Information
    }
    public NotificationType Type { get; init; }
    public Guid Id { get; set; }
  }
}

