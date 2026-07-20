#region Purpose
// Endpoint-centric contract for completing a WebAuthn passkey registration ceremony: the browser's
// answer to StartPasskeyRegistration's options.
#endregion

#region Design
// All three fields are base64url-encoded binary (CredentialId, ClientDataJson, AttestationObject) —
// FluentValidation here only enforces presence and a coarse size ceiling; actual base64url
// well-formedness and CBOR/structural validity are checked by the handler via
// WebAuthnChallengeReader/WebAuthnRegistration.Verify, not by a regex here (per the
// web-api-contracts skill: format validation belongs in the handler/library, not FluentValidation
// regex, for payloads this shape-sensitive).
// Every base64url field carries a MaximumLength ceiling, not just AttestationObject (a round-1
// review caught the original CredentialId/ClientDataJson being left uncapped): CredentialId at 2KB
// (the spec caps raw credential ids at 1023 bytes, ~1366 base64url chars — 2KB rounds up with
// headroom) and ClientDataJson/AttestationObject at 64KB (real payloads are a few hundred bytes to
// a couple KB; 64KB is a coarse DoS-shaped ceiling, not a realistic-size estimate).
// No GetMockResponseFactory — see StartPasskeyRegistration's Design region.
// [EndpointAllowAnonymous] (task 110): this IS the request that creates the session (on success) —
// nothing to authorize against yet.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

[ApiEndpoint]
[EndpointAllowAnonymous("This is the request that creates the session on success — nothing to authorize against yet.")]
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
      RuleFor(x => x.CredentialId).NotEmpty().MaximumLength(2 * 1024);
      RuleFor(x => x.ClientDataJson).NotEmpty().MaximumLength(64 * 1024);
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
