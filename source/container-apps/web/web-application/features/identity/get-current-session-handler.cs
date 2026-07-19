#region Purpose
// Server-side handler for the GetCurrentSession query: reads the ambient browser session, if any.
#endregion

#region Design
// A pure read — no IPrincipalStore call at all, so no concurrency note applies. IsAuthenticated and
// PrincipalId are always set together from the same IBrowserSessionService result, matching the
// contract Response's Design region (the pairing can never disagree).
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

using static TimeWarp.Architecture.Features.Identity.GetCurrentSession;

public sealed partial class GetCurrentSession
{
  public class Handler : IRequestHandler<Query, OneOf<Response, SharedProblemDetails>>
  {
    private readonly IBrowserSessionService BrowserSessionService;

    public Handler(IBrowserSessionService browserSessionService)
    {
      BrowserSessionService = browserSessionService;
    }

    public async Task<OneOf<Response, SharedProblemDetails>> Handle(Query query, CancellationToken cancellationToken)
    {
      PrincipalId? principalId = await BrowserSessionService.GetCurrentPrincipalIdAsync(cancellationToken);

      return new Response(principalId is not null, principalId);
    }
  }
}
