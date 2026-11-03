#region Purpose
// Completes an Entra ID ticket: validate tid/oid/iss, then link, sync-hit, or bootstrap choice.
#endregion

#region Design
// RFC 219 fork 1 + D4 + D10. HTTP Challenge/OnTicketReceived stay in the server adapter; this type
// is the store-side decision. Every entry point checks the issuer first: a mismatch logs Warning
// with expected vs token issuer URIs (not secrets) and returns a 400 whose detail names that check.
// The link mode and the already-have attach go to EntraAccountLinker (link, merge, and
// one-Microsoft-365-per-principal rules live there). Last-used stamps and the account hint refresh
// go to EntraSignInStamp. Both are built here from the injected stores, so the DI registration is
// this one type.
// Sync-hit is Find-by-handle of an active EntraAccount. Revoked rows are non-hits for sync
// (403 EntraCredentialRevoked, not Restore; unique Type+Handle still blocks re-insert).
// Bootstrap Create is gated by IEntraSignInPolicy (site settings AllowBootstrap AND tenant
// acceptance: restricted list or any non-empty tid). Unknown-handle bootstrap does NOT mint a
// principal immediately: ProcessAsync returns EntraChoiceRequired so the HTTP adapter parks
// claims and the SPA choose page decides create vs already-have. CompleteBootstrapCreateAsync
// is the original mint (Principal.Create + EntraAccount + first-admin). AttachEntraToPrincipalAsync
// is link semantics for the already-have path.
// Concurrent first-login for the same tid:oid can miss both finds, create two principals, and
// lose on unique (Type, Handle) at AddCredentialAsync. On that InvalidOperationException, re-Find
// by handle; an active winner is treated as sync-hit (return that PrincipalId). IPrincipalStore
// has no delete-principal — the losing AddPrincipalAsync row is abandoned.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

using Microsoft.Extensions.Logging;
using System.Text;
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
  private readonly EntraSignInStamp Stamp;
  private readonly EntraAccountLinker Linker;
  private readonly ILogger<EntraTicketProcessor> Logger;

  public EntraTicketProcessor
  (
    IPrincipalStore principalStore,
    IPrincipalRoleStore principalRoleStore,
    IEntraSignInPolicy entraSignInPolicy,
    CredentialUsageRecorder usageRecorder,
    ILogger<EntraTicketProcessor> logger
  )
  {
    PrincipalStore = principalStore;
    PrincipalRoleStore = principalRoleStore;
    EntraSignInPolicy = entraSignInPolicy;
    Stamp = new EntraSignInStamp(principalStore, usageRecorder);
    Linker = new EntraAccountLinker(principalStore, entraSignInPolicy, Stamp);
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
      return await Linker.LinkAsync(existing, handle, material, claims, linkCallerPrincipalId, cancellationToken);
    }

    return await ProcessBootstrapAsync(existing, claims, cancellationToken);
  }

  private static bool IssuerMatchesTenant(EntraIdTokenClaims claims, out string expectedIssuer)
  {
    expectedIssuer = Encoding.UTF8.GetString(EntraIssuerMaterial.FromTenantId(claims.TenantId));
    return string.Equals(claims.Issuer, expectedIssuer, StringComparison.Ordinal);
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

      await Stamp.RecordSignInAsync(existing, claims, cancellationToken);
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

      await Stamp.RecordSignInAsync(existing, claims, cancellationToken);
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

        await Stamp.RecordSignInAsync(winner, claims, cancellationToken);
        return winner.PrincipalId;
      }

      return IdentityProblems.CredentialAlreadyRegistered("Entra account");
    }

    await Stamp.RecordNewAsync(entraCredential, cancellationToken);
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

    return await Linker.AttachToPrincipalAsync(claims, principalId, cancellationToken);
  }
}
