#region Purpose
// AddPasskey: attach another passkey to the signed-in principal (Settings "Create a passkey").
#endregion

#region Design
// Multi-step ceremony cannot be a single DefaultApiHandler request:
//   1. HTTP StartPasskeyRegistration with ForCurrentAccount=true (task 253: the server names the
//      passkey for the signed-in account, "TimeWarp account · <fingerprint>", from the session)
//   2. browser WebAuthnJsModule.CreateCredentialAsync (import of web-authn.js, not window.Spa)
//   3. HTTP AddPasskey (authenticated attach)
// Both HTTP legs go through IWebServerApiService inside this ActionSet (not the page, not a
// ceremony client GetResponse for Settings). Outcomes go to the shell region: API Fail
// publishes ProblemDetailsNotification, a JSException publishes an Error OutcomeNotification,
// success publishes "Passkey created." (handlers never dispatch actions — TWS0002). CeremonyFailed
// is the page-facing flag; callers sequence FetchCredentials only when it is false so Fetch
// cannot mask the failure. Task 169 + 247.
// Task 248-001: the browser JSON also carries authenticatorAttachment + transports (registration
// context hints, forwarded verbatim); success records PendingNicknameCredentialId with the
// response's ProviderLabel as the prefill so the UI can prompt "name this passkey". Ownership defaults
// to the list (PendingNicknameOwnedByPrompt=false); AddPasskeyPrompt claims it right after success.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Text.Json;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Services;
using TimeWarp.Foundation.Types;

partial class CredentialsState
{
  public static class AddPasskeyActionSet
  {
    [TrackAction]
    public sealed class Action : IBaseAction
    {
      public Action()
      {
      }

      public Action(string nickname)
      {
        Nickname = nickname;
      }

      public string? Nickname { get; }
    }

    internal sealed class Handler : BaseHandler<Action>
    {
      private readonly IWebServerApiService ApiService;
      private readonly IJSRuntime JsRuntime;
      private readonly AuthenticationStateProvider AuthenticationStateProvider;
      private readonly IPublisher<ClientPipeline> Publisher;

      public Handler
      (
        IStore store,
        IWebServerApiService apiService,
        IJSRuntime jsRuntime,
        AuthenticationStateProvider authenticationStateProvider,
        IPublisher<ClientPipeline> publisher
      ) : base(store)
      {
        ApiService = apiService;
        JsRuntime = jsRuntime;
        AuthenticationStateProvider = authenticationStateProvider;
        Publisher = publisher;
      }

      public override async ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        CredentialsState.CeremonyFailed = false;

        try
        {
          OneOf<StartPasskeyRegistration.Response, FileResponse, SharedProblemDetails> startResult =
            await ApiService.GetResponse<StartPasskeyRegistration.Response>(
              new StartPasskeyRegistration.Command { ForCurrentAccount = true },
              cancellationToken);

          if (!startResult.IsT0)
          {
            await FailAsync(ToProblem(startResult), cancellationToken);
            return;
          }

          string credentialJson =
            await WebAuthnJsModule.CreateCredentialAsync(
              JsRuntime,
              startResult.AsT0.OptionsJson,
              preferHybrid: false,
              cancellationToken);

          using var document = JsonDocument.Parse(credentialJson);
          JsonElement root = document.RootElement;

          Guid userId = await ResolveUserIdAsync();
          AddPasskey.Command completeCommand = new()
          {
            UserId = userId,
            CredentialId = root.GetProperty("credentialId").GetString()!,
            ClientDataJson = root.GetProperty("clientDataJson").GetString()!,
            AttestationObject = root.GetProperty("attestationObject").GetString()!,
            Nickname = action.Nickname,
            AuthenticatorAttachment = ReadOptionalString(root, "authenticatorAttachment"),
            Transports = ReadOptionalStrings(root, "transports")
          };

          OneOf<AddPasskey.Response, FileResponse, SharedProblemDetails> completeResult =
            await ApiService.GetResponse<AddPasskey.Response>(completeCommand, cancellationToken);

          if (!completeResult.IsT0)
          {
            await FailAsync(ToProblem(completeResult), cancellationToken);
            return;
          }

          CredentialsState.LastAddedCredentialId = completeResult.AsT0.CredentialId.Value;
          CredentialsState.PendingNicknameCredentialId = completeResult.AsT0.CredentialId.Value;
          CredentialsState.PendingNicknameDefault = completeResult.AsT0.ProviderLabel;
          CredentialsState.PendingNicknameOwnedByPrompt = false; // the list owns it unless the prompt claims it
          await Publisher.Publish
          (
            new OutcomeNotification(MessageBarIntent.Success, "Passkey created."),
            cancellationToken
          );
        }
        catch (JSException jsException)
        {
          CredentialsState.CeremonyFailed = true;
          await Publisher.Publish
          (
            new OutcomeNotification
            (
              MessageBarIntent.Error,
              "The browser could not complete the passkey ceremony",
              jsException.Message
            ),
            cancellationToken
          );
        }
      }

      private Task FailAsync(SharedProblemDetails problem, CancellationToken cancellationToken)
      {
        CredentialsState.CeremonyFailed = true;
        return Publisher.Publish(new ProblemDetailsNotification(problem), cancellationToken);
      }

      private static string? ReadOptionalString(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out JsonElement element) && element.ValueKind == JsonValueKind.String
          ? element.GetString()
          : null;

      private static List<string>? ReadOptionalStrings(JsonElement root, string propertyName)
      {
        if (!root.TryGetProperty(propertyName, out JsonElement element) || element.ValueKind != JsonValueKind.Array)
        {
          return null;
        }

        List<string> values = [];
        foreach (JsonElement item in element.EnumerateArray())
        {
          if (item.ValueKind == JsonValueKind.String && item.GetString() is { Length: > 0 } value)
          {
            values.Add(value);
          }
        }

        return values.Count == 0 ? null : values;
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
