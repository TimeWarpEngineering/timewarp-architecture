#region Purpose
// Admin write of the site xAI API key. The response does not echo the key.
#endregion

#region Design
// PUT replaces the stored key. A retry stores the same secret again, so this is
// IIdempotentCommand. The request body is the only place the key appears. Validation rejects
// empty, short, long, and whitespace so the stored suffix cannot be the whole secret. The
// handler trims. settings.write. No agent-token scheme.
#endregion

namespace TimeWarp.Architecture.Features.AgentChats;

/// <summary>Sets or replaces the site xAI API key.</summary>
[ApiEndpoint]
[EndpointAuthorize
(
  Policy = PermissionIds.SettingsWrite,
  AuthenticationSchemes = AuthenticationSchemeNames.IdentitySession + "," + AuthenticationSchemeNames.MockIdentitySession
)]
public static partial class SetXaiApiKey
{
  /// <summary>The new key. It is not returned.</summary>
  [ApiRoute("api/admin/ai/xai-api-key", HttpVerb.Put)]
  [Operation("xai-api-key.set", Description = "Set or replace the site xAI API key.", Group = "admin.ai")]
  public sealed partial class Command : IApiRequest, IIdempotentCommand<OneOf<Response, SharedProblemDetails>>
  {
    /// <summary>The xAI API key. Required. Not stored in the response.</summary>
    public string ApiKey { get; set; } = null!;
  }

  /// <summary>Status after the write. No key.</summary>
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
      managedByHost: false,
      configured: false,
      storageAvailable: false,
      canChange: false,
      suffix: null,
      setAt: null,
      setByPrincipalId: null,
      setupHint: XaiChatDefaults.DeployedSetupHint);
}
