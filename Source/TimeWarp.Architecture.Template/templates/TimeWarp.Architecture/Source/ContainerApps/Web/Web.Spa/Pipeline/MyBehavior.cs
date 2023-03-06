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

  public MyBehavior
  (
    ILogger<MyBehavior<TRequest, TResponse>> aLogger
  )
  {
    Logger = aLogger;
    Logger.LogDebug($"{GetType().Name}: Constructor");
  }

  public async Task<TResponse> Handle
  (
    TRequest aRequest,
    RequestHandlerDelegate<TResponse> aNext,
    CancellationToken aCancellationToken
  )
  {
    Guard.Argument(aNext, nameof(aNext)).NotNull();

    Logger.LogDebug($"{GetType().Name}: Start");

    Logger.LogDebug($"{GetType().Name}: Call next");
    TResponse newState = await aNext().ConfigureAwait(false);
    Logger.LogDebug($"{GetType().Name}: Start Post Processing");
    // Constrain here based on a type or anything you want.
    if (typeof(IState).IsAssignableFrom(typeof(TResponse)))
    {
      Logger.LogDebug($"{GetType().Name}: Do Constrained Action");
    }

    Logger.LogDebug($"{GetType().Name}: End");
    return newState;
  }
}
