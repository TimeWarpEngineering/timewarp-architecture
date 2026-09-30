#region Purpose
// FetchMicrosoft365Choice: peek whether a parked Microsoft 365 sign-in is still waiting for create / already-have.
#endregion

#region Design
// Parked Entra claims are server-side; this only reads validity (the completion responses carry
// the destination). Any failure reads as expired — the page then offers "sign in with Microsoft 365 again".
// The verdict resets to null (unknown) before each fetch so a revisit paints "Checking sign-in…"
// rather than the previous visit's answer.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using TimeWarp.Architecture.Services;

partial class SignInState
{
  public static class FetchMicrosoft365ChoiceActionSet
  {
    public sealed class Action : IBaseAction;

    internal sealed class Handler(IStore store, IWebServerApiService apiService) : BaseHandler<Action>(store)
    {
      public override async ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        SignInState.Microsoft365ChoiceValid = null;
        try
        {
          OneOf<GetEntraBootstrapChoice.Response, FileResponse, SharedProblemDetails> result =
            await apiService.GetResponse<GetEntraBootstrapChoice.Response>(
              new GetEntraBootstrapChoice.Query(),
              cancellationToken);
          if (result.TryPickT0(out GetEntraBootstrapChoice.Response response, out _))
          {
            SignInState.Microsoft365ChoiceValid = response.Valid;
            return;
          }
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
          // Fall through to expired.
        }

        SignInState.Microsoft365ChoiceValid = false;
      }
    }
  }
}
