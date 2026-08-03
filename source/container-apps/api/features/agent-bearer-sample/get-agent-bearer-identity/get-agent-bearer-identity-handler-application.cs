#region Purpose
// Handler for GetAgentBearerIdentity: reads ambient agent bearer caller and returns principal
// identity plus the presented token's scopes.
#endregion

#region Design
// Pure read — mirrors web GetAgentIdentity.Handler (defense-in-depth 401 if caller context or
// principal is missing despite [EndpointAuthorize]).
// Scopes come from the token claims (IAgentCallerContext), not every scope the principal could hold.
#endregion

namespace TimeWarp.Architecture.Features.AgentBearerSamples.Application;

using TimeWarp.Architecture.Abstractions;
using TimeWarp.Architecture.Features.AgentBearerSamples;
using TimeWarp.Foundation.Types;
using TimeWarp.Identity;
using static TimeWarp.Architecture.Features.AgentBearerSamples.GetAgentBearerIdentity;

public sealed partial class GetAgentBearerIdentity
{
  public class Handler : IRequestHandler<Query, OneOf<Response, SharedProblemDetails>>
  {
    private readonly IAgentCallerContext CallerContext;
    private readonly IPrincipalStore PrincipalStore;

    public Handler(IAgentCallerContext callerContext, IPrincipalStore principalStore)
    {
      CallerContext = callerContext;
      PrincipalStore = principalStore;
    }

    public async Task<OneOf<Response, SharedProblemDetails>> Handle(Query query, CancellationToken cancellationToken)
    {
      AgentCaller? caller = CallerContext.GetCurrentCaller();
      if (caller is null)
      {
        return Unauthorized();
      }

      Principal? principal = await PrincipalStore.GetPrincipalAsync(caller.PrincipalId, cancellationToken);
      if (principal is null)
      {
        return Unauthorized();
      }

      return new Response(principal.Id, principal.Kind, principal.TrustTier, caller.Scopes);
    }

    private static SharedProblemDetails Unauthorized() => new()
    {
      Title = "Unauthorized",
      Status = 401,
      Detail = "A valid agent bearer token is required."
    };
  }
}
