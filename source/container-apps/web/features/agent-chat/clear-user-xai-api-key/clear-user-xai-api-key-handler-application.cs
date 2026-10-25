#region Purpose
// Clears the signed-in user's stored xAI API key.
#endregion

#region Design
// A missing row still succeeds and is audited. The response is the status after the clear.
// Off is 409 from the service and does not touch the row.
#endregion

namespace TimeWarp.Architecture.Features.AgentChats.Application;

using static TimeWarp.Architecture.Features.AgentChats.ClearUserXaiApiKey;

/// <summary>Handles <see cref="Command"/>.</summary>
public sealed class ClearUserXaiApiKey
{
  /// <summary>Clears the stored key for the current principal.</summary>
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

    /// <summary>Returns the status after the clear, or a problem when the mode is Off.</summary>
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

      OneOf<UserXaiApiKeyStatus, SharedProblemDetails> result = await Keys
        .ClearAsync(principalId.Value.Value, cancellationToken)
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
