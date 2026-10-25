#region Purpose
// Profile write of the signed-in user's xAI API key. The response does not echo the key.
#endregion

#region Design
// PUT replaces the stored key. A retry stores the same secret again, so this is
// IIdempotentCommand. The request body is the only place the key appears. Validation rejects
// empty, short, long, and whitespace so the stored suffix cannot be the whole secret. The
// handler trims. profile.write. No agent-token scheme. A host key does not block this write.
#endregion

namespace TimeWarp.Architecture.Features.AgentChats;

using TimeWarp.Identity;

/// <summary>Sets or replaces the signed-in user's xAI API key.</summary>
[ApiEndpoint]
[EndpointAuthorize
(
  Policy = PermissionIds.ProfileWrite,
  AuthenticationSchemes = AuthenticationSchemeNames.IdentitySession + "," + AuthenticationSchemeNames.MockIdentitySession
)]
public static partial class SetUserXaiApiKey
{
  /// <summary>The new key. It is not returned.</summary>
  [ApiRoute("api/Users/Current/xai-api-key", HttpVerb.Put)]
  [Operation("user-xai-api-key.set", Description = "Set or replace the current user's xAI API key.", Group = "profile")]
  public sealed partial class Command : IApiRequest, IIdempotentCommand<OneOf<Response, SharedProblemDetails>>
  {
    /// <summary>The xAI API key. Required. Not stored in the response.</summary>
    public string ApiKey { get; set; } = null!;
  }

  /// <summary>Status after the write. No key.</summary>
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

  /// <summary>Length and whitespace. The message does not include the submitted value.</summary>
  public sealed class Validator : AbstractValidator<Command>
  {
    /// <summary>Creates the rules.</summary>
    public Validator()
    {
      RuleFor(command => command.ApiKey)
        .NotEmpty()
        .Must(SiteXaiApiKeyNames.IsAcceptable)
        .WithMessage(
          "Enter the xAI API key ("
          + SiteXaiApiKeyNames.MinimumLength
          + " to "
          + SiteXaiApiKeyNames.MaximumLength
          + " characters, no spaces).");
    }
  }

  /// <summary>Offline status. The submitted key is discarded.</summary>
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
