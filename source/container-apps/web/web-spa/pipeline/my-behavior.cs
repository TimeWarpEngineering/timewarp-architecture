#region Purpose
// Sample mediator pipeline behavior showing where cross-cutting pre/post logic hooks in.
#endregion

#region Design
// Template teaching artifact, not production middleware: it only logs each pipeline stage.
// The IState type check demonstrates how to constrain post-processing to a category of
// responses without a generic constraint, which would exclude the behavior from other requests.
// The Guid property exists to observe instance lifetime when debugging DI scope registrations.
#endregion

namespace TimeWarp.Architecture.Pipeline;

/// <summary>
/// Sample Pipeline Behavior AKA Middle-ware
/// </summary>
/// <typeparam name="TRequest"></typeparam>
/// <typeparam name="TResponse"></typeparam>
/// <remarks>see Mediator for more examples</remarks>
public class MyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
  where TRequest : notnull
{
  private readonly ILogger Logger;

  public Guid Guid { get; } = Guid.NewGuid();
  private string TypeName => GetType().Name;

  public MyBehavior
  (
    ILogger<MyBehavior<TRequest, TResponse>> logger
  )
  {
    Logger = logger;
    Logger.LogDebug(message: "{GetType().Name}: Constructor",TypeName);
  }

  public async Task<TResponse> Handle
  (
    TRequest request,
    RequestHandlerDelegate<TResponse> next,
    CancellationToken cancellationToken
  )
  {
    Guard.Against.Null(next);

    Logger.LogDebug(message: "{TypeName}: Start", TypeName);

    Logger.LogDebug(message: "{TypeName}: Call next", TypeName);
    TResponse newState = await next().ConfigureAwait(false);
    Logger.LogDebug(message: "{TypeName}: Start Post Processing",TypeName);
    // Constrain here based on a type or anything you want.
    if (typeof(IState).IsAssignableFrom(typeof(TResponse)))
    {
      Logger.LogDebug(message: "{TypeName}: Do Constrained Action", TypeName);
    }

    Logger.LogDebug(message: "{TypeName}: End",TypeName);
    return newState;
  }
}
