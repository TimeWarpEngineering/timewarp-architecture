#region Purpose
// Endpoint-centric contract for completing a WebAuthn passkey authentication ceremony: the
// browser's answer to StartPasskeyAuthentication's options.
#endregion

#region Design
// UserHandle is intentionally unused by the handler: account resolution is credential-handle-based
// (IPrincipalStore.FindCredentialByHandleAsync via CredentialId), not userHandle-based, because this
// template never persists the WebAuthn userHandle it minted at options time (see
// StartPasskeyRegistration's Design region — user.id is opaque and discarded after the ceremony).
// The property still exists on the contract because the browser's
// PublicKeyCredential.toJSON()/AuthenticatorAssertionResponse always includes a userHandle field for
// discoverable credentials; declaring it here documents that it travels but is deliberately ignored,
// rather than silently dropping a field the client sends.
// No GetMockResponseFactory — see StartPasskeyRegistration's Design region.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

public static partial class CompletePasskeyAuthentication
{
  [ApiRoute("api/identity/passkey/authenticate", HttpVerb.Post)]
  public sealed partial class Command : IApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>
  {
    public string CredentialId { get; set; } = null!;
    public string ClientDataJson { get; set; } = null!;
    public string AuthenticatorData { get; set; } = null!;
    public string Signature { get; set; } = null!;

    /// <summary>Unused by the handler — see Design region.</summary>
    public string? UserHandle { get; set; }
  }

  public sealed class Validator : AbstractValidator<Command>
  {
    public Validator()
    {
      RuleFor(x => x.CredentialId).NotEmpty();
      RuleFor(x => x.ClientDataJson).NotEmpty();
      RuleFor(x => x.AuthenticatorData).NotEmpty();
      RuleFor(x => x.Signature).NotEmpty();
    }
  }

  public sealed class Response
  {
    public PrincipalId PrincipalId { get; }

    public Response(PrincipalId principalId)
    {
      if (principalId.IsEmpty)
      {
        throw new ArgumentException("PrincipalId cannot be empty.", nameof(principalId));
      }

      PrincipalId = principalId;
    }
  }
}
