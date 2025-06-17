namespace TimeWarp.Architecture.Features.Notifications;

[StateAccessMixin]
public sealed partial class NotificationState : State<NotificationState>
{
  private List<Notification>? NotificationList = [];
  public IReadOnlyList<Notification>? Notifications => NotificationList?.AsReadOnly();

  public NotificationState()
  {
    Initialize();
  }

  public sealed override void Initialize() => NotificationList = null;

  public class Notification
  {
    public required string Message { get; init; }

    public required string Title { get; init; }
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

