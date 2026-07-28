#region Purpose
// Placeholder Mediator pipeline behavior marking where cross-cutting concerns hook in.
#endregion

#region Design
// Exemplar for generated apps: a no-op behavior registered before FluentValidationBehavior
// (registration order is execution order). Logs with LoggerMessage (not Console.WriteLine) so
// the template does not teach host logging anti-patterns (task 131 F-017). Replace with real
// concerns (metrics, transactions) rather than stacking more demo behaviors.
// Lives in the api-server artifact folder as host bootstrap exemplar; move under features/
// or platform/ if it grows real product logic.
#endregion

namespace TimeWarp.Architecture.Api.Server;

public partial class GenericPipelineBehavior<TRequest, TResponse>(ILogger<GenericPipelineBehavior<TRequest, TResponse>> logger)
  : IPipelineBehavior<TRequest, TResponse>
  where TRequest : notnull
{
  public async Task<TResponse> Handle
  (
    TRequest request,
    RequestHandlerDelegate<TResponse> next,
    CancellationToken cancellationToken
  )
  {
    LogHandling(logger, typeof(TRequest).Name);
    TResponse response = await next().ConfigureAwait(false);
    LogFinished(logger, typeof(TRequest).Name);
    return response;
  }

  [LoggerMessage(Level = LogLevel.Debug, Message = "Handling {RequestType}")]
  private static partial void LogHandling(ILogger logger, string requestType);

  [LoggerMessage(Level = LogLevel.Debug, Message = "Finished {RequestType}")]
  private static partial void LogFinished(ILogger logger, string requestType);
}
