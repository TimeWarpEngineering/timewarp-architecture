#region Purpose
// Republishes every completed request/response pair as a PostPipelineNotification.
#endregion

#region Design
// Fan-out for decoupled observation after an action finishes. IPipelineBehavior so the
// generated mediator weaves it via [assembly: MediatorBehavior]. Parameter must be named
// next. Public because generated closed types reference this type by name.
#endregion

namespace TimeWarp.Architecture.Pipeline.NotificationPostProcessor;

public class PostPipelineNotificationRequestPostProcessor<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
  where TRequest : notnull
{
  private readonly ILogger Logger;
  private readonly IPublisher<ClientPipeline> Publisher;

  public PostPipelineNotificationRequestPostProcessor
  (
    ILogger<PostPipelineNotificationRequestPostProcessor<TRequest, TResponse>> logger,
    IPublisher<ClientPipeline> publisher
  )
  {
    Logger = logger;
    Publisher = publisher;
  }

  public async Task<TResponse> Handle
  (
    TRequest request,
    RequestHandlerDelegate<TResponse> next,
    CancellationToken cancellationToken
  )
  {
    TResponse response = await next(cancellationToken);

    var notification = new PostPipelineNotification
    {
      Request = request,
      Response = response
    };

    Logger.LogDebug("PostPipelineNotificationRequestPostProcessor");
    await Publisher.Publish(notification, cancellationToken);
    return response;
  }
}
