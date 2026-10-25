#region Purpose
// Returns the signed-in user's xAI API key status. Does not return the key.
#endregion

#region Design
// The endpoint policy already requires profile.read. A missing principal is still 401. The
// service decides Off, storage, and the suffix. The plaintext is not copied onto the response.
#endregion

namespace TimeWarp.Architecture.Features.AgentChats.Application;

using TimeWarp.Architecture.Features.AgentChats;
using static TimeWarp.Architecture.Features.AgentChats.GetUserXaiApiKeyStatus;

/// <summary>Handles <see cref="Query"/>.</summary>
public sealed class GetUserXaiApiKeyStatus
{
  /// <summary>Reads the status for the current principal.</summary>
  public sealed class Handler : IRequestHandler<Query, OneOf<Response, SharedProblemDetails>>
  {
    private readonly ICurrentPrincipalAccessor CurrentPrincipalAccessor;
    private readonly IUserXaiApiKey Keys;

    /// <summary>Creates the handler.</summary>
    public Handler(ICurrentPrincipalAccessor currentPrincipalAccessor, IUserXaiApiKey keys)
    {
      CurrentPrincipalAccessor = currentPrincipalAccessor;
      Keys = keys;
    }

    /// <summary>Returns the status, or 401.</summary>
    public async Task<OneOf<Response, SharedProblemDetails>> Handle(
      Query request,
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

      OneOf<UserXaiApiKeyStatus, SharedProblemDetails> status =
        await Keys.GetStatusAsync(principalId.Value.Value, cancellationToken).ConfigureAwait(false);
      return status.Match<OneOf<Response, SharedProblemDetails>>(
        statusValue => ToResponse(statusValue),
        problem => problem);
    }
  }

  internal static Response ToResponse(UserXaiApiKeyStatus status) =>
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
