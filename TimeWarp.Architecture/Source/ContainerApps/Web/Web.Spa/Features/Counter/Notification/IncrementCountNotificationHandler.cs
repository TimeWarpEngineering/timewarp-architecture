namespace TimeWarp.Architecture.Features.Counters;

using static CounterState;

internal class IncrementCountNotificationHandler
  : INotificationHandler<PostPipelineNotification<IncrementCounterActionSet.Action, Unit>>
{
  private readonly ILogger Logger;

  public IncrementCountNotificationHandler(ILogger<IncrementCountNotificationHandler> aLogger)
  {
    Logger = aLogger;
  }

  public Task Handle
  (
    PostPipelineNotification<IncrementCounterActionSet.Action, Unit> postPipelineNotification,
    CancellationToken cancellationToken
  )
  {
    Logger.LogDebug(postPipelineNotification.Request.GetType().Name);
    Logger.LogDebug($"{nameof(IncrementCountNotificationHandler)} handled");
    return Unit.Task;
  }
}
