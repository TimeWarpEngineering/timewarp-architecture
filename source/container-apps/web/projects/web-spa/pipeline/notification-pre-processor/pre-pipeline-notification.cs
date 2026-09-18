#region Purpose
// Notification carrying a request about to be handled so features can observe it without coupling to the handler.
#endregion

#region Design
// Non-generic: TimeWarp.Mediator 14 generated Publish wiring cannot emit open-generic
// INotification types. Observers inspect Request.GetType() if they need the action type.
#endregion

namespace TimeWarp.Architecture.Pipeline.NotificationPreProcessor;

public class PrePipelineNotification : INotification
{
  public required object Request { get; init; }
}
