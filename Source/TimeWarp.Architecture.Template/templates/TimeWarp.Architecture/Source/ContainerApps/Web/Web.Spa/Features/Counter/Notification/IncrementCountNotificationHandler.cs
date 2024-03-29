namespace TimeWarp.Architecture.Features.Counters;

using static TimeWarp.Architecture.Features.Counters.CounterState;

internal class IncrementCountNotificationHandler
  : INotificationHandler<PostPipelineNotification<IncrementCounter.Action, Unit>>
{
  private readonly ILogger Logger;

  public IncrementCountNotificationHandler(ILogger<IncrementCountNotificationHandler> aLogger)
  {
    Logger = aLogger;
  }

  public Task Handle
  (
    PostPipelineNotification<IncrementCounter.Action, Unit> postPipelineNotification,
    CancellationToken cancellationToken
  )
  {
    Logger.LogDebug(postPipelineNotification.Request.GetType().Name);
    Logger.LogDebug($"{nameof(IncrementCountNotificationHandler)} handled");
    return Unit.Task;
  }
}
