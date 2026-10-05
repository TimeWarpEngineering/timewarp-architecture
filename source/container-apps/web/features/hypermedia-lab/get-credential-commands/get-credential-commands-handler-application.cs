#region Purpose
// Server-side handler for GetCredentialCommands: the caller's credentials plus the link commands valid for them now (approach C).
#endregion

#region Design
// Same decisions as GetCredentialOffers (LabCredentialSnapshot), spelled as C's links: Rename and
// (while CanRevoke) Revoke POST to the existing typed endpoints; Link Microsoft 365 is a NAVIGATE to
// the ChallengeEntra browser endpoint returning to the lab page. Body templates carry the caller's
// principal id as UserId (the existing commands' IAuthApiRequest validator needs it; their handlers
// ignore it). ReturnPath is the lab page route as a literal — the page lives in web-spa, which this
// application layer does not reference.
#endregion

namespace TimeWarp.Architecture.Features.HypermediaLab.Application;

using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Foundation.Features;
using static TimeWarp.Architecture.Features.HypermediaLab.GetCredentialCommands;

public sealed partial class GetCredentialCommands
{
  [CrossSliceReference(typeof(GetCredentials), "Commands are computed over the GetCredentials summaries (task 275 evaluation).")]
  public sealed class Handler : IRequestHandler<Query, OneOf<Response, SharedProblemDetails>>
  {
    internal const string ReturnPath = "/HypermediaLab";

    private readonly ISender Sender;
    private readonly ICurrentPrincipalAccessor CurrentPrincipalAccessor;

    public Handler(ISender sender, ICurrentPrincipalAccessor currentPrincipalAccessor)
    {
      Sender = sender;
      CurrentPrincipalAccessor = currentPrincipalAccessor;
    }

    public async Task<OneOf<Response, SharedProblemDetails>> Handle(Query query, CancellationToken cancellationToken)
    {
      _ = query;
      OneOf<LabCredentialSnapshot, SharedProblemDetails> loaded =
        await LabCredentialSnapshot.LoadAsync(Sender, CurrentPrincipalAccessor, cancellationToken);
      if (loaded.TryPickT1(out SharedProblemDetails? problem, out LabCredentialSnapshot? snapshot))
      {
        return problem;
      }

      Guid userId = snapshot.CallerId.Value;
      List<LinkCommand> commands = [];
      foreach (GetCredentials.CredentialSummary credential in snapshot.Credentials)
      {
        commands.Add(Rename(credential.Id, userId));
        if (snapshot.CanRevoke)
        {
          commands.Add(Revoke(credential.Id, userId));
        }
      }

      if (snapshot.CanLinkMicrosoft365)
      {
        commands.Add(LinkMicrosoft365(ReturnPath));
      }

      return new Response(snapshot.Credentials, commands, SelfHref);
    }
  }
}
