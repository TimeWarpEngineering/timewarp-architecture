#region Purpose
// Reports whether the server can call xAI, plus the command that sets the key. Never returns the key.
#endregion

#region Design
// The WASM client cannot see user-secrets. This GET is the only signal Ask uses. Configured is
// true when a key is present, or when the Development/Testing fake upstream is explicitly on.
// SetupCommand is the environment's setup hint. The live handler returns SetupHint. The mock
// factory stays the Development command. The client falls back to DeployedSetupHint when the
// server omits the text, so a blank answer never shows the user-secrets id.
// Model is the configured id when Configured is true, and null otherwise.
// RecordChats, PrivacyNotice, and PrivacyUrl come from XAI configuration (task 347); SupportUrl and
// CredentialLifetimeMinutes are template settings. The notice is shown only when RecordChats is
// true, and PrivacyUrl is null when the app has no privacy page. StarterPrompts (task 348) is the
// normalized XAI:StarterPrompts list, or the template defaults. The key is still never returned.
#endregion

namespace TimeWarp.Architecture.Features.AgentChats;

[System.Diagnostics.CodeAnalysis.SuppressMessage(
  "Design",
  "CA1056:URI-like properties should not be strings",
  Justification = "SupportUrl is a relative app path or an absolute href, copied as text into the anchor.")]
[ApiEndpoint]
[EndpointAuthorize
(
  Policy = XaiChatDefaults.AuthenticatedPolicy,
  AuthenticationSchemes = AuthenticationSchemeNames.IdentitySession + "," + AuthenticationSchemeNames.MockIdentitySession
)]
public static partial class GetAgentChatConfiguration
{
  [ApiRoute("api/agent-chat/configuration", HttpVerb.Get)]
  public sealed partial class Query : IApiRequest, IQuery<OneOf<Response, SharedProblemDetails>>;

  public sealed class Response : BaseResponse
  {
    public bool Configured { get; set; }

    public string SetupCommand { get; set; } = null!;

    public string? Model { get; set; }

    public bool RecordChats { get; set; }

    public string? PrivacyNotice { get; set; }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
      "Design",
      "CA1056:URI-like properties should not be strings",
      Justification = "PrivacyUrl is a relative app path or an absolute href, copied as text into the anchor.")]
    public string? PrivacyUrl { get; set; }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
      "Design",
      "CA1056:URI-like properties should not be strings",
      Justification = "SupportUrl is a relative app path or an absolute href, copied as text into the anchor.")]
    public string SupportUrl { get; set; } = XaiChatDefaults.SupportUrl;

    public int CredentialLifetimeMinutes { get; set; } = XaiChatDefaults.CredentialLifetimeMinutes;

    public IReadOnlyList<AgentChatStarterPrompt> StarterPrompts { get; set; } = AgentChatStarterPrompts.Defaults;
  }

  public sealed class Validator : AbstractValidator<Query>
  {
    public Validator()
    {
    }
  }

  public static MockResponseFactory<Response> GetMockResponseFactory() =>
    static _ => new Response
    {
      Configured = false,
      SetupCommand = XaiChatDefaults.SetupCommand,
      Model = null,
      RecordChats = XaiChatDefaults.RecordChats,
      PrivacyNotice = XaiChatDefaults.PrivacyNotice,
      PrivacyUrl = null,
      SupportUrl = XaiChatDefaults.SupportUrl,
      CredentialLifetimeMinutes = XaiChatDefaults.CredentialLifetimeMinutes,
      StarterPrompts = AgentChatStarterPrompts.Defaults
    };
}
