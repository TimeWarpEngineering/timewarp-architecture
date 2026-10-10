#region Purpose
// Shared xAI chat constants: configuration keys, the default model, and the user-secrets command.
#endregion

#region Design
// The key name, the web-server project path and its user-secrets id are duplicated by dev-cli (it
// must not reference this assembly). Checked https://docs.x.ai/docs/models on 2026-10-09: grok-4.7 is the current
// recommended chat and code model, and the alias tracks the latest stable release, so the
// template default is not a dated snapshot. XAI:Model overrides it. The endpoint is the
// OpenAI-compatible API at https://api.x.ai/v1.
// AuthenticatedPolicy repeats IdentitySessionDefaults.AuthenticatedPolicy. Contracts cannot
// reference that server type. TWA0024 constant-folds the attribute against the server AddPolicy.
// The relay accepts identity-session and mock-identity-session only. Agent-token is omitted so
// an agent bearer cannot spend the Grok key.
#endregion

namespace TimeWarp.Architecture.Features.AgentChats;

/// <summary>Configuration keys and the development setup command for the xAI relay.</summary>
public static class XaiChatDefaults
{
  /// <summary>Configuration section. Environment variable prefix is <c>XAI__</c>.</summary>
  public const string SectionName = "XAI";

  /// <summary>User-secrets and configuration key for the xAI API key. Never written to appsettings.</summary>
  public const string ApiKeyKey = "XAI:ApiKey";

  /// <summary>Optional model override. Empty uses <see cref="DefaultModel"/>.</summary>
  public const string ModelKey = "XAI:Model";

  /// <summary>Optional endpoint override. Empty uses <see cref="DefaultEndpoint"/>.</summary>
  public const string EndpointKey = "XAI:Endpoint";

  /// <summary>Current recommended Grok chat model (docs.x.ai, 2026-10-09).</summary>
  public const string DefaultModel = "grok-4.7";

  /// <summary>OpenAI-compatible xAI API root.</summary>
  public const string DefaultEndpoint = "https://api.x.ai/v1";

  /// <summary>
  /// Same literal as <c>IdentitySessionDefaults.AuthenticatedPolicy</c>. Any signed-in
  /// identity-session or mock-identity-session principal.
  /// </summary>
  public const string AuthenticatedPolicy = "identity-session-authenticated";

  /// <summary>web-server project, relative to the repository root. Owns the user-secrets id.</summary>
  public const string ProjectPath = "source/container-apps/web/projects/web-server/web-server.csproj";

  /// <summary>web-server's &lt;UserSecretsId&gt; (web-server.csproj). A test pins the two together.</summary>
  public const string UserSecretsId = "0e53fdd3-6f93-4d5a-9c86-040621f7929e";

  /// <summary>
  /// Exact pwsh command shown when no key is configured. <c>--id</c> works from any directory;
  /// the old repo-relative <c>--project</c> path failed outside the repo root (task 290).
  /// </summary>
  public const string SetupCommand =
    "dotnet user-secrets set \"XAI:ApiKey\" \"<your-xai-key>\" --id " + UserSecretsId;

  /// <summary>Template default: chats are not recorded until a deployment turns this on.</summary>
  public const bool RecordChats = false;

  /// <summary>Shown in Ask only when <see cref="RecordChats"/> is true.</summary>
  public const string PrivacyNotice = "Chats are recorded.";

  /// <summary>Support link under an answer. Empty hides the link.</summary>
  public const string SupportUrl = "/Feedback";

  /// <summary>Conversation credential lifetime, capped again at the next UTC midnight.</summary>
  public const int CredentialLifetimeMinutes = 720;
}
