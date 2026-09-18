#region Purpose
// Publishes a PrePipelineNotification for every TimeWarp.State action before its handler runs.
#endregion

#region Design
// Bridges the mediator pipeline to pub/sub so observers can react to any action without each
// handler opting in. Constrained to IAction. IPipelineBehavior (not IRequestPreProcessor) so
// the generated mediator can weave it via [assembly: MediatorBehavior]. Parameter must be
// named next. Public because generated closed types reference this type by name.
#endregion

namespace TimeWarp.Architecture.Pipeline.NotificationPreProcessor;

public class PrePipelineNotificationRequestPreProcessor<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
  where TRequest : notnull, IAction
{
  private readonly ILogger Logger;
  private readonly IPublisher<ClientPipeline> Publisher;

  public PrePipelineNotificationRequestPreProcessor
  (
    ILogger<PrePipelineNotificationRequestPreProcessor<TRequest, TResponse>> logger,
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
    var notification = new PrePipelineNotification
    {
      Request = request,
    };

    Logger.LogDebug("PrePipelineNotificationRequestPreProcessor");
    await Publisher.Publish(notification, cancellationToken);
    return await next(cancellationToken);
  }
}
