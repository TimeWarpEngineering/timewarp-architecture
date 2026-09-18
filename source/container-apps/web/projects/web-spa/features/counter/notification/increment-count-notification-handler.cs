#region Purpose
// Example of reacting to a completed state action via a post-pipeline notification.
#endregion

#region Design
// Logs only — its value is the shape: subscribe to PostPipelineNotification and filter on
// Request type to run cross-cutting work after an action finishes, without coupling to
// or modifying the action's handler. Copy this pattern for real side effects.
#endregion

namespace TimeWarp.Architecture.Features.Counters;

using static CounterState;

internal class IncrementCountNotificationHandler : INotificationHandler<PostPipelineNotification>
{
  private readonly ILogger Logger;

  public IncrementCountNotificationHandler(ILogger<IncrementCountNotificationHandler> logger)
  {
    Logger = logger;
  }

  public Task Handle
  (
    PostPipelineNotification postPipelineNotification,
    CancellationToken cancellationToken
  )
  {
    _ = cancellationToken;
    if (postPipelineNotification.Request is not IncrementCounterActionSet.Action)
    {
      return Task.CompletedTask;
    }

    Logger.LogDebug(postPipelineNotification.Request.GetType().Name);
    Logger.LogDebug($"{nameof(IncrementCountNotificationHandler)} handled");
    return Task.CompletedTask;
  }
}
