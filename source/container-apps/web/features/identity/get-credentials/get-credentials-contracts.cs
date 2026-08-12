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
    public string? Label { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset? RevokedAt { get; }
    public bool IsActive { get; }

    public CredentialSummary
    (
      CredentialId id,
      CredentialType type,
      string? label,
      DateTimeOffset createdAt,
      DateTimeOffset? revokedAt,
      bool isActive
    )
    {
      if (id.IsEmpty)
      {
        throw new ArgumentException("Id cannot be empty.", nameof(id));
      }

      Id = id;
      Type = type;
      Label = label;
      CreatedAt = createdAt;
      RevokedAt = revokedAt;
      IsActive = isActive;
    }
  }

  public static MockResponseFactory<Response> GetMockResponseFactory()
  {
    return _ => new Response
    (
      [
        new CredentialSummary(CredentialId.New(), CredentialType.Passkey, "laptop", DateTimeOffset.UtcNow.AddDays(-30), revokedAt: null, isActive: true),
        new CredentialSummary(CredentialId.New(), CredentialType.Passkey, "phone", DateTimeOffset.UtcNow.AddDays(-7), revokedAt: null, isActive: true)
      ]
    );
  }
}
