#region Purpose
// Admin read of the site xAI API key status. The key is not in the response.
#endregion

#region Design
// settings.write, same gate as Admin → Authentication. settings.read stays self-service and does
// not see this. Identity-session and mock-identity-session only: an agent token must not read
// even the suffix. IQuery. The mock factory is an empty unconfigured status so offline Admin → AI
// renders the host-only explanation.
#endregion

namespace TimeWarp.Architecture.Features.AgentChats;

/// <summary>Reads the write-only xAI API key status.</summary>
[ApiEndpoint]
[EndpointAuthorize
(
  Policy = PermissionIds.SettingsWrite,
  AuthenticationSchemes = AuthenticationSchemeNames.IdentitySession + "," + AuthenticationSchemeNames.MockIdentitySession
)]
public static partial class GetXaiApiKeyStatus
{
  /// <summary>No parameters. The caller is the authenticated admin.</summary>
  [ApiRoute("api/admin/ai/xai-api-key", HttpVerb.Get)]
  [Operation("xai-api-key.get", Description = "Read whether the site xAI API key is configured.", Group = "admin.ai")]
  public sealed partial class Query : IApiRequest, IQuery<OneOf<Response, SharedProblemDetails>>;

  /// <summary>Status only.</summary>
  public sealed class Response : XaiApiKeyStatus
  {
    /// <summary>Creates the status response.</summary>
    public Response(
      bool managedByHost,
      bool configured,
      bool storageAvailable,
      bool canChange,
      string? suffix,
      DateTimeOffset? setAt,
      Guid? setByPrincipalId,
      string setupHint)
      : base(managedByHost, configured, storageAvailable, canChange, suffix, setAt, setByPrincipalId, setupHint)
    {
    }
  }

  /// <summary>No query fields.</summary>
  public sealed class Validator : AbstractValidator<Query>;

  /// <summary>Offline status: nothing stored, host configuration only.</summary>
  public static MockResponseFactory<Response> GetMockResponseFactory() =>
    static _ => new Response(
      managedByHost: false,
      configured: false,
      storageAvailable: false,
      canChange: false,
      suffix: null,
      setAt: null,
      setByPrincipalId: null,
      setupHint: XaiChatDefaults.DeployedSetupHint);
}
