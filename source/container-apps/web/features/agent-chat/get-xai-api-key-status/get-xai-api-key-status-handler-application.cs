#region Purpose
// Returns the site xAI API key status for an admin. Does not unprotect the key.
#endregion

#region Design
// The endpoint policy already requires settings.write. A missing principal is still 401. The
// setup hint attached here is the host text only: it does not tell the admin to open this page.
// A store failure is the 503 from ISiteXaiApiKey. The plaintext is not copied onto the response.
#endregion

namespace TimeWarp.Architecture.Features.AgentChats.Application;

using Microsoft.Extensions.Hosting;
using TimeWarp.Architecture.Features.AgentChats;
using static TimeWarp.Architecture.Features.AgentChats.GetXaiApiKeyStatus;

/// <summary>Handles <see cref="Query"/>.</summary>
public sealed class GetXaiApiKeyStatus
{
  /// <summary>Reads the status for the current principal.</summary>
  public sealed class Handler : IRequestHandler<Query, OneOf<Response, SharedProblemDetails>>
  {
    private readonly ICurrentPrincipalAccessor CurrentPrincipalAccessor;
    private readonly ISiteXaiApiKey Keys;
    private readonly IHostEnvironment Environment;

    /// <summary>Creates the handler.</summary>
    public Handler(
      ICurrentPrincipalAccessor currentPrincipalAccessor,
      ISiteXaiApiKey keys,
      IHostEnvironment environment)
    {
      CurrentPrincipalAccessor = currentPrincipalAccessor;
      Keys = keys;
      Environment = environment;
    }

    /// <summary>Returns the status, or 401.</summary>
    public async Task<OneOf<Response, SharedProblemDetails>> Handle(
      Query request,
      CancellationToken cancellationToken)
    {
      _ = request;
      if (await CurrentPrincipalAccessor.GetCurrentPrincipalIdAsync(cancellationToken).ConfigureAwait(false) is null)
      {
        return XaiApiKeyProblems.Unauthenticated();
      }

      OneOf<SiteXaiApiKeyStatus, SharedProblemDetails> status =
        await Keys.GetStatusAsync(cancellationToken).ConfigureAwait(false);
      return status.Match<OneOf<Response, SharedProblemDetails>>(
        value => ToResponse(value, Environment.IsDevelopment()),
        problem => problem);
    }
  }

  internal static Response ToResponse(SiteXaiApiKeyStatus status, bool development) =>
    new(
      managedByHost: status.ManagedByHost,
      configured: status.Configured,
      storageAvailable: status.StorageAvailable,
      canChange: status.CanChange,
      needsReplacement: status.NeedsReplacement,
      suffix: status.Suffix,
      setAt: status.SetAt,
      setByPrincipalId: status.SetByPrincipalId,
      setupHint: XaiChatDefaults.HostSetupText(development));
}
