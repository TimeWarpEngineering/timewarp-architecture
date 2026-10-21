#region Purpose
// Admin clear of the stored site xAI API key. There is no request body and no key in the response.
#endregion

#region Design
// DELETE is safe to retry: a second call leaves the row gone. IIdempotentCommand. Host
// configuration is not cleared by this command. settings.write. No agent-token scheme.
#endregion

namespace TimeWarp.Architecture.Features.AgentChats;

/// <summary>Clears the stored site xAI API key.</summary>
[ApiEndpoint]
[EndpointAuthorize
(
  Policy = PermissionIds.SettingsWrite,
  AuthenticationSchemes = AuthenticationSchemeNames.IdentitySession + "," + AuthenticationSchemeNames.MockIdentitySession
)]
public static partial class ClearXaiApiKey
{
  /// <summary>No body. The caller is the authenticated admin.</summary>
  [ApiRoute("api/admin/ai/xai-api-key", HttpVerb.Delete)]
  [Operation("xai-api-key.clear", Description = "Clear the stored site xAI API key.", Group = "admin.ai")]
  public sealed partial class Command : IApiRequest, IIdempotentCommand<OneOf<Response, SharedProblemDetails>>;

  /// <summary>Status after the clear. No key.</summary>
  public sealed class Response : XaiApiKeyStatus
  {
    /// <summary>Creates the status response.</summary>
    public Response(
      bool managedByHost,
      bool configured,
      bool storageAvailable,
      bool canChange,
      bool needsReplacement,
      string? suffix,
      DateTimeOffset? setAt,
      Guid? setByPrincipalId,
      string setupHint)
      : base(managedByHost, configured, storageAvailable, canChange, needsReplacement, suffix, setAt, setByPrincipalId, setupHint)
    {
    }
  }

  /// <summary>No fields.</summary>
  public sealed class Validator : AbstractValidator<Command>;

  /// <summary>Offline status.</summary>
  public static MockResponseFactory<Response> GetMockResponseFactory() =>
    static _ => new Response(
      managedByHost: false,
      configured: false,
      storageAvailable: false,
      canChange: false,
      needsReplacement: false,
      suffix: null,
      setAt: null,
      setByPrincipalId: null,
      setupHint: XaiChatDefaults.DeployedSetupHint);
}
