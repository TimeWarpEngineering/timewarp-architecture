#region Purpose
// Endpoint-centric contract for attaching an additional passkey to the CALLER's existing principal —
// task 104-005's "add credential" requirement (phone + laptop passkeys on one account).
#endregion

#region Design
// Shape mirrors CompletePasskeyRegistration exactly (same three base64url fields, same size caps —
// see that contract's Design region for the byte-size rationale) plus an optional Nickname the caller
// can attach for their own recognition (e.g. "MacBook") — task 248-001 renamed this from Label: the
// provider name (Credential.Label, AAGUID map) and the user's nickname are now separate fields, so a
// caller-supplied name no longer overwrites the provider — and the two registration-context hints
// the browser exposes (AuthenticatorAttachment from PublicKeyCredential.authenticatorAttachment,
// Transports from AuthenticatorAttestationResponse.getTransports()); both optional, display-only,
// reduced server-side by RegistrationContext. The KEY difference is authentication and
// audience: CompletePasskeyRegistration is anonymous and mints a brand-new Principal, this command is
// authenticated ([EndpointAuthorize], credential-management policy) and attaches to the CALLER's
// EXISTING principal — the handler sources the principal id from ICurrentPrincipalAccessor and never
// calls Principal.Create. UserId is a client/mock-mode identity signal only (see GetCredentials'
// Design region) — the server never trusts it; the accessor is what actually decides whose principal
// gets the new credential.
// Reuses StartPasskeyRegistration's existing ANONYMOUS challenge-minting endpoint rather than adding
// a dedicated authenticated Start ceremony (task 104-005 scope boundary) — StartPasskeyRegistration
// is side-effect-free (mints options/a challenge, creates nothing), so there is nothing
// security-sensitive about a signed-in caller using the same minting endpoint an anonymous
// registration flow uses; the SECURITY-sensitive half (attaching the resulting credential to a
// principal) only happens in this authenticated command's handler. Task 253: the start MUST set
// StartPasskeyRegistration.ForCurrentAccount so the WebAuthn user name is the caller's own account
// name (read from the session there, never from this command's UserId); the handler refuses a
// new-account challenge with 400 ChallengeInvalid.
// Wave-1 simplification (documented, not an oversight): the reused Start endpoint does not
// populate WebAuthn's excludeCredentials with the caller's
// existing credential ids — a browser could technically be prompted to re-register a passkey it
// already has bound to this account (the handler's FindCredentialByHandleAsync check catches this as
// a 409, it just is not prevented client-side with a nicer UX). Follow-up, not blocking.
// Response returns the new CredentialId plus the resolved ProviderLabel so the client can immediately
// open the nickname prompt pre-filled with the provider name (task 248-001) without an extra
// GetCredentials round-trip.
// [EndpointAuthorize] (task 182-006): PermissionIds.CredentialManageSelf dual scheme — see
// GetCredentials' Design region.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

[ApiEndpoint]
[EndpointAuthorize
(
  Policy = PermissionIds.CredentialManageSelf,
  AuthenticationSchemes = AuthenticationSchemeNames.IdentitySession + "," + AuthenticationSchemeNames.AgentToken
)]
public static partial class AddPasskey
{
  [ApiRoute("api/identity/credentials/passkey", HttpVerb.Post)]
  public sealed partial class Command : IAuthApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>
  {
    public Guid UserId { get; set; }
    public string CredentialId { get; set; } = null!;
    public string ClientDataJson { get; set; } = null!;
    public string AttestationObject { get; set; } = null!;
    public string? Nickname { get; set; }
    /// <summary>"platform" / "cross-platform" from the browser, or null when not reported.</summary>
    public string? AuthenticatorAttachment { get; set; }
    /// <summary>Attestation response transports ("internal", "hybrid", "usb", …), or null.</summary>
    public IReadOnlyList<string>? Transports { get; set; }
  }

  public sealed class Validator : AbstractValidator<Command>
  {
    public Validator()
    {
      RuleFor(x => x.CredentialId).NotEmpty().MaximumLength(2 * 1024);
      RuleFor(x => x.ClientDataJson).NotEmpty().MaximumLength(64 * 1024);
      RuleFor(x => x.AttestationObject).NotEmpty().MaximumLength(64 * 1024);
      RuleFor(x => x.Nickname).MaximumLength(Credential.MaxNicknameLength);
      RuleFor(x => x.AuthenticatorAttachment).MaximumLength(32);
      RuleFor(x => x.Transports).Must(transports => transports is null || transports.Count <= 8)
        .WithMessage("Transports cannot list more than 8 entries.");
      RuleForEach(x => x.Transports).NotEmpty().MaximumLength(32);
      RuleFor(x => x).SetValidator(new AuthApiRequestValidator());
    }
  }

  public sealed class Response
  {
    public CredentialId CredentialId { get; }
    /// <summary>Resolved provider name (AAGUID map), or null when unknown — the nickname prompt's prefill.</summary>
    public string? ProviderLabel { get; }

    public Response(CredentialId credentialId, string? providerLabel = null)
    {
      if (credentialId.IsEmpty)
      {
        throw new ArgumentException("CredentialId cannot be empty.", nameof(credentialId));
      }

      CredentialId = credentialId;
      ProviderLabel = providerLabel;
    }
  }

  // No GetMockResponseFactory — same rationale as CompletePasskeyRegistration/
  // StartPasskeyRegistration: a real ceremony cannot be meaningfully mocked without a browser
  // credential to answer it.
}
