#region Purpose
// SignInWithPasskey: run the discoverable passkey authentication ceremony, then go to the return URL (Login) or stay (Passkeys demo).
#endregion

#region Design
// PasskeyCeremonyClient owns the start → browser → complete mapping and notifies the identity
// session auth state provider on success. ReturnPath set: navigate to its safe form (Login).
// ReturnPath StayOnPage (empty): stay and publish "Authenticated." (the Developer ceremony demo).
// A problem from either HTTP leg publishes ProblemDetailsNotification; a JSException (cancelled
// browser dialog, no authenticator) publishes an Error outcome. Mock mode has no ceremony mock
// factories, so the 501 problem reaches the shell region the same way.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using Microsoft.AspNetCore.Components;
using TimeWarp.Architecture.Services;

partial class SignInState
{
  public static class SignInWithPasskeyActionSet
  {
    [TrackAction]
    public sealed class Action : IBaseAction
    {
      public Action(string returnPath)
      {
        ReturnPath = returnPath;
      }

      /// <summary>Where to go after sign-in; <see cref="StayOnPage"/> stays on the current page.</summary>
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
      public override async ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        SignInState.CeremonyFailed = false;
        try
        {
          OneOf<CompletePasskeyAuthentication.Response, SharedProblemDetails> result =
            await ceremony.AuthenticateAsync(cancellationToken);

          if (result.IsT1)
          {
            SignInState.CeremonyFailed = true;
            await publisher.Publish(new ProblemDetailsNotification(result.AsT1), cancellationToken);
            return;
          }

          SignInState.IsAuthenticated = true;
          if (action.ReturnPath.Length > 0)
          {
            navigationManager.NavigateTo(LoginPage.GetSafeReturnUrl(action.ReturnPath));
            return;
          }

          await publisher.Publish
          (
            new OutcomeNotification(MessageBarIntent.Success, "Authenticated.", $"PrincipalId: {result.AsT0.PrincipalId}"),
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

  internal static OutcomeNotification CeremonyFailure(JSException jsException) =>
    new(MessageBarIntent.Error, "The browser could not complete the passkey ceremony", jsException.Message);
}
