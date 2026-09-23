#region Purpose
// Endpoint-centric contract for listing the CALLER's own credentials (passkeys + agent keys) —
// task 104-005's read half of the credential-management surface.
#endregion

#region Design
// Uses [AuthApiRequest] (not the manual interface) so the source generator emits UserId +
// GetAuthQueryParameters() for query-string composition (this is a GET with no body); IncludeRevoked
// is a hand-declared bool merged into the same query string, matching GetRoles.cs's pattern of
// merging a generated parameter set with an additional one. UserId here is a client/mock-mode
// identity signal ONLY (see the web-api-contracts skill's three-state truth table) — the server
// NEVER trusts it; the handler resolves the real caller via ICurrentPrincipalAccessor and ignores
// this field entirely. Load-bearing IDOR rule (task 104-005): whose credentials this endpoint
// returns is decided ENTIRELY server-side — there is no "list someone else's credentials" shape to
// even request.
// CredentialSummary deliberately omits Handle and PublicMaterial (Credential's secret-ish binary
// material) — see Credential.cs's Design region for why those exist at all (lookup key / verification
// material); a list-your-own-credentials endpoint has no reason to ever put either on the wire, and
// doing so would hand a client a copy of authentication material it should never see even for its
// own credentials. Pinned by a json.ShouldNotContain assertion in the contract round-trip test, not
// just by this comment.
// IsActive is the wire-friendly derived flag (!IsRevoked) rather than re-deriving "active" from
// RevokedAt on the client — same "derive server-side, ship the answer" reasoning as other read
// contracts in this feature.
// Task 248-001 discriminators: Nickname (user-chosen, RenameCredential), Label (provider name —
// AAGUID map / Entra display — immutable), RegisteredWith (attachment + browser/OS FAMILY captured at
// registration; never a raw User-Agent), and Fingerprint — the last 8 hex of SHA-256(handle),
// computed server-side (CredentialFingerprint). Fingerprint is the ONLY thing derived from the
// handle that may cross the wire: it is one-way and 32 bits, so it discriminates rows without
// disclosing material. The reflection + json.ShouldNotContain pins on Handle/PublicMaterial stay.
// Task 248-002: LastUsedAt (nullable; null = never used) is the server's stamp from
// Credential.LastUsedAt; the SPA renders it relative ("Last used 3 minutes ago" / "Never used") and
// restates it in the revoke confirmation. It is the last ctor parameter and defaults to null so the
// mock factory's second row is honest about a never-used passkey.
// [EndpointAuthorize] (task 182-006): PermissionIds.CredentialManageSelf via IPermissionEvaluator.
// Dual schemes (identity-session + agent-token): humans get the grant from SelfServicePermissions;
// agents need scope credential:manage → AgentScopePermissionSeed. [AuthApiRequest] on the Query
// remains client/mock identity signal only.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

[ApiEndpoint]
[EndpointAuthorize
(
  Policy = PermissionIds.CredentialManageSelf,
  AuthenticationSchemes = AuthenticationSchemeNames.IdentitySession + "," + AuthenticationSchemeNames.AgentToken
)]
public static partial class GetCredentials
{
  [ApiRoute("api/identity/credentials", HttpVerb.Get)]
  [AuthApiRequest]
  public sealed partial class Query : IQueryStringRouteProvider, IRequest<OneOf<Response, SharedProblemDetails>>
  {
    public bool IncludeRevoked { get; set; }

    public string GetRouteWithQueryString()
    {
      var collection = new NameValueCollection
      {
        GetAuthQueryParameters(),
        { nameof(IncludeRevoked), IncludeRevoked.ToString() }
      };
      return $"{GetRoute()}?{this.GetQueryString(collection)}";
    }
  }

  public sealed class Validator : AbstractValidator<Query>
  {
    public Validator()
    {
      RuleFor(x => x).SetValidator(new AuthApiRequestValidator());
    }
  }

  public sealed class Response
  {
    public IReadOnlyList<CredentialSummary> Credentials { get; }

    public Response(IReadOnlyList<CredentialSummary> credentials)
    {
      Credentials = Guard.Against.Null(credentials);
    }
  }

  public sealed class CredentialSummary
  {
    public CredentialId Id { get; }
    public CredentialType Type { get; }
    /// <summary>Provider label (AAGUID / Entra display); null when unknown.</summary>
    public string? Label { get; }
    /// <summary>User-chosen nickname; null until renamed.</summary>
    public string? Nickname { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset? RevokedAt { get; }
    public bool IsActive { get; }
    /// <summary>Registration context (attachment, browser family, OS family).</summary>
    public RegisteredWith RegisteredWith { get; }
    /// <summary>8 lowercase hex chars derived one-way from the handle; display-only.</summary>
    public string Fingerprint { get; }
    /// <summary>UTC instant of the most recent successful authentication; null when never used.</summary>
    public DateTimeOffset? LastUsedAt { get; }

    public CredentialSummary
    (
      CredentialId id,
      CredentialType type,
      string? label,
      string? nickname,
      DateTimeOffset createdAt,
      DateTimeOffset? revokedAt,
      bool isActive,
      RegisteredWith registeredWith,
      string fingerprint,
      DateTimeOffset? lastUsedAt = null
    )
    {
      if (id.IsEmpty)
      {
        throw new ArgumentException("Id cannot be empty.", nameof(id));
      }

      Id = id;
      Type = type;
      Label = label;
      Nickname = nickname;
      CreatedAt = createdAt;
      RevokedAt = revokedAt;
      IsActive = isActive;
      RegisteredWith = Guard.Against.Null(registeredWith);
      Fingerprint = Guard.Against.NullOrWhiteSpace(fingerprint);
      LastUsedAt = lastUsedAt;
    }
  }

  public static MockResponseFactory<Response> GetMockResponseFactory()
  {
    return _ => new Response
    (
      [
        new CredentialSummary
        (
          CredentialId.New(),
          CredentialType.Passkey,
          "1Password",
          "Work laptop",
          DateTimeOffset.UtcNow.AddDays(-30),
          revokedAt: null,
          isActive: true,
          new RegisteredWith(AuthenticatorAttachment.Platform, "Chrome", "Windows"),
          "3f9a1c2e",
          lastUsedAt: DateTimeOffset.UtcNow.AddHours(-2)
        ),
        new CredentialSummary
        (
          CredentialId.New(),
          CredentialType.Passkey,
          "1Password",
          nickname: null,
          DateTimeOffset.UtcNow.AddDays(-7),
          revokedAt: null,
          isActive: true,
          new RegisteredWith(AuthenticatorAttachment.CrossPlatform, "Safari", "iOS"),
          "b71e04dd"
        )
      ]
    );
  }
}
