#region Purpose
// Clears the ambient identity-session cookie so SPA sign-out works without Entra/MSAL.
#endregion

#region Design
// Thin handler: IBrowserSessionService.SignOutAsync only. No principal store access. Idempotent.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;
using TimeWarp.Architecture.Abstractions;
using static TimeWarp.Architecture.Features.Identity.EndBrowserSession;

public sealed partial class EndBrowserSession
{
  public class Handler : IRequestHandler<Command, OneOf<Response, SharedProblemDetails>>
  {
    private readonly IBrowserSessionService BrowserSessionService;

    public Handler(IBrowserSessionService browserSessionService)
    {
      BrowserSessionService = browserSessionService;
    }

    public async Task<OneOf<Response, SharedProblemDetails>> Handle(
      Command command,
      CancellationToken cancellationToken)
    {
      await BrowserSessionService.SignOutAsync(cancellationToken);
      return new Response();
    }
  }
}
