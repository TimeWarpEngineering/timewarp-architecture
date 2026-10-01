#region Purpose
// CreateAccountWithPasskey: mint a new principal with a passkey, then go to the return URL or Settings (Login) or stay (Passkeys demo).
#endregion

#region Design
// Registration mints a NEW principal and session. ReturnPath set: navigate to its safe form,
// except home ("/"), which becomes /Settings so the new account lands on its passkey list
// (literal path: Settings is another slice). ReturnPath StayOnPage (empty): stay and publish "Passkey registered."
// (Developer demo), which then sequences FetchCredentials + SetPendingNickname from
// LastRegisteredCredentialId / LastRegisteredProviderLabel — handlers do not dispatch.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using Microsoft.AspNetCore.Components;
using TimeWarp.Architecture.Services;

partial class SignInState
{
  public static class CreateAccountWithPasskeyActionSet
  {
    [TrackAction]
    public sealed class Action : IBaseAction
    {
      public Action(string returnPath)
      {
        ReturnPath = returnPath;
      }

      /// <summary>Where to go after the account exists; <see cref="StayOnPage"/> stays on the current page.</summary>
      public string ReturnPath { get; }
    }

    internal sealed class Handler
    (
      IStore store,
      PasskeyCeremonyClient ceremony,
      NavigationManager navigationManager,
      IPublisher<ClientPipeline> publisher
    ) : BaseHandler<Action>(store)
    {
      internal const string PostCreateDestination = "/Settings";

      public override async ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        SignInState.CeremonyFailed = false;
        try
        {
          OneOf<CompletePasskeyRegistration.Response, SharedProblemDetails> result =
            await ceremony.RegisterAsync(cancellationToken);

          if (result.IsT1)
          {
            SignInState.CeremonyFailed = true;
            await publisher.Publish(new ProblemDetailsNotification(result.AsT1), cancellationToken);
            return;
          }

          CompletePasskeyRegistration.Response response = result.AsT0;
          SignInState.IsAuthenticated = true;
          SignInState.LastRegisteredCredentialId = response.CredentialId.Value;
          SignInState.LastRegisteredProviderLabel = response.ProviderLabel;

          if (action.ReturnPath.Length > 0)
          {
            string destination = LoginPage.GetSafeReturnUrl(action.ReturnPath);
            navigationManager.NavigateTo(destination == "/" ? PostCreateDestination : destination);
            return;
          }

          await publisher.Publish
          (
            new OutcomeNotification(MessageBarIntent.Success, "Passkey registered.", $"PrincipalId: {response.PrincipalId}"),
            cancellationToken
          );
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
