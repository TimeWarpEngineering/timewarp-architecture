#region Purpose
// Registers ASP.NET Core rate limiting for principal-register + payment-challenge paths (path-partitioned).
#endregion

#region Design
// EDGE VS APP — see AbuseRateLimitOptions Design region. This module is the app ring only.
//
// Why ASP.NET RateLimiter GlobalLimiter (not FastEndpoints.Throttle, not endpoint RequireRateLimiting):
//   - Configurable sliding windows + per-IP partitions + one OnRejected that writes structured
//     application/problem+json (SharedProblemDetails shape + Retry-After) for agents.
//   - FastEndpoints owns its endpoint pipeline; endpoint metadata RequireRateLimiting is unreliable
//     across FE versions relative to UseRateLimiter. A path-classified GlobalLimiter runs in the
//     ASP.NET middleware pipeline (after UseRouting, before FE) and does not depend on FE metadata.
//   - FE Throttle is fixed-window header-key throttling without a problem+json envelope.
//
// Pipeline: UseRateLimiter after UseRouting so the request path is final (post markdown / tip-alias
// rewrites). Rejected requests never reach handlers or PaymentGate.
//
// Partition key = "{policy}:{RemoteIpAddress|unknown}". IOptions is resolved per request so test
// hosts can PostConfigure tight limits before the first partition is created.
// Enabled=false or non-covered paths → GetNoLimiter.
//
// Routes covered (task 104-015):
//   Principal registration: passkey register options/complete; agent-key register options/complete.
//   Payment challenge: /api/tip (GET|POST), /api/demo/metered-capability (GET).
// Authenticated credential-add paths (e.g. credentials/passkey) are intentionally out of scope —
// they require a session and are not mass-sybil mint surfaces.
#endregion

namespace TimeWarp.Architecture.Abuse;

using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TimeWarp.Foundation.Types;
using TimeWarp.Modules;

/// <summary>Host module: rate-limit registration + payment-challenge abuse surfaces.</summary>
public sealed class AbuseRateLimitingModule : IModule
{
  /// <summary>Named policy for passkey/agent principal registration endpoints.</summary>
  public const string PrincipalRegistrationPolicy = "principal-registration";

  /// <summary>Named policy for tip / metered paths that emit unpaid 402 challenges.</summary>
  public const string PaymentChallengePolicy = "payment-challenge";

  public static void ConfigureServices(IServiceCollection serviceCollection, IConfiguration configuration)
  {
    // configuration is unused: partition factories resolve IOptions at request time (test PostConfigure).
    _ = configuration;

    serviceCollection.AddRateLimiter(static limiterOptions =>
    {
      limiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
      limiterOptions.OnRejected = WriteStructured429Async;

      // Path-classified global limiter — does not depend on FastEndpoints endpoint metadata.
      limiterOptions.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
        static httpContext => CreatePartition(httpContext));
    });
  }

  internal static bool IsPrincipalRegistrationRoute(string normalizedRoute) =>
    normalizedRoute is
      "api/identity/passkey/register/options"
      or "api/identity/passkey/register"
      or "api/identity/agent/register/options"
      or "api/identity/agent/register";

  internal static bool IsPaymentChallengeRoute(string normalizedRoute) =>
    normalizedRoute is
      "api/tip"
      or "api/demo/metered-capability";

  private static string NormalizePath(PathString path) =>
    path.Value?.Trim().TrimStart('/').TrimEnd('/') ?? string.Empty;

  private static RateLimitPartition<string> CreatePartition(HttpContext httpContext)
  {
    AbuseRateLimitOptions abuse = httpContext.RequestServices
      .GetRequiredService<IOptions<AbuseRateLimitOptions>>()
      .Value;

    if (!abuse.Enabled)
    {
      return RateLimitPartition.GetNoLimiter("disabled");
    }

    string normalizedPath = NormalizePath(httpContext.Request.Path);
    string clientKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    if (IsPrincipalRegistrationRoute(normalizedPath))
    {
      return SlidingPartition(
        partitionKey: $"{PrincipalRegistrationPolicy}:{clientKey}",
        window: abuse.PrincipalRegistration);
    }

    if (IsPaymentChallengeRoute(normalizedPath))
    {
      return SlidingPartition(
        partitionKey: $"{PaymentChallengePolicy}:{clientKey}",
        window: abuse.PaymentChallenge);
    }

    return RateLimitPartition.GetNoLimiter("exempt");
  }

  private static RateLimitPartition<string> SlidingPartition(
    string partitionKey,
    SlidingWindowLimitOptions window) =>
    RateLimitPartition.GetSlidingWindowLimiter(
      partitionKey,
      _ => new SlidingWindowRateLimiterOptions
      {
        PermitLimit = window.PermitLimit,
        Window = TimeSpan.FromSeconds(window.WindowSeconds),
        SegmentsPerWindow = window.SegmentsPerWindow,
        QueueLimit = 0,
        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
      });

  private static async ValueTask WriteStructured429Async(
    OnRejectedContext context,
    CancellationToken cancellationToken)
  {
    HttpContext httpContext = context.HttpContext;
    httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

    double? retryAfterSeconds = null;
    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
    {
      int retryAfterWholeSeconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
      retryAfterSeconds = retryAfterWholeSeconds;
      httpContext.Response.Headers.RetryAfter = retryAfterWholeSeconds.ToString(CultureInfo.InvariantCulture);
    }

    string normalizedPath = NormalizePath(httpContext.Request.Path);
    string? policyName =
      IsPrincipalRegistrationRoute(normalizedPath) ? PrincipalRegistrationPolicy
      : IsPaymentChallengeRoute(normalizedPath) ? PaymentChallengePolicy
      : null;

    string detail = policyName switch
    {
      PrincipalRegistrationPolicy =>
        "Rate limit exceeded for principal registration. Retry later.",
      PaymentChallengePolicy =>
        "Rate limit exceeded for payment challenge. Retry later.",
      _ => "Rate limit exceeded. Retry later.",
    };

    SharedProblemDetails problem = new()
    {
      Type = "https://httpstatuses.io/429",
      Title = "Too Many Requests",
      Status = StatusCodes.Status429TooManyRequests,
      Detail = detail,
      Instance = httpContext.Request.Path.HasValue ? httpContext.Request.Path.Value : null,
    };

    if (!string.IsNullOrEmpty(policyName))
    {
      problem.Extensions["policy"] = policyName;
    }

    if (retryAfterSeconds is not null)
    {
      problem.Extensions["retryAfterSeconds"] = retryAfterSeconds.Value;
    }

    // contentType overload: WriteAsJsonAsync would otherwise force application/json.
    await httpContext.Response
      .WriteAsJsonAsync(
        problem,
        ContractSerializationDefaults.Options,
        contentType: "application/problem+json; charset=utf-8",
        cancellationToken)
      .ConfigureAwait(false);
  }
}
