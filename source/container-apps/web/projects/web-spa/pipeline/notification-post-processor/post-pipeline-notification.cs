#region Purpose
// Notification carrying a completed request/response pair so features can react without coupling to handlers.
#endregion

#region Design
// Non-generic: TimeWarp.Mediator 14 generated Publish wiring cannot emit open-generic
// INotification types. Observers inspect Request.GetType() if they need the action type.
#endregion

namespace TimeWarp.Architecture.Pipeline.NotificationPostProcessor;

public class PostPipelineNotification : INotification
{
  public required object Request { get; init; }
  public required object? Response { get; init; }
}
