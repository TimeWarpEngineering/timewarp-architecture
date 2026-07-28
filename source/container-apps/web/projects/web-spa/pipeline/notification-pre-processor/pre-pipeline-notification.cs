#region Purpose
// Notification carrying a request about to be handled so features can observe or react before the handler runs, without coupling to it.
#endregion

namespace TimeWarp.Architecture.Pipeline.NotificationPreProcessor;

public class PrePipelineNotification<TRequest> : INotification
{
  public required TRequest Request { get; init; }
}
