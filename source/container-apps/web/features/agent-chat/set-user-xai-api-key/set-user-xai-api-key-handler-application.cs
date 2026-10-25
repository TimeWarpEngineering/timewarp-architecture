#region Purpose
// Stores a new xAI API key for the signed-in user. The response is the status, not the key.
#endregion

#region Design
// Validation has already rejected an empty or short key. The service trims, refuses Off, and
// calls xAI before it writes. A probe problem is returned unchanged and does not contain the
// submitted value. The audit line is written in the service.
#endregion

namespace TimeWarp.Architecture.Features.AgentChats.Application;

using static TimeWarp.Architecture.Features.AgentChats.SetUserXaiApiKey;

/// <summary>Handles <see cref="Command"/>.</summary>
public sealed class SetUserXaiApiKey
{
  /// <summary>Sets the key for the current principal.</summary>
  public sealed class Handler : IRequestHandler<Command, OneOf<Response, SharedProblemDetails>>
  {
    private readonly ICurrentPrincipalAccessor CurrentPrincipalAccessor;
    private readonly IUserXaiApiKey Keys;

    /// <summary>Creates the handler.</summary>
    public Handler(ICurrentPrincipalAccessor currentPrincipalAccessor, IUserXaiApiKey keys)
    {
      CurrentPrincipalAccessor = currentPrincipalAccessor;
      Keys = keys;
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

      OneOf<UserXaiApiKeyStatus, SharedProblemDetails> result = await Keys
        .SetAsync(principalId.Value.Value, request.ApiKey, cancellationToken)
        .ConfigureAwait(false);
      return result.Match<OneOf<Response, SharedProblemDetails>>(
        status => Copy(GetUserXaiApiKeyStatus.ToResponse(status)),
        problem => problem);
    }

    private static Response Copy(TimeWarp.Architecture.Features.AgentChats.GetUserXaiApiKeyStatus.Response status) =>
      new(
        visible: status.Visible,
        mode: status.Mode,
        configured: status.Configured,
        needsReplacement: status.NeedsReplacement,
        canChange: status.CanChange,
        storageAvailable: status.StorageAvailable,
        supersededByHost: status.SupersededByHost,
        suffix: status.Suffix);
  }
}
