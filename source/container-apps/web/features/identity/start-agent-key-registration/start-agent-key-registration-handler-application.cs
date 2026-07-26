#region Purpose
// Server-side handler for the StartAgentKeyRegistration command: mints a one-time registration
// challenge for an agent-key ceremony.
#endregion

#region Design
// No IPrincipalStore call — nothing to look up or persist yet (mirrors StartPasskeyRegistration.Handler's
// Concurrency note: the ceremony has not produced a credential, so 104-028's Update*/
// ConcurrencyConflictException contract is simply not exercised here).
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

using System.Buffers.Text;
using static TimeWarp.Architecture.Features.Identity.StartAgentKeyRegistration;

public sealed partial class StartAgentKeyRegistration
{
  public class Handler : IRequestHandler<Command, OneOf<Response, SharedProblemDetails>>
  {
    private readonly IAgentKeyChallengeStore ChallengeStore;

    public Handler(IAgentKeyChallengeStore challengeStore)
    {
      ChallengeStore = challengeStore;
    }

    public Task<OneOf<Response, SharedProblemDetails>> Handle(Command command, CancellationToken cancellationToken)
    {
      byte[] challenge = ChallengeStore.Issue(AgentKeyCeremonyType.Registration);
      return Task.FromResult<OneOf<Response, SharedProblemDetails>>(new Response(Base64Url.EncodeToString(challenge)));
    }
  }
}
