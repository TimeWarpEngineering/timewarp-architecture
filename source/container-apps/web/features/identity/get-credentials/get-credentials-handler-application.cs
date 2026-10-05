#region Purpose
// Server-side handler for the GetCredentials query: lists the CALLER's own credentials, never
// another principal's.
#endregion

#region Design
// IDOR rule (task 104-005, load-bearing): the principal id comes ONLY from
// ICurrentPrincipalAccessor — command.UserId (the IAuthApiRequest field) is never read here. There is
// no "list someone else's credentials" code path to accidentally reach; the query implicitly scopes
// to the caller by construction, not by a runtime ownership check (contrast RevokeCredential.Handler,
// where an ownership check IS needed because the target is chosen by CredentialId, not implied by the
// caller).
// Null caller -> 401 is defense-in-depth: [EndpointAuthorize(Policy=CredentialManageSelf)]
// already requires an authenticated principal to reach this handler at all (same posture as
// IAgentCallerContext's Design region).
// Secret-material omission (load-bearing, security): CredentialSummary's constructor only ever
// receives Id/Type/Label/Nickname/CreatedAt/RevokedAt/IsActive/RegisteredWith/Fingerprint/
// LastUsedAt/AccountHint (the display-only Entra account text, task 250 — caller-scoped PII) —
// Credential.Handle and Credential.PublicMaterial are never read here, so there is no code path that
// could accidentally leak them even under a future refactor that adds a field; the contract's
// CredentialSummary shape (see get-credentials.cs's Design region) is what makes serializing either
// one impossible even if this handler tried. Fingerprint (task 248-001) is Credential.Fingerprint —
// the domain computes the one-way digest from its private handle field; this handler never touches
// the handle bytes to produce it.
// A pure read — no IPrincipalStore Update* call, so no concurrency note applies (matches
// GetCurrentSession.Handler's Design region reasoning).
// Offers (task 279): CredentialOffers over the caller's credentials plus EntraSignInOffer (the same
// answer GetEntraSignInOffered gives the login page). With IncludeRevoked=false the listed rows are
// exactly the active set the rules count; with IncludeRevoked=true CredentialOffers ignores revoked rows.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

using Microsoft.Extensions.Options;
using TimeWarp.Identity;
using static TimeWarp.Architecture.Features.Identity.GetCredentials;

public sealed partial class GetCredentials
{
  public class Handler : IRequestHandler<Query, OneOf<Response, SharedProblemDetails>>
  {
    private readonly IPrincipalStore PrincipalStore;
    private readonly ICurrentPrincipalAccessor CurrentPrincipalAccessor;
    private readonly IOptions<EntraAuthenticationOptions> EntraOptions;
    private readonly ISiteSettingsStore SiteSettingsStore;

    public Handler
    (
      IPrincipalStore principalStore,
      ICurrentPrincipalAccessor currentPrincipalAccessor,
      IOptions<EntraAuthenticationOptions> entraOptions,
      ISiteSettingsStore siteSettingsStore
    )
    {
      PrincipalStore = principalStore;
      CurrentPrincipalAccessor = currentPrincipalAccessor;
      EntraOptions = entraOptions;
      SiteSettingsStore = siteSettingsStore;
    }

    public async Task<OneOf<Response, SharedProblemDetails>> Handle(Query query, CancellationToken cancellationToken)
    {
      PrincipalId? callerId = await CurrentPrincipalAccessor.GetCurrentPrincipalIdAsync(cancellationToken);
      if (callerId is null)
      {
        return IdentityProblems.Unauthenticated();
      }

      IReadOnlyList<Credential> credentials =
        await PrincipalStore.ListCredentialsAsync(callerId.Value, query.IncludeRevoked, cancellationToken);

      var summaries = credentials
        .Select(credential => new CredentialSummary(
          credential.Id,
          credential.Type,
          credential.Label,
          credential.Nickname,
          credential.CreatedAt,
          credential.RevokedAt,
          isActive: !credential.IsRevoked,
          credential.RegisteredWith,
          credential.Fingerprint,
          credential.LastUsedAt,
          credential.AccountHint))
        .ToList();

      bool microsoft365Offered = await EntraSignInOffer.IsOfferedAsync(EntraOptions.Value, SiteSettingsStore, cancellationToken);
      return new Response(summaries, CredentialOffers.For(summaries, microsoft365Offered));
    }
  }
}
