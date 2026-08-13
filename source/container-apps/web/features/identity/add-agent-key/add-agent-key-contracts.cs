#region Purpose
// Endpoint-centric contract for attaching an additional agent public key to the CALLER's existing
// principal — task 104-005's "add credential" requirement, the agent-key analog of AddPasskey; also
// how agent key ROTATION works (add the new key, then RevokeCredential the old one).
#endregion

#region Design
// Shape mirrors CompleteAgentKeyRegistration exactly (same three base64url fields plus Label, same
// size caps — see that contract's Design region for the byte-size rationale). The key difference is
// authentication and audience: CompleteAgentKeyRegistration is anonymous and mints a brand-new
// Principal (no sponsor required, by design); this command is authenticated ([EndpointAuthorize],
// PermissionIds.CredentialManageSelf — an agent token needs the credential:manage scope expanded
// via AgentScopePermissionSeed) and attaches to the CALLER's EXISTING principal — the
// handler sources the principal id from ICurrentPrincipalAccessor and never calls Principal.Create.
// UserId is a client/mock-mode identity signal only (see GetCredentials' Design region) — the server
// never trusts it.
// Reuses StartAgentKeyRegistration's existing ANONYMOUS challenge-minting endpoint (same rationale as
// AddPasskey's Design region — the Start endpoint is side-effect-free, so an authenticated caller
// reusing it introduces no new security surface; the sensitive half is this command's handler).
// Response returns the new CredentialId AND KeyId (server-computed SHA-256 of the registered SPKI
// bytes, same as CompleteAgentKeyRegistration's Response) — the agent needs KeyId to later request a
// bearer token for this specific key via CompleteAgentTokenIssuance, exactly as it would for its
// first key.
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
public static partial class AddAgentKey
{
  [ApiRoute("api/identity/credentials/agent-key", HttpVerb.Post)]
  public sealed partial class Command : IAuthApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>
  {
    public Guid UserId { get; set; }
    public string PublicKey { get; set; } = null!;
    public string Challenge { get; set; } = null!;
    public string Signature { get; set; } = null!;
    public string? Label { get; set; }
  }

  public sealed class Validator : AbstractValidator<Command>
  {
    public Validator()
    {
      RuleFor(x => x.PublicKey).NotEmpty().MaximumLength(2 * 1024);
      RuleFor(x => x.Challenge).NotEmpty().MaximumLength(256);
      RuleFor(x => x.Signature).NotEmpty().MaximumLength(1024);
      RuleFor(x => x.Label).MaximumLength(64);
      RuleFor(x => x).SetValidator(new AuthApiRequestValidator());
    }
  }

  public sealed class Response
  {
    public CredentialId CredentialId { get; }
    public string KeyId { get; }

    public Response(CredentialId credentialId, string keyId)
    {
      if (credentialId.IsEmpty)
      {
        throw new ArgumentException("CredentialId cannot be empty.", nameof(credentialId));
      }

      CredentialId = credentialId;
      KeyId = Guard.Against.NullOrEmpty(keyId);
    }
  }

  // No GetMockResponseFactory — same rationale as CompleteAgentKeyRegistration/
  // StartAgentKeyRegistration: a real ceremony cannot be meaningfully mocked without a real keypair
  // to answer it.
}
