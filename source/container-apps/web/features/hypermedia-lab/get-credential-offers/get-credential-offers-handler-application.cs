#region Purpose
// Server-side handler for GetCredentialOffers: the caller's credentials plus the catalog actions valid for them now (approach B).
#endregion

#region Design
// Decisions come from LabCredentialSnapshot (real Identity handlers + CredentialRules); this handler
// only spells them in B's vocabulary. Per active credential: Rename always (the user supplies the
// nickname), Revoke only while CredentialRules.CanRevoke holds. Page-level: Link Microsoft 365 while
// CredentialRules.CanLinkMicrosoft365 holds. Every name is an OfferedActionNames constant, so the
// server cannot name a catalog entry the SPA test has not resolved.
#endregion

namespace TimeWarp.Architecture.Features.HypermediaLab.Application;

using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Foundation.Features;
using static TimeWarp.Architecture.Features.HypermediaLab.GetCredentialOffers;

public sealed partial class GetCredentialOffers
{
  [CrossSliceReference(typeof(GetCredentials), "Offers are computed over the GetCredentials summaries (task 275 evaluation).")]
  public sealed class Handler : IRequestHandler<Query, OneOf<Response, SharedProblemDetails>>
  {
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

      List<OfferedAction> offers = [];
      foreach (GetCredentials.CredentialSummary credential in snapshot.Credentials)
      {
        offers.Add(ForCredential(OfferedActionNames.RenameCredential, "Rename", credential.Id));
        if (snapshot.CanRevoke)
        {
          offers.Add(ForCredential(OfferedActionNames.RevokeCredential, "Revoke", credential.Id));
        }
      }

      if (snapshot.CanLinkMicrosoft365)
      {
        offers.Add(new OfferedAction(OfferedActionNames.LinkMicrosoft365, "Link Microsoft 365", subject: null, new Dictionary<string, System.Text.Json.JsonElement>()));
      }

      return new Response(snapshot.Credentials, offers);
    }
  }
}
