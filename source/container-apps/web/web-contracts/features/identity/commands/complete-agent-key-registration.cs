#region Purpose
// Endpoint-centric contract for completing an agent public-key registration ceremony: the agent's
// answer to StartAgentKeyRegistration's challenge.
#endregion

#region Design
// PublicKey is base64url SPKI DER (ECDSA P-256 only — task 104-004 §1); Challenge travels back
// verbatim (see StartAgentKeyRegistration's Design region); Signature is DER (Rfc3279DerSequence)
// over UTF8("TimeWarp.Identity.AgentKey.Register.v1:") ‖ challenge. Label is an optional
// caller-supplied name for the credential (e.g. "prod-worker-3") — cosmetic only, never used for
// lookup (KeyId, server-computed, is the lookup key).
// Every field carries a MaximumLength ceiling from day one (104-003's round-1 review — finding M4 —
// caught the passkey contracts shipping without these; this task starts with them): PublicKey 2KB
// (SPKI DER for P-256 is ~91 bytes; 2KB is a coarse DoS-shaped ceiling, matching AgentPublicKey's own
// internal MaxSpkiLength guard, not a realistic-size estimate), Challenge 256 (32 raw bytes
// base64url-encodes to ~43 chars), Signature 1KB (a DER ECDSA signature over a P-256 curve is at
// most ~72 bytes; 1KB is generous headroom), Label 64.
// Response returns KeyId (base64url SHA-256 of the exact registered SPKI bytes) — the agent MUST
// save this and resend it in CompleteAgentTokenIssuance.Command.KeyId; there is no
// find-by-public-key shortcut at token time (see CompleteAgentTokenIssuance's Design region).
// No GetMockResponseFactory — see StartAgentKeyRegistration's Design region.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

public static partial class CompleteAgentKeyRegistration
{
  [ApiRoute("api/identity/agent/register", HttpVerb.Post)]
  public sealed partial class Command : IApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>
  {
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
    }
  }

  public sealed class Response
  {
    public PrincipalId PrincipalId { get; }
    public string KeyId { get; }

    public Response(PrincipalId principalId, string keyId)
    {
      if (principalId.IsEmpty)
      {
        throw new ArgumentException("PrincipalId cannot be empty.", nameof(principalId));
      }

      PrincipalId = principalId;
      KeyId = Guard.Against.NullOrEmpty(keyId);
    }
  }
}
