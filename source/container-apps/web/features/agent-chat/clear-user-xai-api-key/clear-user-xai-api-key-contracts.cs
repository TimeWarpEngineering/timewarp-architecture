#region Purpose
// Profile clear of the signed-in user's xAI API key. There is no request body and no key in the response.
#endregion

#region Design
// DELETE is safe to retry: a second call leaves the row gone. IIdempotentCommand. The site mode
// Off refuses the clear so a hidden section cannot be cleared by a stale client. profile.write.
// No agent-token scheme.
#endregion

namespace TimeWarp.Architecture.Features.AgentChats;

using TimeWarp.Identity;

/// <summary>Clears the signed-in user's xAI API key.</summary>
[ApiEndpoint]
[EndpointAuthorize
(
  Policy = PermissionIds.ProfileWrite,
  AuthenticationSchemes = AuthenticationSchemeNames.IdentitySession + "," + AuthenticationSchemeNames.MockIdentitySession
)]
public static partial class ClearUserXaiApiKey
{
  /// <summary>No body. The caller is the authenticated user.</summary>
  [ApiRoute("api/Users/Current/xai-api-key", HttpVerb.Delete)]
  [Operation("user-xai-api-key.clear", Description = "Clear the current user's xAI API key.", Group = "profile")]
  public sealed partial class Command : IApiRequest, IIdempotentCommand<OneOf<Response, SharedProblemDetails>>;

  /// <summary>Status after the clear. No key.</summary>
  public sealed class Response : UserXaiApiKeyStatus
  {
    /// <summary>Creates the status response.</summary>
    public Response(
      bool visible,
      XaiByokMode mode,
      bool configured,
      bool needsReplacement,
      bool canChange,
      bool storageAvailable,
      bool supersededByHost,
      string? suffix)
      : base(visible, mode, configured, needsReplacement, canChange, storageAvailable, supersededByHost, suffix)
    {
    }
  }

  /// <summary>No fields.</summary>
  public sealed class Validator : AbstractValidator<Command>;

  /// <summary>Offline status.</summary>
  public static MockResponseFactory<Response> GetMockResponseFactory() =>
    static _ => new Response(
      visible: false,
      mode: XaiByokMode.Allow,
      configured: false,
      needsReplacement: false,
      canChange: false,
      storageAvailable: false,
      supersededByHost: false,
      suffix: null);
}
