#region Purpose
// Shared xAI chat constants: configuration keys, the default model, and the user-secrets command.
#endregion

#region Design
// The key name and the web-server project path are duplicated by dev-cli (it must not reference
// this assembly). Checked https://docs.x.ai/docs/models on 2026-10-09: grok-4.7 is the current
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

  /// <summary>Exact pwsh command shown when no key is configured.</summary>
  public const string SetupCommand =
    "dotnet user-secrets set \"XAI:ApiKey\" \"<your-xai-key>\" --project source/container-apps/web/projects/web-server/web-server.csproj";
}
