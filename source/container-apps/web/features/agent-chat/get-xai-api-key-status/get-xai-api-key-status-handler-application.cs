#region Purpose
// Returns the site xAI API key status for an admin. Does not unprotect the key.
#endregion

#region Design
// The endpoint policy already requires settings.write. A missing principal is still 401. The
// setup hint is attached here from the host environment. The status itself comes from
// ISiteXaiApiKey, which does not read the plaintext for this call.
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

      SiteXaiApiKeyStatus status = await Keys.GetStatusAsync(cancellationToken).ConfigureAwait(false);
      return ToResponse(status, Environment.IsDevelopment());
    }
  }

  internal static Response ToResponse(SiteXaiApiKeyStatus status, bool development) =>
    new(
      managedByHost: status.ManagedByHost,
      configured: status.Configured,
      storageAvailable: status.StorageAvailable,
      canChange: status.CanChange,
      suffix: status.Suffix,
      setAt: status.SetAt,
      setByPrincipalId: status.SetByPrincipalId,
      setupHint: XaiChatDefaults.SetupHint(development));
}
