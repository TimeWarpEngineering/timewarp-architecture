#region Purpose
// Completes an Entra ID ticket: validate tid/oid/iss, then link, sync-hit, or bootstrap.
#endregion

#region Design
// RFC 219 fork 1 + D4 + D10. HTTP Challenge/OnTicketReceived stay in the server adapter; this type
// is the store-side decision. Link requires the caller's PrincipalId (D4: identity-session is
// enough — no passkey re-assert). Duplicate handle is 409 with no foreign-id oracle (104-005).
// Sync-hit is Find-by-handle of an active EntraAccount. Revoked rows are non-hits and cannot be
// re-inserted (unique Type+Handle) — 403, not Restore (Graph Restore is a later product path).
// Bootstrap Create is gated by AllowBootstrap AND tid ∈ TrustedTenants. TrustedTenants also
// gates sync-hit so an untrusted tenant never issues a session. First human bootstrap claims
// Administrator the same way CompletePasskeyRegistration does.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

using System.Text;
using TimeWarp.Architecture.Features;

public sealed class EntraTicketProcessor
{
  public const string ModeLink = "link";
  public const string ModeBootstrap = "bootstrap";

  private readonly IPrincipalStore PrincipalStore;
  private readonly IPrincipalRoleStore PrincipalRoleStore;
  private readonly IOptions<EntraAuthenticationOptions> Options;

  public EntraTicketProcessor
  (
    IPrincipalStore principalStore,
    IPrincipalRoleStore principalRoleStore,
    IOptions<EntraAuthenticationOptions> options
  )
  {
    PrincipalStore = principalStore;
    PrincipalRoleStore = principalRoleStore;
    Options = options;
  }

  public async Task<OneOf<PrincipalId, SharedProblemDetails>> ProcessAsync
  (
    EntraIdTokenClaims claims,
    string mode,
    PrincipalId? linkCallerPrincipalId,
    CancellationToken cancellationToken
  )
  {
    if (!IssuerMatchesTenant(claims))
    {
      return IdentityProblems.InvalidEntraToken();
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

  public static bool IsTrustedTenant(Guid tenantId, EntraAuthenticationOptions options)
  {
    ArgumentNullException.ThrowIfNull(options);
    foreach (string entry in options.TrustedTenants)
    {
      if (Guid.TryParse(entry, out Guid trusted) && trusted == tenantId)
      {
        return true;
      }
    }

    return false;
  }

  private static bool IssuerMatchesTenant(EntraIdTokenClaims claims)
  {
    string expected = Encoding.UTF8.GetString(EntraIssuerMaterial.FromTenantId(claims.TenantId));
    return string.Equals(claims.Issuer, expected, StringComparison.Ordinal);
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
    EntraAuthenticationOptions options = Options.Value;
    if (!IsTrustedTenant(claims.TenantId, options))
    {
      return IdentityProblems.UntrustedTenant();
    }

    if (existing is { IsRevoked: false })
    {
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
      return IdentityProblems.EntraCredentialRevoked();
    }

    if (!options.AllowBootstrap)
    {
      return IdentityProblems.BootstrapNotAllowed();
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
      return IdentityProblems.CredentialAlreadyRegistered("Entra account");
    }

    _ = await PrincipalRoleStore.TryClaimFirstAdministratorAsync(created.Id, cancellationToken);
    return created.Id;
  }
}
