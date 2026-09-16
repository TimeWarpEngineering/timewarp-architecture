#region Purpose
// Completes an Entra ID ticket: validate tid/oid/iss, then link, sync-hit, or bootstrap.
#endregion

#region Design
// RFC 219 fork 1 + D4 + D10. HTTP Challenge/OnTicketReceived stay in the server adapter; this type
// is the store-side decision. Link requires the caller's PrincipalId (D4: identity-session is
// enough — no passkey re-assert). Duplicate handle is 409 with no foreign-id oracle (104-005).
// Sync-hit is Find-by-handle of an active EntraAccount. Revoked rows are non-hits and cannot be
// re-inserted (unique Type+Handle) — 403, not Restore (Graph Restore is a later product path).
// Bootstrap Create is gated by IEntraSignInPolicy (site settings AllowBootstrap AND
// tid ∈ TrustedTenants). TrustedTenants also gates sync-hit so an untrusted tenant never
// issues a session. Policy replaces EntraAuthenticationOptions for those two fields.
// First human bootstrap claims Administrator the same way CompletePasskeyRegistration does.
// Concurrent first-login for the same tid:oid can miss both finds, create two principals, and
// lose on unique (Type, Handle) at AddCredentialAsync. On that InvalidOperationException, re-Find
// by handle; an active winner is treated as sync-hit (return that PrincipalId). IPrincipalStore
// has no delete-principal — the losing AddPrincipalAsync row is abandoned.
// Issuer mismatch logs Warning with expected vs token issuer URIs (not secrets) and returns a
// 400 whose detail names that check.
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

  public async Task<OneOf<PrincipalId, SharedProblemDetails>> ProcessAsync
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
      return await ProcessLinkAsync(existing, handle, material, linkCallerPrincipalId, cancellationToken);
    }

    return await ProcessBootstrapAsync(existing, handle, material, claims, cancellationToken);
  }

  private static bool IssuerMatchesTenant(EntraIdTokenClaims claims, out string expectedIssuer)
  {
    expectedIssuer = Encoding.UTF8.GetString(EntraIssuerMaterial.FromTenantId(claims.TenantId));
    return string.Equals(claims.Issuer, expectedIssuer, StringComparison.Ordinal);
  }

  private async Task<OneOf<PrincipalId, SharedProblemDetails>> ProcessLinkAsync
  (
    Credential? existing,
    byte[] handle,
    byte[] material,
    PrincipalId? linkCallerPrincipalId,
    CancellationToken cancellationToken
  )
  {
    if (linkCallerPrincipalId is null)
    {
      return IdentityProblems.Unauthenticated();
    }

    if (existing is not null)
    {
      if (existing.PrincipalId == linkCallerPrincipalId.Value && !existing.IsRevoked)
      {
        return linkCallerPrincipalId.Value;
      }

      return IdentityProblems.CredentialAlreadyRegistered("Entra account");
    }

    var credential = Credential.Create(
      linkCallerPrincipalId.Value,
      CredentialType.EntraAccount,
      handle,
      material,
      "Microsoft 365");
    try
    {
      await PrincipalStore.AddCredentialAsync(credential, cancellationToken);
    }
    catch (InvalidOperationException)
    {
      return IdentityProblems.CredentialAlreadyRegistered("Entra account");
    }

    return linkCallerPrincipalId.Value;
  }

  private async Task<OneOf<PrincipalId, SharedProblemDetails>> ProcessBootstrapAsync
  (
    Credential? existing,
    byte[] handle,
    byte[] material,
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
      "Microsoft 365");
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
}
