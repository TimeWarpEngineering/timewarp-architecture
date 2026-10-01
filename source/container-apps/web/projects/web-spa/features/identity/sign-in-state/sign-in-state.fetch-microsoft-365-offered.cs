#region Purpose
// FetchMicrosoft365Offered: read GetEntraSignInOffered so Login and Settings show Microsoft 365 only when the server offers it.
#endregion

#region Design
// Any failure (transport, problem, mock 501) reads as "not offered": the section is an optional
// path and must not break the page. The server challenge stays the authority (404 when the
// scheme is not registered, 403 when site policy turns sign-in off).
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using TimeWarp.Architecture.Services;

partial class SignInState
{
  public static class FetchMicrosoft365OfferedActionSet
  {
    public sealed class Action : IBaseAction;

    internal sealed class Handler(IStore store, IWebServerApiService apiService) : BaseHandler<Action>(store)
    {
      public override async ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        SignInState.Microsoft365Offered = await LoadOfferedAsync(cancellationToken);
      }

      private async Task<bool> LoadOfferedAsync(CancellationToken cancellationToken)
      {
        try
        {
          OneOf<GetEntraSignInOffered.Response, FileResponse, SharedProblemDetails> result =
            await apiService.GetResponse<GetEntraSignInOffered.Response>(
              new GetEntraSignInOffered.Query(),
              cancellationToken);
          return result.TryPickT0(out GetEntraSignInOffered.Response response, out _)
            && response.Offered;
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
          return false;
        }
      }
    }
  }
}
