#region Purpose
// Code-behind for the /Passkeys demo page: registers and authenticates a WebAuthn passkey against
// the identity-session cookie, calling the contract endpoints directly.
#endregion

#region Design
// Deliberately bypasses the TimeWarp.State action/store pipeline (ApiHandler/DefaultApiHandler):
// this is a minimal demo of the raw ceremony (task 104-003's own scope — full CTA/login UX
// integration is 104-016), so IWebServerApiService and IJSRuntime are injected directly, mirroring
// the legacy RegisterPasskey.razor's direct-injection style rather than introducing an
// action/state/reducer trio for a two-button demo page.
// StartPasskeyRegistration/StartPasskeyAuthentication/CompletePasskeyRegistration/
// CompletePasskeyAuthentication carry no GetMockResponseFactory (see their contracts' Design
// regions), so under MOCK_WEB_API, MockWebApiService's built-in "no factory found" fallback runs
// these through its real inner IApiService — which in a mock-first/no-BFF app is NullApiService,
// producing a 501 SharedProblemDetails automatically. That IS this page's "passkeys not supported"
// story: no special-case mock-detection code needed here, the existing fallback chain already
// produces a sensible error through the same ErrorMessage path every other failure uses.
// window.Spa.WebAuthn.CreateCredential/GetCredential (source/features/web-authn.ts) do the binary
// ArrayBuffer<->base64url conversion in the browser; this code-behind only shuttles JSON strings
// and maps field names onto the Complete* commands.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using System.Text.Json;
using Microsoft.JSInterop;

[Page("/Passkeys")]
partial class PasskeysPage
{
  private string? ErrorMessage;
  private string? StatusMessage;
  private bool IsBusy;
  private bool? IsAuthenticated;

  protected override async Task OnInitializedAsync()
  {
    await base.OnInitializedAsync();
    await RefreshSessionAsync();
  }

  private async Task RegisterPasskey()
  {
    ErrorMessage = null;
    StatusMessage = null;
    IsBusy = true;
    try
    {
      OneOf<StartPasskeyRegistration.Response, FileResponse, SharedProblemDetails> startResult =
        await ApiService.GetResponse<StartPasskeyRegistration.Response>(new StartPasskeyRegistration.Command(), CancellationToken.None);

      if (!startResult.IsT0)
      {
        ErrorMessage = FormatError(startResult);
        return;
      }

      string credentialJson = await JsRuntime.InvokeAsync<string>("Spa.WebAuthn.CreateCredential", startResult.AsT0.OptionsJson);

      using var document = JsonDocument.Parse(credentialJson);
      JsonElement root = document.RootElement;

      CompletePasskeyRegistration.Command completeCommand = new()
      {
        CredentialId = root.GetProperty("credentialId").GetString()!,
        ClientDataJson = root.GetProperty("clientDataJson").GetString()!,
        AttestationObject = root.GetProperty("attestationObject").GetString()!
      };

      OneOf<CompletePasskeyRegistration.Response, FileResponse, SharedProblemDetails> completeResult =
        await ApiService.GetResponse<CompletePasskeyRegistration.Response>(completeCommand, CancellationToken.None);

      if (!completeResult.IsT0)
      {
        ErrorMessage = FormatError(completeResult);
        return;
      }

      StatusMessage = $"Passkey registered. PrincipalId: {completeResult.AsT0.PrincipalId}";
      await RefreshSessionAsync();
    }
    catch (JSException jsException)
    {
      ErrorMessage = $"The browser could not complete the passkey ceremony: {jsException.Message}";
    }
    finally
    {
      IsBusy = false;
    }
  }

  private async Task AuthenticateWithPasskey()
  {
    ErrorMessage = null;
    StatusMessage = null;
    IsBusy = true;
    try
    {
      OneOf<StartPasskeyAuthentication.Response, FileResponse, SharedProblemDetails> startResult =
        await ApiService.GetResponse<StartPasskeyAuthentication.Response>(new StartPasskeyAuthentication.Command(), CancellationToken.None);

      if (!startResult.IsT0)
      {
        ErrorMessage = FormatError(startResult);
        return;
      }

      string assertionJson = await JsRuntime.InvokeAsync<string>("Spa.WebAuthn.GetCredential", startResult.AsT0.OptionsJson);

      using var document = JsonDocument.Parse(assertionJson);
      JsonElement root = document.RootElement;

      CompletePasskeyAuthentication.Command completeCommand = new()
      {
        CredentialId = root.GetProperty("credentialId").GetString()!,
        ClientDataJson = root.GetProperty("clientDataJson").GetString()!,
        AuthenticatorData = root.GetProperty("authenticatorData").GetString()!,
        Signature = root.GetProperty("signature").GetString()!,
        UserHandle = root.TryGetProperty("userHandle", out JsonElement userHandleElement) && userHandleElement.ValueKind == JsonValueKind.String
          ? userHandleElement.GetString()
          : null
      };

      OneOf<CompletePasskeyAuthentication.Response, FileResponse, SharedProblemDetails> completeResult =
        await ApiService.GetResponse<CompletePasskeyAuthentication.Response>(completeCommand, CancellationToken.None);

      if (!completeResult.IsT0)
      {
        ErrorMessage = FormatError(completeResult);
        return;
      }

      StatusMessage = $"Authenticated. PrincipalId: {completeResult.AsT0.PrincipalId}";
      await RefreshSessionAsync();
    }
    catch (JSException jsException)
    {
      ErrorMessage = $"The browser could not complete the passkey ceremony: {jsException.Message}";
    }
    finally
    {
      IsBusy = false;
    }
  }

  private async Task RefreshSessionAsync()
  {
    OneOf<GetCurrentSession.Response, FileResponse, SharedProblemDetails> sessionResult =
      await ApiService.GetResponse<GetCurrentSession.Response>(new GetCurrentSession.Query(), CancellationToken.None);

    IsAuthenticated = sessionResult.IsT0 && sessionResult.AsT0.IsAuthenticated;
  }

  private static string FormatError<TResponse>(OneOf<TResponse, FileResponse, SharedProblemDetails> result)
    where TResponse : class =>
    result.IsT2
      ? $"{result.AsT2.Title}: {result.AsT2.Detail}"
      : "An unexpected response was received.";
}
