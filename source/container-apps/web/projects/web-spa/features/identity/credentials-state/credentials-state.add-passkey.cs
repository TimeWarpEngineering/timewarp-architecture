#region Purpose
// AddPasskey: attach another passkey to the signed-in principal (Settings "Create a passkey").
#endregion

#region Design
// Multi-step ceremony cannot be a single DefaultApiHandler request:
//   1. HTTP StartPasskeyRegistration (anonymous options mint — same as product Login create path)
//   2. browser Spa.WebAuthn.CreateCredential
//   3. HTTP AddPasskey (authenticated attach)
// Both HTTP legs go through IWebServerApiService inside this ActionSet (not the page, not a
// ceremony client GetResponse for Settings). JSException → CeremonyError on state; API failures
// → SharedProblemDetails toast via ToastNotificationState (same as DefaultApiHandler).
// Success: LastAddedCredentialId + re-fetch list. Task 169.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Text.Json;
using TimeWarp.Architecture.Features;
using TimeWarp.Foundation.Types;

partial class CredentialsState
{
  internal static class AddPasskeyActionSet
  {
    [TrackAction]
    internal sealed class Action : IBaseAction
    {
      public Action()
      {
      }

      public Action(string label)
      {
        Label = label;
      }

      public string? Label { get; }
    }

    internal sealed class Handler : BaseHandler<Action>
    {
      private readonly IWebServerApiService ApiService;
      private readonly IJSRuntime JsRuntime;
      private readonly AuthenticationStateProvider AuthenticationStateProvider;
      private readonly ISender Sender;

      public Handler
      (
        IStore store,
        IWebServerApiService apiService,
        IJSRuntime jsRuntime,
        AuthenticationStateProvider authenticationStateProvider,
        ISender sender
      ) : base(store)
      {
        ApiService = apiService;
        JsRuntime = jsRuntime;
        AuthenticationStateProvider = authenticationStateProvider;
        Sender = sender;
      }

      public override async Task Handle(Action action, CancellationToken cancellationToken)
      {
        CredentialsState.CeremonyError = null;
        CredentialsState.StatusMessage = null;

        try
        {
          OneOf<StartPasskeyRegistration.Response, FileResponse, SharedProblemDetails> startResult =
            await ApiService.GetResponse<StartPasskeyRegistration.Response>(
              new StartPasskeyRegistration.Command(),
              cancellationToken);

          if (!startResult.IsT0)
          {
            await FailAsync(ToProblem(startResult), cancellationToken);
            return;
          }

          string credentialJson =
            await JsRuntime.InvokeAsync<string>(
              "Spa.WebAuthn.CreateCredential",
              cancellationToken,
              startResult.AsT0.OptionsJson,
              false);

          using var document = JsonDocument.Parse(credentialJson);
          JsonElement root = document.RootElement;

          Guid userId = await ResolveUserIdAsync();
          AddPasskey.Command completeCommand = new()
          {
            UserId = userId,
            CredentialId = root.GetProperty("credentialId").GetString()!,
            ClientDataJson = root.GetProperty("clientDataJson").GetString()!,
            AttestationObject = root.GetProperty("attestationObject").GetString()!,
            Label = action.Label
          };

          OneOf<AddPasskey.Response, FileResponse, SharedProblemDetails> completeResult =
            await ApiService.GetResponse<AddPasskey.Response>(completeCommand, cancellationToken);

          if (!completeResult.IsT0)
          {
            await FailAsync(ToProblem(completeResult), cancellationToken);
            return;
          }

          CredentialsState.LastAddedCredentialId = completeResult.AsT0.CredentialId.Value;
          CredentialsState.StatusMessage = "Passkey created.";
          await Sender.Send(new FetchCredentialsActionSet.Action(), cancellationToken);
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

      private async Task<Guid> ResolveUserIdAsync()
      {
        try
        {
          return await AuthenticationStateProvider.GetUserIdAsync();
        }
        catch (InvalidOperationException)
        {
          return Guid.NewGuid();
        }
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
