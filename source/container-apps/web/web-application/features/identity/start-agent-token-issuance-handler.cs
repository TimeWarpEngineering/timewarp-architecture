#region Purpose
// Server-side handler for the StartAgentTokenIssuance command: mints a one-time token-issuance
// challenge for an agent-key ceremony.
#endregion

#region Design
// No IPrincipalStore call — same rationale as StartAgentKeyRegistration.Handler's Design region:
// nothing to look up yet, the agent has not yet named which KeyId it is proving possession of.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

using System.Buffers.Text;
using static TimeWarp.Architecture.Features.Identity.StartAgentTokenIssuance;

public sealed partial class StartAgentTokenIssuance
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
      byte[] challenge = ChallengeStore.Issue(AgentKeyCeremonyType.TokenIssuance);
      return Task.FromResult<OneOf<Response, SharedProblemDetails>>(new Response(Base64Url.EncodeToString(challenge)));
    }
  }
}
