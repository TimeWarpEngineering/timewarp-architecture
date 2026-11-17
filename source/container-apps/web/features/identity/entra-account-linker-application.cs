#region Purpose
// Links a Microsoft 365 (Entra) account to a signed-in principal: new link, link-merge, or already-have attach.
#endregion

#region Design
// RFC 219 D4: link requires the caller's PrincipalId (identity-session is enough — no passkey
// re-assert). EntraTicketProcessor checks the issuer and the mode first and hands the link path
// here. Duplicate handle is 409 with no foreign-id oracle (104-005). Revoked rows are non-hits for
// link-merge (403 EntraCredentialRevoked, not Restore; unique Type+Handle still blocks re-insert).
// Link: when the handle is owned by another active (non-revoked) unmerged principal B,
// MergePrincipalAsync(B, A) instead of 409 — the Entra sign-in is proof of B. That save deletes
// B's user xAI API key row. This type does not delete it again. Revoked foreign rows refuse with
// 403 (do not merge). 409 remains for already-on-this-account and for merged/quarantined owners.
// One active EntraAccount per principal (task 229): link of a second handle is 409 Microsoft 365
// already linked before AddCredentialAsync (CredentialRules.HoldsMicrosoft365, shared with
// GetCredentials' CanLinkMicrosoft365 flag, task 282). Switch is Unlink then Link. Concurrent links of two
// different handles can both pass the list-then-insert check (same TOCTOU class as last-credential
// revoke); unique (Type, Handle) does not serialize two distinct oids.
// AttachToPrincipalAsync is the bootstrap already-have path: link semantics without the merge, so
// any existing row for the handle refuses. Every link that lands a credential is stamped through
// EntraSignInStamp; the merged row is re-read first because the merge re-parented and re-versioned it.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

/// <summary>The link and attach paths of an Entra ticket.</summary>
internal sealed class EntraAccountLinker
{
  private readonly IPrincipalStore PrincipalStore;
  private readonly IEntraSignInPolicy EntraSignInPolicy;
  private readonly EntraSignInStamp Stamp;

  public EntraAccountLinker
  (
    IPrincipalStore principalStore,
    IEntraSignInPolicy entraSignInPolicy,
    EntraSignInStamp stamp
  )
  {
    PrincipalStore = principalStore;
    EntraSignInPolicy = entraSignInPolicy;
    Stamp = stamp;
  }

  /// <summary>Link mode: <paramref name="existing"/> is the row found for the token's handle, if any.</summary>
  public async Task<OneOf<PrincipalId, EntraChoiceRequired, SharedProblemDetails>> LinkAsync
  (
    Credential? existing,
    byte[] handle,
    byte[] material,
    EntraIdTokenClaims claims,
    PrincipalId? linkCallerPrincipalId,
    CancellationToken cancellationToken
  )
  {
    if (linkCallerPrincipalId is null)
    {
      return IdentityProblems.Unauthenticated();
    }

    EntraSignInDecision linkDecision = await EntraSignInPolicy.EvaluateAsync(
      EntraSignInMode.Link,
      claims.TenantId,
      cancellationToken);
    if (!linkDecision.Allowed)
    {
      return linkDecision.Problem ?? IdentityProblems.UntrustedTenant();
    }

    PrincipalId caller = linkCallerPrincipalId.Value;
    if (existing is { IsRevoked: true })
    {
      return IdentityProblems.EntraCredentialRevoked();
    }

    if (existing is { IsRevoked: false })
    {
      return await CompleteLinkWhenHandleOwnedAsync(existing, claims, caller, cancellationToken);
    }

    OneOf<PrincipalId, SharedProblemDetails> alreadyLinked = await RefuseIfCallerAlreadyHasEntraAsync(
      caller,
      cancellationToken);
    if (alreadyLinked.IsT1)
    {
      return alreadyLinked.AsT1;
    }

    OneOf<PrincipalId, SharedProblemDetails> attached =
      await AddEntraCredentialAsync(handle, material, claims.AccountHint, caller, cancellationToken);
    if (attached.IsT1)
    {
      return attached.AsT1;
    }

    return attached.AsT0;
  }

  /// <summary>Already-have path: attach the token's account to <paramref name="principalId"/>.</summary>
  public async Task<OneOf<PrincipalId, SharedProblemDetails>> AttachToPrincipalAsync(
    EntraIdTokenClaims claims,
    PrincipalId principalId,
    CancellationToken cancellationToken)
  {
    byte[] handle = EntraAccountHandle.Encode(claims.TenantId, claims.ObjectId);
    byte[] material = EntraIssuerMaterial.FromTenantId(claims.TenantId);
    Credential? existing = await PrincipalStore.FindCredentialByHandleAsync(
      CredentialType.EntraAccount,
      handle,
      cancellationToken);
    if (existing is not null)
    {
      if (existing.PrincipalId == principalId && !existing.IsRevoked)
      {
        return IdentityProblems.AlreadyOnThisAccount("Microsoft 365 account");
      }

      return IdentityProblems.CredentialAlreadyRegistered("Entra account");
    }

    OneOf<PrincipalId, SharedProblemDetails> alreadyLinked = await RefuseIfCallerAlreadyHasEntraAsync(
      principalId,
      cancellationToken);
    if (alreadyLinked.IsT1)
    {
      return alreadyLinked;
    }

    return await AddEntraCredentialAsync(
      handle,
      material,
      claims.AccountHint,
      principalId,
      cancellationToken);
  }

  private async Task<OneOf<PrincipalId, EntraChoiceRequired, SharedProblemDetails>> CompleteLinkWhenHandleOwnedAsync(
    Credential existing,
    EntraIdTokenClaims claims,
    PrincipalId caller,
    CancellationToken cancellationToken)
  {
    if (existing.PrincipalId == caller)
    {
      return IdentityProblems.AlreadyOnThisAccount("Microsoft 365 account");
    }

    Principal? owner = await PrincipalStore.GetPrincipalAsync(existing.PrincipalId, cancellationToken);
    if (owner is null)
    {
      return IdentityProblems.AuthenticationFailed();
    }

    if (!owner.IsActive || owner.MergedIntoPrincipalId is not null)
    {
      return IdentityProblems.CredentialAlreadyRegistered("Entra account");
    }

    OneOf<PrincipalId, SharedProblemDetails> alreadyLinked = await RefuseIfCallerAlreadyHasEntraAsync(
      caller,
      cancellationToken);
    if (alreadyLinked.IsT1)
    {
      return alreadyLinked.AsT1;
    }

    try
    {
      await PrincipalStore.MergePrincipalAsync(existing.PrincipalId, caller, cancellationToken);
    }
    catch (InvalidOperationException)
    {
      return IdentityProblems.AccountNotMergeable();
    }
    catch (ConcurrencyConflictException)
    {
      return IdentityProblems.TooMuchContention();
    }

    // Merge re-parented and re-versioned the row; stamp a fresh snapshot, not the pre-merge one.
    Credential? merged = await PrincipalStore.GetCredentialAsync(existing.Id, cancellationToken);
    if (merged is { IsRevoked: false })
    {
      await Stamp.RecordSignInAsync(merged, claims, cancellationToken);
    }

    return caller;
  }

  private async Task<OneOf<PrincipalId, SharedProblemDetails>> RefuseIfCallerAlreadyHasEntraAsync(
    PrincipalId principalId,
    CancellationToken cancellationToken)
  {
    IReadOnlyList<Credential> callerCredentials = await PrincipalStore.ListCredentialsAsync(
      principalId,
      includeRevoked: false,
      cancellationToken);
    if (CredentialRules.HoldsMicrosoft365(callerCredentials.Select(static credential => credential.Type)))
    {
      return IdentityProblems.Microsoft365AlreadyLinked();
    }

    return principalId;
  }

  private async Task<OneOf<PrincipalId, SharedProblemDetails>> AddEntraCredentialAsync(
    byte[] handle,
    byte[] material,
    string? accountHint,
    PrincipalId principalId,
    CancellationToken cancellationToken)
  {
    var credential = Credential.Create(
      principalId,
      CredentialType.EntraAccount,
      handle,
      material,
      EntraIdTokenClaims.ProviderLabel,
      accountHint: accountHint);
    try
    {
      await PrincipalStore.AddCredentialAsync(credential, cancellationToken);
    }
    catch (InvalidOperationException)
    {
      return IdentityProblems.CredentialAlreadyRegistered("Entra account");
    }

    await Stamp.RecordNewAsync(credential, cancellationToken);
    return principalId;
  }
}
