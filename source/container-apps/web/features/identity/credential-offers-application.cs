#region Purpose
// Computes the catalog actions GetCredentials offers for the caller's active credentials from the shared CredentialRules.
#endregion

#region Design
// Hypermedia approach B (task 279): the server decides what is valid now and spells it in the SPA's
// catalog vocabulary through the typed offer records (credential-action-offer-contracts.cs, task
// 280). Per active credential: Rename always (the user supplies the nickname), Revoke only while CredentialRules.CanRevoke holds over the count of EVERY active
// credential type — what RevokeCredential.Handler counts. Page-level: Link Microsoft 365 while
// CredentialRules.CanLinkMicrosoft365 holds. Pure over summaries + the offered flag, so the rule
// table is testable without a host, and the SPA's scripted BFF calls it directly so client tests
// cannot drift from it. Revoked rows never get an offer.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

using static TimeWarp.Architecture.Features.Identity.GetCredentials;

public static class CredentialOffers
{
  /// <summary>Offers for <paramref name="credentials"/> (revoked rows are ignored) given whether Microsoft 365 sign-in is offered.</summary>
  public static IReadOnlyList<OfferedAction> For(IReadOnlyList<CredentialSummary> credentials, bool microsoft365Offered)
  {
    CredentialSummary[] active = [.. credentials.Where(static credential => credential.IsActive)];
    bool canRevoke = CredentialRules.CanRevoke(active.Length);

    List<OfferedAction> offers = [];
    foreach (CredentialSummary credential in active)
    {
      offers.Add(OfferedAction.ForCredential(new RenameCredentialOffer(credential.Id.Value), "Rename"));
      if (canRevoke)
      {
        offers.Add(OfferedAction.ForCredential(new RevokeCredentialOffer(credential.Id.Value), "Revoke"));
      }
    }

    if (CredentialRules.CanLinkMicrosoft365(microsoft365Offered, active.Select(static credential => credential.Type)))
    {
      offers.Add(OfferedAction.ForPage(new LinkMicrosoft365Offer(), "Link Microsoft 365"));
    }

    return offers;
  }
}
