#region Purpose
// Notification carrying a completed request/response pair so features can react to other features' actions without coupling to their handlers.
#endregion

namespace TimeWarp.Architecture.Pipeline.NotificationPostProcessor;

public class PostPipelineNotification<TRequest, TResponse> : INotification
{
  public required TRequest Request { get; init; }
  public required TResponse Response { get; init; }
}
