namespace TimeWarp.Architecture.Features.Counters.Spa;

using static TimeWarp.Architecture.Features.Counters.CounterState;

internal class IncrementCountNotificationHandler
  : INotificationHandler<PostPipelineNotification<IncrementCounterAction, Unit>>
{
  private readonly ILogger Logger;

  public IncrementCountNotificationHandler(ILogger<IncrementCountNotificationHandler> aLogger)
  {
    Logger = aLogger;
  }

  public Task Handle
  (
    PostPipelineNotification<IncrementCounterAction, Unit> aPostPipelineNotification,
    CancellationToken aCancellationToken
  )
  {
    Logger.LogDebug(aPostPipelineNotification.Request.GetType().Name);
    Logger.LogDebug($"{nameof(IncrementCountNotificationHandler)} handled");
    return Unit.Task;
  }
}
