#region Purpose
// Loads what both hypermedia-lab reads decide from: the caller, their active credentials, and the shared Identity rules applied to them.
#endregion

#region Design
// No second implementation (task 275): the credentials come from the real GetCredentials handler and
// the Microsoft 365 flag from the real GetEntraSignInOffered handler, both sent through ISender so
// the same validation pipeline runs. The decisions are Identity's CredentialRules — the predicates
// RevokeCredential.Handler and the Entra link path enforce — so B's offers and C's commands are the
// same set in two vocabularies. The caller id comes from ICurrentPrincipalAccessor only; it fills the
// GetCredentials UserId that its validator requires and the handler ignores (IDOR scoping stays in
// GetCredentials.Handler). Only active credentials are listed: the lab does not show revoked rows.
#endregion

namespace TimeWarp.Architecture.Features.HypermediaLab.Application;

using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Foundation.Features;
using CredentialRules = TimeWarp.Architecture.Features.Identity.Application.CredentialRules;
using IdentityProblems = TimeWarp.Architecture.Features.Identity.Application.IdentityProblems;

[CrossSliceReference(typeof(GetCredentials), "The lab reads credentials and the Microsoft 365 flag through the real Identity handlers and rules (task 275 evaluation).")]
public sealed class LabCredentialSnapshot
{
  public PrincipalId CallerId { get; }
  public IReadOnlyList<GetCredentials.CredentialSummary> Credentials { get; }
  public bool CanRevoke { get; }
  public bool CanLinkMicrosoft365 { get; }

  private LabCredentialSnapshot
  (
    PrincipalId callerId,
    IReadOnlyList<GetCredentials.CredentialSummary> credentials,
    bool canRevoke,
    bool canLinkMicrosoft365
  )
  {
    CallerId = callerId;
    Credentials = credentials;
    CanRevoke = canRevoke;
    CanLinkMicrosoft365 = canLinkMicrosoft365;
  }

  /// <summary>The caller's snapshot, or the problem GetCredentials / GetEntraSignInOffered returned.</summary>
  public static async Task<OneOf<LabCredentialSnapshot, SharedProblemDetails>> LoadAsync
  (
    ISender sender,
    ICurrentPrincipalAccessor currentPrincipalAccessor,
    CancellationToken cancellationToken
  )
  {
    PrincipalId? callerId = await currentPrincipalAccessor.GetCurrentPrincipalIdAsync(cancellationToken);
    if (callerId is null)
    {
      return IdentityProblems.Unauthenticated();
    }

    OneOf<GetCredentials.Response, SharedProblemDetails> credentials =
      await sender.Send(new GetCredentials.Query { UserId = callerId.Value.Value, IncludeRevoked = false }, cancellationToken);
    if (credentials.TryPickT1(out SharedProblemDetails? credentialsProblem, out GetCredentials.Response? list))
    {
      return credentialsProblem;
    }

    OneOf<GetEntraSignInOffered.Response, SharedProblemDetails> offered =
      await sender.Send(new GetEntraSignInOffered.Query(), cancellationToken);
    if (offered.TryPickT1(out SharedProblemDetails? offeredProblem, out GetEntraSignInOffered.Response? microsoft365))
    {
      return offeredProblem;
    }

    GetCredentials.CredentialSummary[] active = [.. list.Credentials.Where(static credential => credential.IsActive)];
    return new LabCredentialSnapshot
    (
      callerId.Value,
      active,
      CredentialRules.CanRevoke(active.Length),
      CredentialRules.CanLinkMicrosoft365(microsoft365.Offered, active.Select(static credential => credential.Type))
    );
  }
}
