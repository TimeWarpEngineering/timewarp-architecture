namespace TimeWarp.Architecture.Pipeline.NotificationPostProcessor;

internal class PostPipelineNotificationRequestPostProcessor<TRequest, TResponse> : IRequestPostProcessor<TRequest, TResponse>
    where TRequest : notnull
{
  private readonly ILogger Logger;

  private readonly IPublisher Publisher;

  public PostPipelineNotificationRequestPostProcessor
  (
    ILogger<PostPipelineNotificationRequestPostProcessor<TRequest, TResponse>> aLogger,
    IPublisher aPublisher
  )
  {
    Logger = aLogger;
    Publisher = aPublisher;
  }

  public Task Process(TRequest aRequest, TResponse aResponse, CancellationToken aCancellationToken)
  {
    var notification = new PostPipelineNotification<TRequest, TResponse>
    {
      Request = aRequest,
      Response = aResponse
    };

    Logger.LogDebug("PostPipelineNotificationRequestPostProcessor");
    return Publisher.Publish(notification, aCancellationToken);
  }
}
