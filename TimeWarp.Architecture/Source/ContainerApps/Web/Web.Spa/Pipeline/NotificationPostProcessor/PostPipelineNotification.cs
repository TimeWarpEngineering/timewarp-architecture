namespace TimeWarp.Architecture.Pipeline.NotificationPostProcessor;

public class PostPipelineNotification<TRequest, TResponse> : INotification
{
  public required TRequest Request { get; init; }
  public required TResponse Response { get; init; }
}
