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
// Task 248-001: AuthenticatorAttachment / Transports are the browser's optional registration-context
// hints (same shape and caps as AddPasskey); Response also returns the minted CredentialId and the
// resolved ProviderLabel so the Passkeys page can open the nickname prompt pre-filled with the
// provider name right after the first-credential ceremony.
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
      RuleFor(x => x.AuthenticatorAttachment).MaximumLength(32);
      RuleFor(x => x.Transports).Must(transports => transports is null || transports.Count <= 8)
        .WithMessage("Transports cannot list more than 8 entries.");
      RuleForEach(x => x.Transports).NotEmpty().MaximumLength(32);
    }
  }

  public sealed class Response
  {
    public PrincipalId PrincipalId { get; }
    /// <summary>The minted credential; null-free so the client can open the nickname prompt for it.</summary>
    public CredentialId CredentialId { get; }
    /// <summary>Resolved provider name (AAGUID map), or null when unknown — the nickname prompt's prefill.</summary>
    public string? ProviderLabel { get; }

    public Response(PrincipalId principalId, CredentialId credentialId, string? providerLabel = null)
    {
      if (principalId.IsEmpty)
      {
        throw new ArgumentException("PrincipalId cannot be empty.", nameof(principalId));
      }

      if (credentialId.IsEmpty)
      {
        throw new ArgumentException("CredentialId cannot be empty.", nameof(credentialId));
      }

      PrincipalId = principalId;
      CredentialId = credentialId;
      ProviderLabel = providerLabel;
    }
  }
}
