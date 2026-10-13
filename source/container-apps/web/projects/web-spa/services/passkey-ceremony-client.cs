#region Purpose
// SPA client for the first-party WebAuthn passkey register/authenticate/session ceremonies.
#endregion

#region Design
// Task 104-016: both the product Login page and the technical Passkeys demo need the same
// options → browser credential → complete → session flow. Centralising it here keeps the
// ceremony payload mapping (JSON field names ↔ Complete* command shapes) and error formatting
// in one place rather than duplicating across razor code-behinds.
// Injects IWebServerApiService (real BFF or mock fallback) and IJSRuntime. Task 200: passkey
// browser calls go through WebAuthnJsModule (IJSRuntime import of ./js/features/web-authn.js
// named exports) so Login does not require window.Spa. Mock mode has no GetMockResponseFactory
// on ceremony contracts, so MockWebApiService falls through to a 501 SharedProblemDetails —
// callers report that SharedProblemDetails through NotificationState.ReportProblem (task 247:
// no page-local formatting or bars).
// Cookie session is set by the server on complete; this client only reads IsAuthenticated via
// GetCurrentSession. No profile fields are collected or required.
// Task 350: every completion that ISSUES the session (CompletePasskeyRegistration,
// CompletePasskeyAuthentication, CompleteEntraBootstrapExisting, CompleteEntraBootstrapCreate) goes
// out through the browser — HttpApiService over BrowserFetchHttpMessageHandler (web-authn.js
// SendJson, credentials: "include"), BaseAddress = NavigationManager.BaseUri — in BOTH render modes.
// Under InteractiveServer, IWebServerApiService is web-server's own self-call HttpClient, so the
// session Set-Cookie landed server-side and the user was never signed in. One path for both modes
// keeps a single tested route; WASM just trades HttpClient's fetch for the module's fetch. Start
// calls and GetCurrentSession stay on IWebServerApiService (they issue nothing; the challenge is
// server-side). The circuit's auth state is fixed for its lifetime, so the action handlers finish a
// successful completion with NavigateTo(..., forceLoad: true): the next request carries the cookie.
// Option A (one-time ticket redeemed by a browser-protocol endpoint) was rejected: more machinery
// plus replay/expiry rules for the same outcome. The ceremony still runs through the mediator
// handler behind the contract route; nothing is duplicated server-side.
// When SPA uses IdentitySessionAuthenticationStateProvider (default non-mock / non-Entra path),
// successful complete notifies that provider so AuthorizeView re-reads session without a full reload
// (the StayOnPage developer demo under WebAssembly; a Server circuit keeps its starting principal).
// Task 165: AuthenticateAsync/RegisterAsync accept preferHybrid — when true, the JS bridge sets
// WebAuthn hints to ["hybrid"] so Chrome prioritizes nearby-device / QR UI (server options already
// include client-device+hybrid soft hints for the default path).
// Task 169: credential management HTTP (GetCredentials / AddPasskey / RevokeCredential) lives on
// CredentialsState ActionSets — not this client. Settings must not call ApiService via ceremony.
// [SideEffectService] (task 265): components never call this directly — TWA0026; action handlers do.
#endregion

namespace TimeWarp.Architecture.Services;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Foundation.Types;

[SideEffectService]
public sealed class PasskeyCeremonyClient
{
  private readonly IWebServerApiService ApiService;
  private readonly IJSRuntime JsRuntime;
  private readonly AuthenticationStateProvider AuthenticationStateProvider;
  private readonly NavigationManager NavigationManager;
  private readonly JsonSerializerOptions JsonSerializerOptions;

  public PasskeyCeremonyClient
  (
    IWebServerApiService apiService,
    IJSRuntime jsRuntime,
    AuthenticationStateProvider authenticationStateProvider,
    NavigationManager navigationManager,
    IOptions<JsonSerializerOptions> jsonSerializerOptions
  )
  {
    ApiService = apiService;
    JsRuntime = jsRuntime;
    AuthenticationStateProvider = authenticationStateProvider;
    NavigationManager = navigationManager;
    JsonSerializerOptions = jsonSerializerOptions.Value;
  }

  public async Task<OneOf<CompletePasskeyRegistration.Response, SharedProblemDetails>> RegisterAsync(
    CancellationToken cancellationToken,
    bool preferHybrid = false)
  {
    OneOf<StartPasskeyRegistration.Response, FileResponse, SharedProblemDetails> startResult =
      await ApiService.GetResponse<StartPasskeyRegistration.Response>(
        new StartPasskeyRegistration.Command(),
        cancellationToken);

    if (!startResult.IsT0)
    {
      return ToProblem(startResult);
    }

    string credentialJson =
      await WebAuthnJsModule.CreateCredentialAsync(
        JsRuntime,
        startResult.AsT0.OptionsJson,
        preferHybrid,
        cancellationToken);

    using var document = JsonDocument.Parse(credentialJson);
    JsonElement root = document.RootElement;

    CompletePasskeyRegistration.Command completeCommand = new()
    {
      CredentialId = root.GetProperty("credentialId").GetString()!,
      ClientDataJson = root.GetProperty("clientDataJson").GetString()!,
      AttestationObject = root.GetProperty("attestationObject").GetString()!,
      AuthenticatorAttachment = ReadOptionalString(root, "authenticatorAttachment"),
      Transports = ReadOptionalStrings(root, "transports")
    };

    OneOf<CompletePasskeyRegistration.Response, FileResponse, SharedProblemDetails> completeResult =
      await CompleteInBrowserAsync<CompletePasskeyRegistration.Response>(completeCommand, cancellationToken);

    if (completeResult.IsT0)
    {
      NotifyIdentitySessionIfNeeded();
      return completeResult.AsT0;
    }

    return ToProblem(completeResult);
  }

  public async Task<OneOf<CompletePasskeyAuthentication.Response, SharedProblemDetails>> AuthenticateAsync(
    CancellationToken cancellationToken,
    bool preferHybrid = false)
  {
    OneOf<StartPasskeyAuthentication.Response, FileResponse, SharedProblemDetails> startResult =
      await ApiService.GetResponse<StartPasskeyAuthentication.Response>(
        new StartPasskeyAuthentication.Command(),
        cancellationToken);

    if (!startResult.IsT0)
    {
      return ToProblem(startResult);
    }

    string assertionJson =
      await WebAuthnJsModule.GetCredentialAsync(
        JsRuntime,
        startResult.AsT0.OptionsJson,
        preferHybrid,
        cancellationToken);

    using var document = JsonDocument.Parse(assertionJson);
    JsonElement root = document.RootElement;

    CompletePasskeyAuthentication.Command completeCommand = new()
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

    OneOf<CompletePasskeyAuthentication.Response, FileResponse, SharedProblemDetails> completeResult =
      await CompleteInBrowserAsync<CompletePasskeyAuthentication.Response>(completeCommand, cancellationToken);

    if (completeResult.IsT0)
    {
      NotifyIdentitySessionIfNeeded();
      return completeResult.AsT0;
    }

    return ToProblem(completeResult);
  }

  public async Task<OneOf<CompleteEntraBootstrapExisting.Response, SharedProblemDetails>> CompleteEntraChoiceExistingAsync(
    CancellationToken cancellationToken,
    bool preferHybrid = false)
  {
    OneOf<StartPasskeyAuthentication.Response, FileResponse, SharedProblemDetails> startResult =
      await ApiService.GetResponse<StartPasskeyAuthentication.Response>(
        new StartPasskeyAuthentication.Command(),
        cancellationToken);

    if (!startResult.IsT0)
    {
      return ToProblem(startResult);
    }

    string assertionJson =
      await WebAuthnJsModule.GetCredentialAsync(
        JsRuntime,
        startResult.AsT0.OptionsJson,
        preferHybrid,
        cancellationToken);

    using var document = JsonDocument.Parse(assertionJson);
    JsonElement root = document.RootElement;

    CompleteEntraBootstrapExisting.Command completeCommand = new()
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

    OneOf<CompleteEntraBootstrapExisting.Response, FileResponse, SharedProblemDetails> completeResult =
      await CompleteInBrowserAsync<CompleteEntraBootstrapExisting.Response>(completeCommand, cancellationToken);

    if (completeResult.IsT0)
    {
      NotifyIdentitySessionIfNeeded();
      return completeResult.AsT0;
    }

    return ToProblem(completeResult);
  }

  /// <summary>Turns the parked Microsoft 365 sign-in into a new account; the session is issued to the browser.</summary>
  public async Task<OneOf<CompleteEntraBootstrapCreate.Response, SharedProblemDetails>> CompleteEntraChoiceCreateAsync(
    CancellationToken cancellationToken)
  {
    OneOf<CompleteEntraBootstrapCreate.Response, FileResponse, SharedProblemDetails> completeResult =
      await CompleteInBrowserAsync<CompleteEntraBootstrapCreate.Response>(
        new CompleteEntraBootstrapCreate.Command(),
        cancellationToken);

    if (completeResult.IsT0)
    {
      NotifyIdentitySessionIfNeeded();
      return completeResult.AsT0;
    }

    return ToProblem(completeResult);
  }

  public async Task<bool?> GetIsAuthenticatedAsync(CancellationToken cancellationToken)
  {
    OneOf<GetCurrentSession.Response, FileResponse, SharedProblemDetails> sessionResult =
      await ApiService.GetResponse<GetCurrentSession.Response>(new GetCurrentSession.Query(), cancellationToken);

    return sessionResult.IsT0 ? sessionResult.AsT0.IsAuthenticated : null;
  }

  // Settings credential list/add/revoke HTTP lives on CredentialsState (task 169). This client
  // is the WebAuthn ceremony helper for Login / Passkeys demo only.

  // Session-issuing completions: the browser sends the request so it receives the Set-Cookie (task 350).
  private async Task<OneOf<TResponse, FileResponse, SharedProblemDetails>> CompleteInBrowserAsync<TResponse>(
    IApiRequest request,
    CancellationToken cancellationToken)
    where TResponse : class
  {
    using var handler = new BrowserFetchHttpMessageHandler(JsRuntime);
    using var httpClient = new HttpClient(handler, disposeHandler: false)
    {
      BaseAddress = new Uri(NavigationManager.BaseUri)
    };
    var browserApi = new HttpApiService(httpClient, JsonSerializerOptions);
    return await browserApi.GetResponse<TResponse>(request, cancellationToken);
  }

  private void NotifyIdentitySessionIfNeeded()
  {
    if (AuthenticationStateProvider is IdentitySessionAuthenticationStateProvider identitySession)
    {
      identitySession.NotifySessionChanged();
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

  // Task 248-001: registration-context hints from web-authn.ts (both optional).
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
}
