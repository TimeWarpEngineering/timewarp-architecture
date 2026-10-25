#region Purpose
// Profile read of the signed-in user's xAI API key status. The key is not in the response.
#endregion

#region Design
// profile.read. Identity-session and mock-identity-session only. The server derives the user.
// Visible is false when the site mode is Off, and then Suffix is null. The mock factory is a
// hidden status so an offline Profile does not offer a key the app will not use.
#endregion

namespace TimeWarp.Architecture.Features.AgentChats;

using TimeWarp.Identity;

/// <summary>Reads the signed-in user's xAI API key status.</summary>
[ApiEndpoint]
[EndpointAuthorize
(
  Policy = PermissionIds.ProfileRead,
  AuthenticationSchemes = AuthenticationSchemeNames.IdentitySession + "," + AuthenticationSchemeNames.MockIdentitySession
)]
public static partial class GetUserXaiApiKeyStatus
{
  /// <summary>No parameters. The caller is the authenticated user.</summary>
  [ApiRoute("api/Users/Current/xai-api-key", HttpVerb.Get)]
  [Operation("user-xai-api-key.get", Description = "Read whether the current user's xAI API key is configured.", Group = "profile")]
  public sealed partial class Query : IApiRequest, IQuery<OneOf<Response, SharedProblemDetails>>;

  /// <summary>Status only. No key.</summary>
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

  /// <summary>No query fields.</summary>
  public sealed class Validator : AbstractValidator<Query>;

  /// <summary>Offline status: the section stays hidden.</summary>
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
