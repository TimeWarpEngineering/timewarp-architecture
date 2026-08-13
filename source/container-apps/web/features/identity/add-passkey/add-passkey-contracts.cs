#region Purpose
// Endpoint-centric contract for attaching an additional passkey to the CALLER's existing principal —
// task 104-005's "add credential" requirement (phone + laptop passkeys on one account).
#endregion

#region Design
// Shape mirrors CompletePasskeyRegistration exactly (same three base64url fields, same size caps —
// see that contract's Design region for the byte-size rationale) plus an optional Label the caller
// can attach for their own recognition (e.g. "MacBook"); the KEY difference is authentication and
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
// principal) only happens in this authenticated command's handler.
// Wave-1 simplification (documented, not an oversight): the reused Start endpoint does not know a
// caller is already signed in, so it cannot populate WebAuthn's excludeCredentials with the caller's
// existing credential ids — a browser could technically be prompted to re-register a passkey it
// already has bound to this account (the handler's FindCredentialByHandleAsync check catches this as
// a 409, it just is not prevented client-side with a nicer UX). Follow-up, not blocking.
// Response returns the new CredentialId so the client can immediately show/select it (e.g. to give it
// a label in the UI) without an extra GetCredentials round-trip.
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
    public string? Label { get; set; }
  }

  public sealed class Validator : AbstractValidator<Command>
  {
    public Validator()
    {
      RuleFor(x => x.CredentialId).NotEmpty().MaximumLength(2 * 1024);
      RuleFor(x => x.ClientDataJson).NotEmpty().MaximumLength(64 * 1024);
      RuleFor(x => x.AttestationObject).NotEmpty().MaximumLength(64 * 1024);
      RuleFor(x => x.Label).MaximumLength(64);
      RuleFor(x => x).SetValidator(new AuthApiRequestValidator());
    }
  }

  public sealed class Response
  {
    public CredentialId CredentialId { get; }

    public Response(CredentialId credentialId)
    {
      if (credentialId.IsEmpty)
      {
        throw new ArgumentException("CredentialId cannot be empty.", nameof(credentialId));
      }

      CredentialId = credentialId;
    }
  }

  // No GetMockResponseFactory — same rationale as CompletePasskeyRegistration/
  // StartPasskeyRegistration: a real ceremony cannot be meaningfully mocked without a browser
  // credential to answer it.
}
