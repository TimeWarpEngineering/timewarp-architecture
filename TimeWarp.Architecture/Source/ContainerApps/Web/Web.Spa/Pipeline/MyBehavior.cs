namespace TimeWarp.Architecture.Pipeline;

/// <summary>
/// Sample Pipeline Behavior AKA Middle-ware
/// </summary>
/// <typeparam name="TRequest"></typeparam>
/// <typeparam name="TResponse"></typeparam>
/// <remarks>see MediatR for more examples</remarks>
public class MyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
  where TRequest : notnull
{
  private readonly ILogger Logger;

  public Guid Guid { get; } = Guid.NewGuid();
  private string TypeName => GetType().Name;

  public MyBehavior
  (
    ILogger<MyBehavior<TRequest, TResponse>> aLogger
  )
  {
    Logger = aLogger;
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

    Logger.LogDebug(message: "{typeName}: Start", TypeName);

    Logger.LogDebug(message: "{typeName}: Call next", TypeName);
    TResponse newState = await next().ConfigureAwait(false);
    Logger.LogDebug(message: "{typeName}: Start Post Processing",TypeName);
    // Constrain here based on a type or anything you want.
    if (typeof(IState).IsAssignableFrom(typeof(TResponse)))
    {
      Logger.LogDebug(message: "{typeName}: Do Constrained Action", TypeName);
    }

    Logger.LogDebug(message: "{typeName}: End",TypeName);
    return newState;
  }
}
