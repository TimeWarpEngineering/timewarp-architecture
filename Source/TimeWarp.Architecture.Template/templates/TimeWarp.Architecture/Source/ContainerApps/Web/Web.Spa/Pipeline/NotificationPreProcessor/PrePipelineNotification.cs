namespace TimeWarp.Architecture.Pipeline.NotificationPreProcessor;

public class PrePipelineNotification<TRequest> : INotification
{
  public TRequest Request { get; set; }
}
