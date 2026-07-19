#region Purpose
// Endpoint-centric contract for completing a WebAuthn passkey registration ceremony: the browser's
// answer to StartPasskeyRegistration's options.
#endregion

#region Design
// All three fields are base64url-encoded binary (CredentialId, ClientDataJson, AttestationObject) —
// FluentValidation here only enforces presence and a coarse size ceiling on the largest/most
// variable field (AttestationObject, 64KB); actual base64url well-formedness and CBOR/structural
// validity are checked by the handler via WebAuthnChallengeReader/WebAuthnRegistration.Verify, not
// by a regex here (per the web-api-contracts skill: format validation belongs in the
// handler/library, not FluentValidation regex, for payloads this shape-sensitive).
// No GetMockResponseFactory — see StartPasskeyRegistration's Design region.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

public static partial class CompletePasskeyRegistration
{
  [ApiRoute("api/identity/passkey/register", HttpVerb.Post)]
  public sealed partial class Command : IApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>
  {
    public string CredentialId { get; set; } = null!;
    public string ClientDataJson { get; set; } = null!;
    public string AttestationObject { get; set; } = null!;
  }

  public sealed class Validator : AbstractValidator<Command>
  {
    public Validator()
    {
      RuleFor(x => x.CredentialId).NotEmpty();
      RuleFor(x => x.ClientDataJson).NotEmpty();
      RuleFor(x => x.AttestationObject).NotEmpty().MaximumLength(64 * 1024);
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
