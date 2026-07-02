#region Purpose
// Example of reacting to a completed state action via a post-pipeline notification.
#endregion

#region Design
// Logs only — its value is the shape: subscribe to PostPipelineNotification<TAction,
// TResponse> to run cross-cutting work after an action finishes, without coupling to
// or modifying the action's handler. Copy this pattern for real side effects.
#endregion

namespace TimeWarp.Architecture.Features.Counters;

using static CounterState;

internal class IncrementCountNotificationHandler
  : INotificationHandler<PostPipelineNotification<IncrementCounterActionSet.Action, Unit>>
{
  private readonly ILogger Logger;

  public IncrementCountNotificationHandler(ILogger<IncrementCountNotificationHandler> logger)
  {
    Logger = logger;
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
