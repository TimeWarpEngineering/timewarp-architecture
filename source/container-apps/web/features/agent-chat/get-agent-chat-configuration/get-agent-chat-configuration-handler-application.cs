#region Purpose
// Returns whether xAI is configured. Does not call the model and does not read the key into the response.
#endregion

#region Design
// Status is unauthenticated-only plus the upstream predicate. It does not take an admission slot:
// the palette probes on every shell load, and that must not consume the completion budget.
// A missing principal is 401 even though the endpoint policy already requires one.
// The notice settings are read from XaiChatOptions per request. A blank notice falls back to
// XaiChatDefaults.PrivacyNotice and a blank PrivacyUrl is sent as null (no link). StarterPrompts
// goes through AgentChatStarterPrompts.Normalize, so an unset list sends the template defaults.
// SetupCommand is the environment's host text and does not include the Admin sentence. AdminHint
// is that sentence only when ISiteXaiApiKey.StorageAvailable is true. Configured reads
// IsConfiguredAsync so an admin key counts without a restart. The model id comes from options,
// not Upstream.Model, because that property only sees the host key and the fake.
#endregion

namespace TimeWarp.Architecture.Features.AgentChats.Application;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using static TimeWarp.Architecture.Features.AgentChats.GetAgentChatConfiguration;

/// <summary>Handles <see cref="Query"/>.</summary>
public sealed class GetAgentChatConfiguration
{
  /// <summary>Reads <see cref="IAgentChatUpstream.IsConfiguredAsync"/> for the current principal.</summary>
  public sealed class Handler : IRequestHandler<Query, OneOf<Response, SharedProblemDetails>>
  {
    private readonly ICurrentPrincipalAccessor CurrentPrincipalAccessor;
    private readonly IAgentChatUpstream Upstream;
    private readonly IOptions<XaiChatOptions> Options;
    private readonly IHostEnvironment Environment;
    private readonly ISiteXaiApiKey Keys;

    /// <summary>Creates the handler.</summary>
    public Handler
    (
      ICurrentPrincipalAccessor currentPrincipalAccessor,
      IAgentChatUpstream upstream,
      IOptions<XaiChatOptions> options,
      IHostEnvironment environment,
      ISiteXaiApiKey keys
    )
    {
      CurrentPrincipalAccessor = currentPrincipalAccessor;
      Upstream = upstream;
      Options = options;
      Environment = environment;
      Keys = keys;
    }

    /// <summary>Returns configured, the setup command, and the model id.</summary>
    public async Task<OneOf<Response, SharedProblemDetails>> Handle
    (
      Query request,
      CancellationToken cancellationToken
    )
    {
      PrincipalId? principalId = await CurrentPrincipalAccessor
        .GetCurrentPrincipalIdAsync(cancellationToken)
        .ConfigureAwait(false);
      if (principalId is null)
      {
        return AgentChatProblems.Unauthenticated();
      }

      bool configured = await Upstream.IsConfiguredAsync(cancellationToken).ConfigureAwait(false);
      XaiChatOptions options = Options.Value;
      string? model = null;
      if (configured)
      {
        model = string.IsNullOrWhiteSpace(options.Model) ? XaiChatDefaults.DefaultModel : options.Model;
      }

      return new Response
      {
        Configured = configured,
        SetupCommand = XaiChatDefaults.HostSetupText(Environment.IsDevelopment()),
        AdminHint = Keys.StorageAvailable ? XaiChatDefaults.AdminAiSentence : null,
        Model = model,
        RecordChats = options.RecordChats,
        PrivacyNotice = string.IsNullOrWhiteSpace(options.PrivacyNotice)
          ? XaiChatDefaults.PrivacyNotice
          : options.PrivacyNotice.Trim(),
        PrivacyUrl = string.IsNullOrWhiteSpace(options.PrivacyUrl) ? null : options.PrivacyUrl.Trim(),
        SupportUrl = XaiChatDefaults.SupportUrl,
        CredentialLifetimeMinutes = XaiChatDefaults.CredentialLifetimeMinutes,
        StarterPrompts = AgentChatStarterPrompts.Normalize(options.StarterPrompts)
      };
    }
  }
}
