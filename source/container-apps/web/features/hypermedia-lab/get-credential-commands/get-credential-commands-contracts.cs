#region Purpose
// Hypermedia lab, approach C (task 275): the caller's credentials plus Siren-style link commands the server says are valid now.
#endregion

#region Design
// Evaluation-only contract, removed with the lab once Steve decides (see GetCredentialOffers).
// Approach C ships links, not names: each LinkCommand is { Rel, Label, Subject, Method, Href, Body,
// Fields }. The SPA's FollowCommand handler is a generic interpreter — it does not know what
// "revoke" means; it sends Method + Href with Body (plus the user's Fields) through the same
// IWebServerApiService pipeline as every typed call, then follows Self to refresh. Commands point at
// the EXISTING typed endpoints (RevokeCredential / RenameCredential routes, the ChallengeEntra
// browser endpoint) — no lab-only write endpoints, no second implementation.
// Method is POST (an API call answered with JSON) or NAVIGATE (a full-page browser navigation —
// Link Microsoft 365 is an auth challenge redirect, which an XHR cannot follow). Hrefs are always
// app-relative ("/api/…"); the client refuses anything else (absolute, protocol-relative, scheme),
// so a tampered payload cannot send the bearer token or the user to another origin.
// Body is the request body template the server already knows: the existing commands are
// IAuthApiRequest, whose validator demands a non-empty UserId, so the server writes the caller's
// principal id there (the value the server then ignores — the mock/client identity signal leaks
// into hypermedia; recorded in the task comparison). Fields names body properties the user must
// supply (Rename's nickname); the palette skips commands that have any.
// Self is this resource's own href, the "follow-up payload" every command refreshes from.
// The handler computes the set from the shared Identity CredentialRules, exactly like
// GetCredentialOffers; both lab reads stay in step by construction.
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
public static partial class GetCredentialCommands
{
  [ApiRoute("api/hypermedia-lab/credential-commands", HttpVerb.Get)]
  public sealed partial class Query : IApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>;

  public sealed class Validator : AbstractValidator<Query>;

  [CrossSliceReference(typeof(GetCredentials), "The lab lists the real credentials with the GetCredentials summary shape (task 275 evaluation).")]
  public sealed class Response
  {
    public IReadOnlyList<GetCredentials.CredentialSummary> Credentials { get; }
    public IReadOnlyList<LinkCommand> Commands { get; }
    /// <summary>App-relative href of this resource; every command refreshes from it.</summary>
    public string Self { get; }

    public Response(IReadOnlyList<GetCredentials.CredentialSummary> credentials, IReadOnlyList<LinkCommand> commands, string self)
    {
      Credentials = Guard.Against.Null(credentials);
      Commands = Guard.Against.Null(commands);
      Self = Guard.Against.NullOrWhiteSpace(self);
    }
  }

  /// <summary>One command the server offers now: what to send where.</summary>
  public sealed class LinkCommand
  {
    /// <summary>Relation name (<see cref="LinkCommandRels"/>); display/test vocabulary only.</summary>
    public string Rel { get; }
    public string Label { get; }
    /// <summary>Credential id (Guid "D") the command applies to; null for a page-level command.</summary>
    public string? Subject { get; }
    /// <summary><see cref="LinkCommandMethods.Post"/> or <see cref="LinkCommandMethods.Navigate"/>.</summary>
    public string Method { get; }
    /// <summary>App-relative href ("/api/…"); anything else is refused by the client.</summary>
    public string Href { get; }
    /// <summary>Request body template (POST); null when the command sends no body.</summary>
    public IReadOnlyDictionary<string, JsonElement>? Body { get; }
    /// <summary>Body properties the user supplies (string values).</summary>
    public IReadOnlyList<string> Fields { get; }

    public LinkCommand
    (
      string rel,
      string label,
      string? subject,
      string method,
      string href,
      IReadOnlyDictionary<string, JsonElement>? body,
      IReadOnlyList<string> fields
    )
    {
      Rel = Guard.Against.NullOrWhiteSpace(rel);
      Label = Guard.Against.NullOrWhiteSpace(label);
      Subject = subject;
      Method = Guard.Against.NullOrWhiteSpace(method);
      Href = Guard.Against.NullOrWhiteSpace(href);
      Body = body;
      Fields = Guard.Against.Null(fields);
    }
  }

  public static class LinkCommandMethods
  {
    public const string Post = "POST";
    public const string Navigate = "NAVIGATE";
  }

  public static class LinkCommandRels
  {
    public const string Revoke = "revoke";
    public const string Rename = "rename";
    public const string LinkMicrosoft365 = "link-microsoft-365";

    public const string NicknameField = "nickname";
    public const string UserIdField = "userId";
  }

  public static MockResponseFactory<Response> GetMockResponseFactory()
  {
    return _ =>
    {
      var first = CredentialId.New();
      var second = CredentialId.New();
      var userId = Guid.NewGuid();
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
          Revoke(first, userId),
          Rename(first, userId),
          Revoke(second, userId),
          Rename(second, userId)
        ],
        SelfHref
      );
    };
  }

  /// <summary>This resource's app-relative href.</summary>
  public static string SelfHref => AppRelative(new Query().GetRoute());

  /// <summary>POST to the existing RevokeCredential endpoint for one credential.</summary>
  public static LinkCommand Revoke(CredentialId credentialId, Guid userId) =>
    new
    (
      LinkCommandRels.Revoke,
      "Revoke",
      credentialId.Value.ToString("D"),
      LinkCommandMethods.Post,
      AppRelative(new RevokeCredential.Command { CredentialId = credentialId.Value }.GetRoute()),
      UserIdBody(userId),
      []
    );

  /// <summary>POST to the existing RenameCredential endpoint; the user supplies the nickname.</summary>
  public static LinkCommand Rename(CredentialId credentialId, Guid userId) =>
    new
    (
      LinkCommandRels.Rename,
      "Rename",
      credentialId.Value.ToString("D"),
      LinkCommandMethods.Post,
      AppRelative(new RenameCredential.Command { CredentialId = credentialId.Value }.GetRoute()),
      UserIdBody(userId),
      [LinkCommandRels.NicknameField]
    );

  /// <summary>Full-page navigation to the Entra link challenge, returning to <paramref name="returnPath"/>.</summary>
  public static LinkCommand LinkMicrosoft365(string returnPath) =>
    new
    (
      LinkCommandRels.LinkMicrosoft365,
      "Link Microsoft 365",
      subject: null,
      LinkCommandMethods.Navigate,
      ChallengeEntra.GetRoute("link", returnPath),
      body: null,
      []
    );

  private static Dictionary<string, JsonElement> UserIdBody(Guid userId) =>
    new() { [LinkCommandRels.UserIdField] = JsonSerializer.SerializeToElement(userId) };

  private static string AppRelative(string route) =>
    route.StartsWith('/') ? route : "/" + route;
}
