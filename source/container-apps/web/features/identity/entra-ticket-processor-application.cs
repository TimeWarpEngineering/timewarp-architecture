#region Purpose
// Completes an Entra ID ticket: validate tid/oid/iss, then link, sync-hit, or bootstrap choice.
#endregion

#region Design
// RFC 219 fork 1 + D4 + D10. HTTP Challenge/OnTicketReceived stay in the server adapter; this type
// is the store-side decision. Link requires the caller's PrincipalId (D4: identity-session is
// enough — no passkey re-assert). Duplicate handle is 409 with no foreign-id oracle (104-005).
// Sync-hit is Find-by-handle of an active EntraAccount. Revoked rows are non-hits for sync and for
// link-merge (403 EntraCredentialRevoked, not Restore; unique Type+Handle still blocks re-insert).
// Bootstrap Create is gated by IEntraSignInPolicy (site settings AllowBootstrap AND
// tid GUID-equals Authentication:Entra:TenantId). Unknown-handle bootstrap does NOT mint a
// principal immediately: ProcessAsync returns EntraChoiceRequired so the HTTP adapter parks
// claims and the SPA choose page decides create vs already-have. CompleteBootstrapCreateAsync
// is the original mint (Principal.Create + EntraAccount + first-admin). AttachEntraToPrincipalAsync
// is link semantics for the already-have path.
// Link: when the handle is owned by another active (non-revoked) unmerged principal B,
// MergePrincipalAsync(B, A) instead of 409 — the Entra sign-in is proof of B. Revoked foreign
// rows refuse with 403 (do not merge). 409 remains for already-on-this-account and for
// merged/quarantined owners. Sync-hit and AllowBootstrap-off are unchanged.
// Concurrent first-login for the same tid:oid can miss both finds, create two principals, and
// lose on unique (Type, Handle) at AddCredentialAsync. On that InvalidOperationException, re-Find
// by handle; an active winner is treated as sync-hit (return that PrincipalId). IPrincipalStore
// has no delete-principal — the losing AddPrincipalAsync row is abandoned.
// Issuer mismatch logs Warning with expected vs token issuer URIs (not secrets) and returns a
// 400 whose detail names that check.
// One active EntraAccount per principal (task 229): link of a second handle is 409 Microsoft 365
// already linked before AddCredentialAsync. Switch is Unlink then Link. Concurrent links of two
// different handles can both pass the list-then-insert check (same TOCTOU class as last-credential
// revoke); unique (Type, Handle) does not serialize two distinct oids.
// Credential.Label is the provider, EntraIdTokenClaims.ProviderLabel ("Microsoft 365"), on both
// bootstrap and link Create; the account is Credential.AccountHint = the token's preferred_username
// (task 250 — display-only, never a join key; the join stays tid+oid). Principal.SetDisplayName stays
// the name claim. Refresh decision (task 250): a sign-in that resolves to an existing active Entra
// credential (sync-hit, and the already-linked early return of CompleteBootstrapCreateAsync) also
// refreshes AccountHint when the token carries a different preferred_username — that backfills links
// made before 250 (which have no hint) and follows UPN renames. A token WITHOUT preferred_username
// leaves the stored hint alone (absence is not evidence the account changed). The refresh write is
// advisory like last-used (CredentialUsageRecorder): it only happens when the value changed, and a
// lost Version race is DROPPED — a concurrent revoke/rename wins and the next sign-in retries. The
// hint is PII: it is never logged here and reaches the wire only via the caller's own GetCredentials.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

using System.Text;
using Microsoft.Extensions.Logging;
using TimeWarp.Architecture.Features;

public sealed class EntraTicketProcessor
{
  public const string ModeLink = "link";
  public const string ModeBootstrap = "bootstrap";

  private static readonly Action<ILogger, string, string, Exception?> LogIssuerMismatch =
    LoggerMessage.Define<string, string>
    (
      LogLevel.Warning,
      new EventId(1, nameof(LogIssuerMismatch)),
      "Entra ticket issuer does not match tenant. ExpectedIssuer={ExpectedIssuer} TokenIssuer={TokenIssuer}"
    );

  private readonly IPrincipalStore PrincipalStore;
  private readonly IPrincipalRoleStore PrincipalRoleStore;
  private readonly IEntraSignInPolicy EntraSignInPolicy;
  private readonly ILogger<EntraTicketProcessor> Logger;

  public EntraTicketProcessor
  (
    IPrincipalStore principalStore,
    IPrincipalRoleStore principalRoleStore,
    IEntraSignInPolicy entraSignInPolicy,
    ILogger<EntraTicketProcessor> logger
  )
  {
    PrincipalStore = principalStore;
    PrincipalRoleStore = principalRoleStore;
    EntraSignInPolicy = entraSignInPolicy;
    Logger = logger;
  }

  public async Task<OneOf<PrincipalId, EntraChoiceRequired, SharedProblemDetails>> ProcessAsync
  (
    EntraIdTokenClaims claims,
    string mode,
    PrincipalId? linkCallerPrincipalId,
    CancellationToken cancellationToken
  )
  {
    if (!IssuerMatchesTenant(claims, out string expectedIssuer))
    {
      LogIssuerMismatch(Logger, expectedIssuer, claims.Issuer, null);
      return IdentityProblems.InvalidEntraTokenIssuerMismatch();
    }

    bool isLink = string.Equals(mode, ModeLink, StringComparison.OrdinalIgnoreCase);
    if (!isLink && !string.Equals(mode, ModeBootstrap, StringComparison.OrdinalIgnoreCase))
    {
      return IdentityProblems.InvalidEntraMode();
    }

    byte[] handle = EntraAccountHandle.Encode(claims.TenantId, claims.ObjectId);
    byte[] material = EntraIssuerMaterial.FromTenantId(claims.TenantId);
    Credential? existing = await PrincipalStore.FindCredentialByHandleAsync(
      CredentialType.EntraAccount,
      handle,
      cancellationToken);

    if (isLink)
    {
      return await ProcessLinkAsync(existing, handle, material, claims, linkCallerPrincipalId, cancellationToken);
    }

    return await ProcessBootstrapAsync(existing, claims, cancellationToken);
  }

  private static bool IssuerMatchesTenant(EntraIdTokenClaims claims, out string expectedIssuer)
  {
    expectedIssuer = Encoding.UTF8.GetString(EntraIssuerMaterial.FromTenantId(claims.TenantId));
    return string.Equals(claims.Issuer, expectedIssuer, StringComparison.Ordinal);
  }

  private async Task<OneOf<PrincipalId, EntraChoiceRequired, SharedProblemDetails>> ProcessLinkAsync
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
      return await CompleteLinkWhenHandleOwnedAsync(existing, caller, cancellationToken);
    }

    OneOf<PrincipalId, SharedProblemDetails> alreadyLinked = await RefuseIfCallerAlreadyHasEntraAsync(
      caller,
      cancellationToken);
    if (alreadyLinked.IsT1)
    {
      return alreadyLinked.AsT1;
    }

    OneOf<PrincipalId, SharedProblemDetails> attached =
      await AttachEntraToPrincipalAsync(handle, material, claims.AccountHint, caller, cancellationToken);
    if (attached.IsT1)
    {
      return attached.AsT1;
    }

    return attached.AsT0;
  }

  private async Task<OneOf<PrincipalId, EntraChoiceRequired, SharedProblemDetails>> CompleteLinkWhenHandleOwnedAsync(
    Credential existing,
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

    return caller;
  }

  private async Task<OneOf<PrincipalId, EntraChoiceRequired, SharedProblemDetails>> ProcessBootstrapAsync
  (
    Credential? existing,
    EntraIdTokenClaims claims,
    CancellationToken cancellationToken
  )
  {
    if (existing is { IsRevoked: false })
    {
      EntraSignInDecision syncHit = await EntraSignInPolicy.EvaluateAsync(
        EntraSignInMode.SyncHit,
        claims.TenantId,
        cancellationToken);
      if (!syncHit.Allowed)
      {
        return syncHit.Problem ?? IdentityProblems.UntrustedTenant();
      }

      Principal? principal = await PrincipalStore.GetPrincipalAsync(existing.PrincipalId, cancellationToken);
      if (principal is null)
      {
        return IdentityProblems.AuthenticationFailed();
      }

      if (!principal.IsActive)
      {
        return IdentityProblems.Quarantined();
      }

      await RefreshAccountHintAsync(existing, claims, cancellationToken);
      return existing.PrincipalId;
    }

    if (existing is { IsRevoked: true })
    {
      EntraSignInDecision revokedTenant = await EntraSignInPolicy.EvaluateAsync(
        EntraSignInMode.SyncHit,
        claims.TenantId,
        cancellationToken);
      if (!revokedTenant.Allowed)
      {
        return revokedTenant.Problem ?? IdentityProblems.UntrustedTenant();
      }

      return IdentityProblems.EntraCredentialRevoked();
    }

    EntraSignInDecision createDecision = await EntraSignInPolicy.EvaluateAsync(
      EntraSignInMode.BootstrapCreate,
      claims.TenantId,
      cancellationToken);
    if (!createDecision.Allowed)
    {
      return createDecision.Problem ?? IdentityProblems.BootstrapNotAllowed();
    }

    return new EntraChoiceRequired();
  }

  public async Task<OneOf<PrincipalId, SharedProblemDetails>> CompleteBootstrapCreateAsync(
    EntraIdTokenClaims claims,
    CancellationToken cancellationToken)
  {
    if (!IssuerMatchesTenant(claims, out string expectedIssuer))
    {
      LogIssuerMismatch(Logger, expectedIssuer, claims.Issuer, null);
      return IdentityProblems.InvalidEntraTokenIssuerMismatch();
    }

    EntraSignInDecision createDecision = await EntraSignInPolicy.EvaluateAsync(
      EntraSignInMode.BootstrapCreate,
      claims.TenantId,
      cancellationToken);
    if (!createDecision.Allowed)
    {
      return createDecision.Problem ?? IdentityProblems.BootstrapNotAllowed();
    }

    byte[] handle = EntraAccountHandle.Encode(claims.TenantId, claims.ObjectId);
    byte[] material = EntraIssuerMaterial.FromTenantId(claims.TenantId);
    Credential? existing = await PrincipalStore.FindCredentialByHandleAsync(
      CredentialType.EntraAccount,
      handle,
      cancellationToken);
    if (existing is { IsRevoked: false })
    {
      Principal? existingPrincipal = await PrincipalStore.GetPrincipalAsync(existing.PrincipalId, cancellationToken);
      if (existingPrincipal is null)
      {
        return IdentityProblems.AuthenticationFailed();
      }

      if (!existingPrincipal.IsActive)
      {
        return IdentityProblems.Quarantined();
      }

      await RefreshAccountHintAsync(existing, claims, cancellationToken);
      return existing.PrincipalId;
    }

    if (existing is { IsRevoked: true })
    {
      return IdentityProblems.EntraCredentialRevoked();
    }

    var created = Principal.Create(PrincipalKind.Human);
    if (!string.IsNullOrWhiteSpace(claims.DisplayName))
    {
      created.SetDisplayName(claims.DisplayName);
    }

    await PrincipalStore.AddPrincipalAsync(created, cancellationToken);

    var entraCredential = Credential.Create(
      created.Id,
      CredentialType.EntraAccount,
      handle,
      material,
      EntraIdTokenClaims.ProviderLabel,
      accountHint: claims.AccountHint);
    try
    {
      await PrincipalStore.AddCredentialAsync(entraCredential, cancellationToken);
    }
    catch (InvalidOperationException)
    {
      Credential? winner = await PrincipalStore.FindCredentialByHandleAsync(
        CredentialType.EntraAccount,
        handle,
        cancellationToken);
      if (winner is { IsRevoked: false })
      {
        Principal? winnerPrincipal = await PrincipalStore.GetPrincipalAsync(
          winner.PrincipalId,
          cancellationToken);
        if (winnerPrincipal is null)
        {
          return IdentityProblems.AuthenticationFailed();
        }

        if (!winnerPrincipal.IsActive)
        {
          return IdentityProblems.Quarantined();
        }

        return winner.PrincipalId;
      }

      return IdentityProblems.CredentialAlreadyRegistered("Entra account");
    }

    _ = await PrincipalRoleStore.TryClaimFirstAdministratorAsync(created.Id, cancellationToken);
    return created.Id;
  }

  public async Task<OneOf<PrincipalId, SharedProblemDetails>> AttachEntraToPrincipalAsync(
    EntraIdTokenClaims claims,
    PrincipalId principalId,
    CancellationToken cancellationToken)
  {
    if (!IssuerMatchesTenant(claims, out string expectedIssuer))
    {
      LogIssuerMismatch(Logger, expectedIssuer, claims.Issuer, null);
      return IdentityProblems.InvalidEntraTokenIssuerMismatch();
    }

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

    return await AttachEntraToPrincipalAsync(
      handle,
      material,
      claims.AccountHint,
      principalId,
      cancellationToken);
  }

  private async Task<OneOf<PrincipalId, SharedProblemDetails>> RefuseIfCallerAlreadyHasEntraAsync(
    PrincipalId principalId,
    CancellationToken cancellationToken)
  {
    IReadOnlyList<Credential> callerCredentials = await PrincipalStore.ListCredentialsAsync(
      principalId,
      includeRevoked: false,
      cancellationToken);
    if (callerCredentials.Any(credential => credential.Type == CredentialType.EntraAccount))
    {
      return IdentityProblems.Microsoft365AlreadyLinked();
    }

    return principalId;
  }

  private async Task RefreshAccountHintAsync(
    Credential existing,
    EntraIdTokenClaims claims,
    CancellationToken cancellationToken)
  {
    if (claims.AccountHint is null || !existing.SetAccountHint(claims.AccountHint))
    {
      return;
    }

    try
    {
      await PrincipalStore.UpdateCredentialAsync(existing, cancellationToken);
    }
    catch (ConcurrencyConflictException)
    {
      // Advisory display text: a concurrent writer (revoke / rename) wins; the next sign-in retries.
    }
  }

  private async Task<OneOf<PrincipalId, SharedProblemDetails>> AttachEntraToPrincipalAsync(
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

    return principalId;
  }
}
