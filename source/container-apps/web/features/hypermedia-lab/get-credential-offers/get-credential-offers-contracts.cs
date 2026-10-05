#region Purpose
// Hypermedia lab, approach B (task 275): the caller's credentials plus the catalog actions the server says are valid now.
#endregion

#region Design
// Evaluation-only contract. The lab is removed (or folded into Identity) once Steve picks B, C or
// neither; it must not ship in a template release as-is.
// Approach B names CLIENT catalog actions ([CatalogAction] names such as
// "Credentials.RevokeCredential") with positional-by-name Arguments. The catalog is the allow-list:
// the server can only point at actions the SPA already has, and the SPA binds Arguments against the
// entry's ActionCatalogParameter list (ContextualActionArguments) and fails closed on an unknown
// name or a binding failure. OfferedActionNames is the server's whole vocabulary — every name it can
// emit is a constant here, and a web-spa test resolves each one in the real IActionCatalog, so a
// renamed action breaks a test instead of silently vanishing from the offer.
// Arguments are JsonElement values keyed by the action constructor's parameter name (camelCase, the
// catalog's parameter names). An offer may leave a required parameter unbound (Rename leaves
// nickname to the user); the client renders an input for it and the palette skips such rows.
// Subject names the credential row an offer applies to (null = page-level, Link Microsoft 365) —
// a display hint so the page can put the button on the right row without reading Arguments.
// Credentials reuse GetCredentials.CredentialSummary (CrossSliceReference): same data, same
// Handle/PublicMaterial omission, no second shape. The handler computes the set from the shared
// Identity CredentialRules — the predicates RevokeCredential.Handler and the Entra link path enforce.
// IApiRequest, not IAuthApiRequest: the server never trusts a client UserId on these reads (the
// caller comes from the session), and a plain GET keeps the lab's self link followable by a generic
// client. [EndpointAuthorize] matches GetCredentials (credential.manage.self, dual scheme), so an
// agent with credential:manage gets the same offer — the surface task 271 could reuse.
#endregion

namespace TimeWarp.Architecture.Features.HypermediaLab;

using System.Text.Json;
using TimeWarp.Architecture.Features.Identity;

[ApiEndpoint]
[EndpointAuthorize
(
  Policy = PermissionIds.CredentialManageSelf,
  AuthenticationSchemes = AuthenticationSchemeNames.IdentitySession + "," + AuthenticationSchemeNames.AgentToken
)]
[CrossSliceReference(typeof(GetCredentials), "The lab reuses Identity's auth schemes and credential summary shape (task 275 evaluation).")]
public static partial class GetCredentialOffers
{
  [ApiRoute("api/hypermedia-lab/credential-offers", HttpVerb.Get)]
  public sealed partial class Query : IApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>;

  public sealed class Validator : AbstractValidator<Query>;

  [CrossSliceReference(typeof(GetCredentials), "The lab lists the real credentials with the GetCredentials summary shape (task 275 evaluation).")]
  public sealed class Response
  {
    public IReadOnlyList<GetCredentials.CredentialSummary> Credentials { get; }
    public IReadOnlyList<OfferedAction> Offers { get; }

    public Response(IReadOnlyList<GetCredentials.CredentialSummary> credentials, IReadOnlyList<OfferedAction> offers)
    {
      Credentials = Guard.Against.Null(credentials);
      Offers = Guard.Against.Null(offers);
    }
  }

  /// <summary>One catalog action the server offers now, with the arguments it binds.</summary>
  public sealed class OfferedAction
  {
    /// <summary>Catalog name (<see cref="OfferedActionNames"/>).</summary>
    public string Name { get; }
    public string Label { get; }
    /// <summary>Credential id (Guid "D") the offer applies to; null for a page-level offer.</summary>
    public string? Subject { get; }
    /// <summary>Arguments keyed by catalog parameter name; unbound required parameters come from the user.</summary>
    public IReadOnlyDictionary<string, JsonElement> Arguments { get; }

    public OfferedAction(string name, string label, string? subject, IReadOnlyDictionary<string, JsonElement> arguments)
    {
      Name = Guard.Against.NullOrWhiteSpace(name);
      Label = Guard.Against.NullOrWhiteSpace(label);
      Subject = subject;
      Arguments = Guard.Against.Null(arguments);
    }
  }

  /// <summary>Every catalog name the server may offer, with the argument keys it binds.</summary>
  public static class OfferedActionNames
  {
    public const string RevokeCredential = "Credentials.RevokeCredential";
    public const string RenameCredential = "Credentials.RenameCredential";
    public const string LinkMicrosoft365 = "Credentials.LinkMicrosoft365";

    public const string CredentialIdArgument = "credentialId";

    public static IReadOnlyList<string> All { get; } = [RevokeCredential, RenameCredential, LinkMicrosoft365];
  }

  public static MockResponseFactory<Response> GetMockResponseFactory()
  {
    return _ =>
    {
      var first = CredentialId.New();
      var second = CredentialId.New();
      return new Response
      (
        [
          new GetCredentials.CredentialSummary
          (
            first,
            CredentialType.Passkey,
            "1Password",
            "Work laptop",
            DateTimeOffset.UtcNow.AddDays(-30),
            revokedAt: null,
            isActive: true,
            new RegisteredWith(AuthenticatorAttachment.Platform, "Chrome", "Windows"),
            "3f9a1c2e"
          ),
          new GetCredentials.CredentialSummary
          (
            second,
            CredentialType.Passkey,
            "1Password",
            nickname: null,
            DateTimeOffset.UtcNow.AddDays(-7),
            revokedAt: null,
            isActive: true,
            new RegisteredWith(AuthenticatorAttachment.CrossPlatform, "Safari", "iOS"),
            "b71e04dd"
          )
        ],
        [
          ForCredential(OfferedActionNames.RevokeCredential, "Revoke", first),
          ForCredential(OfferedActionNames.RenameCredential, "Rename", first),
          ForCredential(OfferedActionNames.RevokeCredential, "Revoke", second),
          ForCredential(OfferedActionNames.RenameCredential, "Rename", second)
        ]
      );
    };
  }

  /// <summary>An offer bound to one credential: <c>{ "credentialId": "…" }</c>.</summary>
  public static OfferedAction ForCredential(string name, string label, CredentialId credentialId)
  {
    string subject = credentialId.Value.ToString("D");
    return new OfferedAction
    (
      name,
      label,
      subject,
      new Dictionary<string, JsonElement>
      {
        [OfferedActionNames.CredentialIdArgument] = JsonSerializer.SerializeToElement(subject)
      }
    );
  }
}
