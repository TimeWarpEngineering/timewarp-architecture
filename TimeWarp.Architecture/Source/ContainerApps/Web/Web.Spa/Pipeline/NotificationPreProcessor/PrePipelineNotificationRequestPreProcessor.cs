namespace TimeWarp.Architecture.Pipeline.NotificationPreProcessor;

internal class PrePipelineNotificationRequestPreProcessor<TRequest> : IRequestPreProcessor<TRequest> where TRequest : IAction
{
  private readonly ILogger Logger;

  private readonly IPublisher Publisher;

  public PrePipelineNotificationRequestPreProcessor
  (
    ILogger<PrePipelineNotificationRequestPreProcessor<TRequest>> logger,
    IPublisher publisher
  )
  {
    Logger = logger;
    Publisher = publisher;
  }

  public Task Process(TRequest request, CancellationToken cancellationToken)
  {
    var notification = new PrePipelineNotification<TRequest>
    {
      Request = request,
    };

    Logger.LogDebug("PrePipelineNotificationRequestPreProcessor");
    return Publisher.Publish(notification, cancellationToken);
  }
}
