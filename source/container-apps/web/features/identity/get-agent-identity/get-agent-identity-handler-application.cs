#region Purpose
// Server-side handler for the GetAgentIdentity query: reads the ambient bearer-authenticated agent
// caller, if any, and returns its principal identity plus the token's scopes.
#endregion

#region Design
// A pure read — no IPrincipalStore Update* call at all, so no concurrency note applies (matches
// GetCurrentSession.Handler's posture).
// IAgentCallerContext.GetCurrentCaller returning null is treated as a 401 here purely as
// defense-in-depth: the endpoint carries [Authorize(Policy = AgentTokenDefaults.IdentityReadPolicy)]
// (web-server), so an unauthenticated/insufficiently-scoped request should never reach this handler
// at all — but the handler does not trust the endpoint attribute alone to be the only thing standing
// between an unauthenticated caller and a Response.
// GetPrincipalAsync returning null (the caller's token validated against a credential/principal that
// existed at bearer-validation time but was deleted since — not achievable in this task, since there
// is no delete API yet, but the check costs nothing) also maps to the same 401 rather than a 500.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

using static TimeWarp.Architecture.Features.Identity.GetAgentIdentity;

public sealed partial class GetAgentIdentity
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
        return IdentityProblems.Unauthorized();
      }

      Principal? principal = await PrincipalStore.GetPrincipalAsync(caller.PrincipalId, cancellationToken);
      if (principal is null)
      {
        return IdentityProblems.Unauthorized();
      }

      return new Response(principal.Id, principal.Kind, principal.TrustTier, caller.Scopes);
    }
  }
}
