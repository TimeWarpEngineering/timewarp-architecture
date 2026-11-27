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
// Every base64url field carries a MaximumLength ceiling, matching CompletePasskeyRegistration's
// treatment (a round-1 review caught this command capping nothing at all): CredentialId/UserHandle
// at 2KB (spec caps raw credential/user ids well under that), ClientDataJson/AuthenticatorData/
// Signature at 64KB (coarse DoS-shaped ceiling, not a realistic-size estimate — see
// CompletePasskeyRegistration's Design region for the same rationale).
// No GetMockResponseFactory — see StartPasskeyRegistration's Design region.
// [EndpointAllowAnonymous] (task 110): this IS the request that establishes the session (on
// success) — nothing to authorize against yet. A rejected assertion never reaches a 401/403 auth
// challenge; it returns a SharedProblemDetails, since there is no prior session here to challenge
// against.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

[ApiEndpoint]
[EndpointAllowAnonymous("This is the request that establishes the session on success — nothing to authorize against yet.")]
public static partial class CompletePasskeyAuthentication
{
  [ApiRoute("api/identity/passkey/authenticate", HttpVerb.Post)]
  [Operation("passkey-authentication.complete", Description = "Verify a passkey assertion and sign the user in.", Group = "identity")]
  public sealed partial class Command : IApiRequest, ICommand<OneOf<Response, SharedProblemDetails>>
  {
    /// <summary>The credential id, base64url, as returned by the browser.</summary>
    public string CredentialId { get; set; } = null!;
    /// <summary>The base64url clientDataJSON from the browser's WebAuthn response.</summary>
    public string ClientDataJson { get; set; } = null!;
    /// <summary>The base64url authenticatorData from the browser's WebAuthn assertion.</summary>
    public string AuthenticatorData { get; set; } = null!;
    /// <summary>The base64url assertion signature from the browser.</summary>
    public string Signature { get; set; } = null!;

    /// <summary>Unused by the handler — see Design region.</summary>
    public string? UserHandle { get; set; }
  }

  public sealed class Validator : AbstractValidator<Command>
  {
    public Validator()
    {
      RuleFor(x => x.CredentialId).NotEmpty().MaximumLength(2 * 1024);
      RuleFor(x => x.ClientDataJson).NotEmpty().MaximumLength(64 * 1024);
      RuleFor(x => x.AuthenticatorData).NotEmpty().MaximumLength(64 * 1024);
      RuleFor(x => x.Signature).NotEmpty().MaximumLength(64 * 1024);
      RuleFor(x => x.UserHandle).MaximumLength(2 * 1024);
    }
  }

  public sealed class Response
  {
    /// <summary>The id of the signed-in principal.</summary>
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
