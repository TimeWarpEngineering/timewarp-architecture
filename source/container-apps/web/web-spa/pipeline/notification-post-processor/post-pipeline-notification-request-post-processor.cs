namespace TimeWarp.Architecture.Pipeline.NotificationPostProcessor;

internal class PostPipelineNotificationRequestPostProcessor<TRequest, TResponse> : IRequestPostProcessor<TRequest, TResponse>
    where TRequest : notnull
{
  private readonly ILogger Logger;

  private readonly IPublisher Publisher;

  public PostPipelineNotificationRequestPostProcessor
  (
    ILogger<PostPipelineNotificationRequestPostProcessor<TRequest, TResponse>> logger,
    IPublisher publisher
  )
  {
    Logger = logger;
    Publisher = publisher;
  }

  public Task Process(TRequest request, TResponse response, CancellationToken cancellationToken)
  {
    var notification = new PostPipelineNotification<TRequest, TResponse>
    {
      Request = request,
      Response = response
    };

    Logger.LogDebug("PostPipelineNotificationRequestPostProcessor");
    return Publisher.Publish(notification, cancellationToken);
  }
}
