#region Purpose
// Map an Entra tenant lookup to a response or a distinct problem.
#endregion

#region Design
// The resolver already logged network failure, timeout, unexpected status, and unexpected
// issuer. This handler only chooses the HTTP problem. Not-found is 404 "Tenant not found".
// Network failure is 503. Timeout is 504. Unexpected status and unexpected issuer are 502.
// Invalid domain is 400. None of these is a null response. Validation of the query still
// runs in FluentValidationBehavior before Handle.
#endregion

namespace TimeWarp.Architecture.Features.Settings.Application;

using TimeWarp.Identity;
using static TimeWarp.Architecture.Features.Settings.ResolveEntraTenant;

public sealed class ResolveEntraTenant
{
  public sealed class Handler : IRequestHandler<Query, OneOf<Response, SharedProblemDetails>>
  {
    private readonly EntraTenantResolver Resolver;

    public Handler(EntraTenantResolver resolver)
    {
      Resolver = resolver;
    }

    public async Task<OneOf<Response, SharedProblemDetails>> Handle(
      Query query,
      CancellationToken cancellationToken)
    {
      EntraTenantLookupResult result = await Resolver.ResolveAsync(query.Domain, cancellationToken)
        .ConfigureAwait(false);
      return result.Outcome switch
      {
        EntraTenantLookupOutcome.Found => new Response(result.TenantId, result.Domain ?? query.Domain),
        EntraTenantLookupOutcome.NotFound => NotFound(result.Domain),
        EntraTenantLookupOutcome.InvalidDomain => InvalidDomain(result.Domain),
        EntraTenantLookupOutcome.NetworkFailure => NetworkFailure(result.Domain),
        EntraTenantLookupOutcome.Timeout => TimedOut(result.Domain),
        EntraTenantLookupOutcome.UnexpectedStatus => UnexpectedStatus(result.Domain, result.StatusCode),
        EntraTenantLookupOutcome.UnexpectedIssuer => UnexpectedIssuer(result.Domain),
        _ => UnexpectedIssuer(result.Domain),
      };
    }
  }

  private static SharedProblemDetails NotFound(string? domain) => new()
  {
    Title = "Tenant not found",
    Status = 404,
    Detail = $"No Entra tenant is published for '{domain}'."
  };

  private static SharedProblemDetails InvalidDomain(string? domain) => new()
  {
    Title = "Invalid domain",
    Status = 400,
    Detail = $"'{domain}' is not a domain name."
  };

  private static SharedProblemDetails NetworkFailure(string? domain) => new()
  {
    Title = "Tenant lookup failed",
    Status = 503,
    Detail = $"The lookup for '{domain}' did not complete. Try again."
  };

  private static SharedProblemDetails TimedOut(string? domain) => new()
  {
    Title = "Tenant lookup timed out",
    Status = 504,
    Detail = $"The lookup for '{domain}' timed out. Try again."
  };

  private static SharedProblemDetails UnexpectedStatus(string? domain, int? statusCode) => new()
  {
    Title = "Tenant lookup failed",
    Status = 502,
    Detail = $"The lookup for '{domain}' returned HTTP {statusCode}."
  };

  private static SharedProblemDetails UnexpectedIssuer(string? domain) => new()
  {
    Title = "Unexpected issuer",
    Status = 502,
    Detail = $"The discovery document for '{domain}' did not contain an Entra tenant issuer."
  };
}
