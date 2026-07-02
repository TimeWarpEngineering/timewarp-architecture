#region Purpose
// Publishes a PrePipelineNotification for every TimeWarp.State action before its handler runs.
#endregion

#region Design
// Bridges the mediator pre-processor stage to pub/sub notifications so cross-cutting observers
// (event stream, dev tooling, diagnostics) can react to any action without each handler opting in.
// Constrained to IAction: plain mediator requests bypass this hook, keeping the fan-out limited to
// state actions where such observation is meaningful.
#endregion

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
