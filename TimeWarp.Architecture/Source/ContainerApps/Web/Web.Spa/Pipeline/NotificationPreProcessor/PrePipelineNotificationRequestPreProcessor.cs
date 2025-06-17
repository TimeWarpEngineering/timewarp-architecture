namespace TimeWarp.Architecture.Pipeline.NotificationPreProcessor;

internal class PrePipelineNotificationRequestPreProcessor<TRequest> : IRequestPreProcessor<TRequest> where TRequest : IAction
{
  private readonly ILogger Logger;

  private readonly IPublisher Publisher;

  public PrePipelineNotificationRequestPreProcessor
  (
    ILogger<PrePipelineNotificationRequestPreProcessor<TRequest>> aLogger,
    IPublisher aPublisher
  )
  {
    Logger = aLogger;
    Publisher = aPublisher;
  }

  public Task Process(TRequest aRequest, CancellationToken aCancellationToken)
  {
    var notification = new PrePipelineNotification<TRequest>
    {
      Request = aRequest,
    };

    Logger.LogDebug("PrePipelineNotificationRequestPreProcessor");
    return Publisher.Publish(notification, aCancellationToken);
  }
}
