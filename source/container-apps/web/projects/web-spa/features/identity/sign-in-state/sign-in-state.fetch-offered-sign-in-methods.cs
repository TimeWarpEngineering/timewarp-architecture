#region Purpose
// FetchOfferedSignInMethods: read the server's offered sign-in methods for the login card and the shell gate.
#endregion

#region Design
// OfferedMethods starts null. A successful read stores the list (possibly empty). A failure leaves
// it null so the card invents no buttons and the required-passkey gate does not block. The server
// remains the authority: passkey ceremonies 403 when Passkey is not listed, and the Entra challenge
// 404s when Microsoft365 is not listed.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using TimeWarp.Architecture.Services;

partial class SignInState
{
  public static class FetchOfferedSignInMethodsActionSet
  {
    [CommandPaletteOptOut("Page/startup data load; the owning page or shell runs it, a person never needs to.")]
    public sealed class Action : IBaseAction;

    internal sealed class Handler(IStore store, IWebServerApiService apiService) : BaseHandler<Action>(store)
    {
      public override async ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        IReadOnlyList<SignInMethod>? methods = await LoadMethodsAsync(cancellationToken);
        SignInState.OfferedMethodList = methods is null ? null : [.. methods];
      }

      private async Task<IReadOnlyList<SignInMethod>?> LoadMethodsAsync(CancellationToken cancellationToken)
      {
        try
        {
          OneOf<GetEntraSignInOffered.Response, FileResponse, SharedProblemDetails> result =
            await apiService.GetResponse<GetEntraSignInOffered.Response>(
              new GetEntraSignInOffered.Query(),
              cancellationToken);
          return result.TryPickT0(out GetEntraSignInOffered.Response response, out _)
            ? response.Methods
            : null;
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
          return null;
        }
      }
    }
  }
}
