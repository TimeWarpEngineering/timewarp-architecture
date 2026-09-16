#region Purpose
// AddExistingPasskey: assert a passkey from another principal and merge that account into this one.
#endregion

#region Design
// Same three-step ceremony as AddPasskey, but start/complete are merge-scoped
// (StartAddExistingPasskey / CompleteAddExistingPasskey). Success refreshes credentials and
// sets StatusMessage to "Merged account: N credential(s) moved".
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using Microsoft.JSInterop;
using System.Text.Json;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Services;
using TimeWarp.Foundation.Types;

partial class CredentialsState
{
  internal static class AddExistingPasskeyActionSet
  {
    [TrackAction]
    internal sealed class Action : IBaseAction;

    internal sealed class Handler : BaseHandler<Action>
    {
      private readonly IWebServerApiService ApiService;
      private readonly IJSRuntime JsRuntime;

      public Handler
      (
        IStore store,
        IWebServerApiService apiService,
        IJSRuntime jsRuntime
      ) : base(store)
      {
        ApiService = apiService;
        JsRuntime = jsRuntime;
      }

      public override async Task Handle(Action action, CancellationToken cancellationToken)
      {
        CredentialsState.CeremonyError = null;
        CredentialsState.StatusMessage = null;

        try
        {
          OneOf<StartAddExistingPasskey.Response, FileResponse, SharedProblemDetails> startResult =
            await ApiService.GetResponse<StartAddExistingPasskey.Response>(
              new StartAddExistingPasskey.Command(),
              cancellationToken);

          if (!startResult.IsT0)
          {
            await FailAsync(ToProblem(startResult), cancellationToken);
            return;
          }

          string assertionJson =
            await WebAuthnJsModule.GetCredentialAsync(
              JsRuntime,
              startResult.AsT0.OptionsJson,
              preferHybrid: false,
              cancellationToken);

          using var document = JsonDocument.Parse(assertionJson);
          JsonElement root = document.RootElement;

          CompleteAddExistingPasskey.Command completeCommand = new()
          {
            CredentialId = root.GetProperty("credentialId").GetString()!,
            ClientDataJson = root.GetProperty("clientDataJson").GetString()!,
            AuthenticatorData = root.GetProperty("authenticatorData").GetString()!,
            Signature = root.GetProperty("signature").GetString()!,
            UserHandle = root.TryGetProperty("userHandle", out JsonElement userHandleElement)
              && userHandleElement.ValueKind == JsonValueKind.String
                ? userHandleElement.GetString()
                : null
          };

          OneOf<CompleteAddExistingPasskey.Response, FileResponse, SharedProblemDetails> completeResult =
            await ApiService.GetResponse<CompleteAddExistingPasskey.Response>(completeCommand, cancellationToken);

          if (!completeResult.IsT0)
          {
            await FailAsync(ToProblem(completeResult), cancellationToken);
            return;
          }

          int moved = completeResult.AsT0.CredentialsMoved;
          CredentialsState.StatusMessage =
            moved == 1
              ? "Merged account: 1 credential moved"
              : $"Merged account: {moved} credential(s) moved";
          await CredentialsState.FetchCredentials(externalCancellationToken: cancellationToken);
        }
        catch (JSException jsException)
        {
          CredentialsState.CeremonyError =
            $"The browser could not complete the passkey ceremony: {jsException.Message}";
        }
      }

      private async Task FailAsync(SharedProblemDetails problem, CancellationToken cancellationToken)
      {
        CredentialsState.CeremonyError = $"{problem.Title}: {problem.Detail}";
        await ToastNotificationState.AddProblemDetails(problem, cancellationToken);
      }

      private static SharedProblemDetails ToProblem<TResponse>(
        OneOf<TResponse, FileResponse, SharedProblemDetails> result)
        where TResponse : class =>
        result.IsT2
          ? result.AsT2
          : new SharedProblemDetails
          {
            Status = 500,
            Title = "Unexpected response",
            Detail = "An unexpected response was received from the identity service."
          };
    }
  }
}
