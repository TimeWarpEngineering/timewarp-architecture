#region Purpose
// CreateAccountFromMicrosoft365: turn the parked Microsoft 365 sign-in into a new account, then go to its destination.
#endregion

#region Design
// CompleteEntraBootstrapCreate issues the session cookie and clears the parked choice cookie, so it
// runs through PasskeyCeremonyClient.CompleteEntraChoiceCreateAsync: the browser sends the request
// and stores both Set-Cookie headers (task 350; under InteractiveServer IWebServerApiService is a
// server-side self-call). The navigation is forceLoad so the next request carries the session, the
// same as the already-have path. A problem whose
// title or detail says "expired" flips Microsoft365ChoiceValid to false so the page offers the
// sign-in again; every problem also goes to the shell region.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using Microsoft.AspNetCore.Components;
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
      PasskeyCeremonyClient ceremony,
      NavigationManager navigationManager,
      IPublisher<ClientPipeline> publisher
    ) : BaseHandler<Action>(store)
    {
      public override async ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        SignInState.CeremonyFailed = false;
        try
        {
          OneOf<CompleteEntraBootstrapCreate.Response, SharedProblemDetails> result =
            await ceremony.CompleteEntraChoiceCreateAsync(cancellationToken);

          if (result.IsT1)
          {
            await SignInState.FailMicrosoft365ChoiceAsync(result.AsT1, publisher, cancellationToken);
            return;
          }

          SignInState.IsAuthenticated = true;
          navigationManager.NavigateTo(LoginPage.GetSafeReturnUrl(result.AsT0.Destination), forceLoad: true);
        }
        catch (JSException jsException)
        {
          SignInState.CeremonyFailed = true;
          await publisher.Publish(CeremonyFailure(jsException), cancellationToken);
        }
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
