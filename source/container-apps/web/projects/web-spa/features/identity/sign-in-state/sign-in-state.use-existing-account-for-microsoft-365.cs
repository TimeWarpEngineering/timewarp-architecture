#region Purpose
// UseExistingAccountForMicrosoft365: prove an existing account with a passkey, attach the parked Microsoft 365 sign-in to it, then go to its destination.
#endregion

#region Design
// PasskeyCeremonyClient.CompleteEntraChoiceExistingAsync runs StartPasskeyAuthentication →
// browser assertion → CompleteEntraBootstrapExisting, so the Entra attach and the session happen
// together, and notifies the identity-session auth state provider on success. Failures follow
// CreateAccountFromMicrosoft365 (expired flips Microsoft365ChoiceValid); a JSException publishes
// the ceremony Error outcome.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using Microsoft.AspNetCore.Components;
using TimeWarp.Architecture.Services;

partial class SignInState
{
  public static class UseExistingAccountForMicrosoft365ActionSet
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
          OneOf<CompleteEntraBootstrapExisting.Response, SharedProblemDetails> result =
            await ceremony.CompleteEntraChoiceExistingAsync(cancellationToken);

          if (result.IsT1)
          {
            await SignInState.FailMicrosoft365ChoiceAsync(result.AsT1, publisher, cancellationToken);
            return;
          }

          SignInState.IsAuthenticated = true;
          navigationManager.NavigateTo(LoginPage.GetSafeReturnUrl(result.AsT0.Destination));
        }
        catch (JSException jsException)
        {
          SignInState.CeremonyFailed = true;
          await publisher.Publish(CeremonyFailure(jsException), cancellationToken);
        }
      }
    }
  }
}
