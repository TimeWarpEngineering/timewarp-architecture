#region Purpose
// Republishes every completed request/response pair as a PostPipelineNotification.
#endregion

#region Design
// Fan-out point for decoupled observation: a feature reacts to another feature's action by
// handling PostPipelineNotification<TRequest,TResponse> (e.g. the counter's increment
// notification handler) without referencing the originating handler.
// Registered as an open generic so it fires for all requests; subscribers select what they
// care about by closing the notification's type parameters.
#endregion

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
