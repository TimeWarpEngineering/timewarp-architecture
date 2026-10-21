#region Purpose
// Stores a new site xAI API key for an admin. The response is the status, not the key.
#endregion

#region Design
// Validation has already rejected an empty or short key. The service trims and repeats the bound
// so the suffix cannot be the secret. Host configuration and missing storage come back as
// problems that do not contain the submitted value. The audit line is written in the service.
#endregion

namespace TimeWarp.Architecture.Features.AgentChats.Application;

using Microsoft.Extensions.Hosting;
using static TimeWarp.Architecture.Features.AgentChats.SetXaiApiKey;

/// <summary>Handles <see cref="Command"/>.</summary>
public sealed class SetXaiApiKey
{
  /// <summary>Sets the key for the current principal.</summary>
  public sealed class Handler : IRequestHandler<Command, OneOf<Response, SharedProblemDetails>>
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

    /// <summary>Returns the new status, or a problem that does not contain the key.</summary>
    public async Task<OneOf<Response, SharedProblemDetails>> Handle(
      Command request,
      CancellationToken cancellationToken)
    {
      PrincipalId? principalId = await CurrentPrincipalAccessor
        .GetCurrentPrincipalIdAsync(cancellationToken)
        .ConfigureAwait(false);
      if (principalId is null)
      {
        return XaiApiKeyProblems.Unauthenticated();
      }

      OneOf<SiteXaiApiKeyStatus, SharedProblemDetails> result = await Keys
        .SetAsync(request.ApiKey, principalId.Value.Value, cancellationToken)
        .ConfigureAwait(false);
      return result.Match<OneOf<Response, SharedProblemDetails>>(
        status => Copy(GetXaiApiKeyStatus.ToResponse(status, Environment.IsDevelopment())),
        problem => problem);
    }

    private static Response Copy(TimeWarp.Architecture.Features.AgentChats.GetXaiApiKeyStatus.Response status) =>
      new(
        managedByHost: status.ManagedByHost,
        configured: status.Configured,
        storageAvailable: status.StorageAvailable,
        canChange: status.CanChange,
        suffix: status.Suffix,
        setAt: status.SetAt,
        setByPrincipalId: status.SetByPrincipalId,
        setupHint: status.SetupHint);
  }
}
