#region Purpose
// Clears the stored site xAI API key for an admin.
#endregion

#region Design
// Does not clear a host-configured key. A missing row still succeeds and is audited. The response
// is the status after the clear.
#endregion

namespace TimeWarp.Architecture.Features.AgentChats.Application;

using Microsoft.Extensions.Hosting;
using static TimeWarp.Architecture.Features.AgentChats.ClearXaiApiKey;

/// <summary>Handles <see cref="Command"/>.</summary>
public sealed class ClearXaiApiKey
{
  /// <summary>Clears the stored key for the current principal.</summary>
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

    /// <summary>Returns the status after the clear, or a problem when the host owns the key.</summary>
    public async Task<OneOf<Response, SharedProblemDetails>> Handle(
      Command request,
      CancellationToken cancellationToken)
    {
      _ = request;
      PrincipalId? principalId = await CurrentPrincipalAccessor
        .GetCurrentPrincipalIdAsync(cancellationToken)
        .ConfigureAwait(false);
      if (principalId is null)
      {
        return XaiApiKeyProblems.Unauthenticated();
      }

      OneOf<SiteXaiApiKeyStatus, SharedProblemDetails> result = await Keys
        .ClearAsync(principalId.Value.Value, cancellationToken)
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
