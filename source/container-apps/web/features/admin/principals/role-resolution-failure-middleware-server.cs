#region Purpose
// Maps RoleResolutionFailedException to HTTP 503 so a role-store outage is not 401 or 403.
#endregion

#region Design
// Task 160: IClaimsTransformation runs inside AuthenticationService.AuthenticateAsync after a
// successful cookie (or mock) authenticate. A throw there must not become Challenge 401
// (PolicyEvaluator: !AuthenticateResult.Succeeded → Challenge) and must not become Forbid 403
// (empty roles / no grants). This middleware is registered AFTER UseDeveloperExceptionPage
// (inner of the Dev page) and BEFORE UseAuthentication so it observes the throw, writes 503,
// and does not rethrow — DeveloperExceptionPage then sees a completed response. Production has
// no Dev page; this middleware is the mapper. Response body is omitted: the status is the
// contract. Logs the inner store failure for operators. If the response has already started,
// rethrow rather than corrupt the stream.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Principals;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using TimeWarp.Architecture.Features;

/// <summary>Converts <see cref="RoleResolutionFailedException"/> to HTTP 503 Service Unavailable.</summary>
public sealed class RoleResolutionFailureMiddleware
{
  private static readonly Action<ILogger, Exception?> LogRoleResolutionFailed =
    LoggerMessage.Define
    (
      LogLevel.Error,
      new EventId(1, nameof(LogRoleResolutionFailed)),
      "Role resolution failed; returning 503 so an infrastructure failure is not an authentication verdict."
    );

  private readonly RequestDelegate Next;
  private readonly ILogger Logger;

  public RoleResolutionFailureMiddleware(
    RequestDelegate next,
    ILogger<RoleResolutionFailureMiddleware> logger)
  {
    Next = next ?? throw new ArgumentNullException(nameof(next));
    Logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  public async Task InvokeAsync(HttpContext httpContext)
  {
    ArgumentNullException.ThrowIfNull(httpContext);

    try
    {
      await Next(httpContext).ConfigureAwait(false);
    }
    catch (RoleResolutionFailedException exception)
    {
      LogRoleResolutionFailed(Logger, exception);
      if (httpContext.Response.HasStarted)
      {
        throw;
      }

      httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
    }
  }
}
