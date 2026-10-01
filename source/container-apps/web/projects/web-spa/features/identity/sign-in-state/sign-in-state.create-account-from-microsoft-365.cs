#region Purpose
// CreateAccountFromMicrosoft365: turn the parked Microsoft 365 sign-in into a new account, then go to its destination.
#endregion

#region Design
// CompleteEntraBootstrapCreate sets the session cookie server-side; the handler notifies the
// identity-session auth state provider (AuthorizeView re-reads without a reload) before the
// navigation, the same as the already-have path through PasskeyCeremonyClient. A problem whose
// title or detail says "expired" flips Microsoft365ChoiceValid to false so the page offers the
// sign-in again; every problem also goes to the shell region.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using TimeWarp.Architecture.Services;

partial class SignInState
{
  public static class CreateAccountFromMicrosoft365ActionSet
  {
    [TrackAction]
    public sealed class Action : IBaseAction;

    internal sealed class Handler
    (
      IStore store,
      IWebServerApiService apiService,
      AuthenticationStateProvider authenticationStateProvider,
      NavigationManager navigationManager,
      IPublisher<ClientPipeline> publisher
    ) : BaseHandler<Action>(store)
    {
      public override async ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        SignInState.CeremonyFailed = false;
        OneOf<CompleteEntraBootstrapCreate.Response, FileResponse, SharedProblemDetails> result =
          await apiService.GetResponse<CompleteEntraBootstrapCreate.Response>(
            new CompleteEntraBootstrapCreate.Command(),
            cancellationToken);

        if (!result.TryPickT0(out CompleteEntraBootstrapCreate.Response response, out OneOf<FileResponse, SharedProblemDetails> rest))
        {
          SharedProblemDetails problem = rest.IsT1
            ? rest.AsT1
            : new SharedProblemDetails { Status = 500, Title = "Could not create the account." };
          await SignInState.FailMicrosoft365ChoiceAsync(problem, publisher, cancellationToken);
          return;
        }

        if (authenticationStateProvider is IdentitySessionAuthenticationStateProvider identitySession)
        {
          identitySession.NotifySessionChanged();
        }

        SignInState.IsAuthenticated = true;
        navigationManager.NavigateTo(LoginPage.GetSafeReturnUrl(response.Destination));
      }
    }
  }

  /// <summary>Records a failed Microsoft 365 create / already-have completion and reports it to the shell region.</summary>
  private Task FailMicrosoft365ChoiceAsync
  (
    SharedProblemDetails problem,
    IPublisher<ClientPipeline> publisher,
    CancellationToken cancellationToken
  )
  {
    CeremonyFailed = true;
    if (IsExpired(problem))
    {
      Microsoft365ChoiceValid = false;
    }

    return publisher.Publish(new ProblemDetailsNotification(problem), cancellationToken);
  }

  internal static bool IsExpired(SharedProblemDetails problem) =>
    (problem.Detail ?? string.Empty).Contains("expired", StringComparison.OrdinalIgnoreCase)
    || (problem.Title ?? string.Empty).Contains("expired", StringComparison.OrdinalIgnoreCase);
}
