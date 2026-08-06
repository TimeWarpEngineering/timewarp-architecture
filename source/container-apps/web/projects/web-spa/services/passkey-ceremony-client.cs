#region Purpose
// SPA client for the first-party WebAuthn passkey register/authenticate/session ceremonies.
#endregion

#region Design
// Task 104-016: both the product Login page and the technical Passkeys demo need the same
// options → browser credential → complete → session flow. Centralising it here keeps the
// ceremony payload mapping (JSON field names ↔ Complete* command shapes) and error formatting
// in one place rather than duplicating across razor code-behinds.
// Injects IWebServerApiService (real BFF or mock fallback) and IJSRuntime → window.Spa.WebAuthn
// (source/features/web-authn.ts). Mock mode has no GetMockResponseFactory on ceremony contracts,
// so MockWebApiService falls through to a 501 SharedProblemDetails — callers surface that as a
// "passkeys not supported" style error via FormatError, same as PasskeysPage before this extract.
// Cookie session is set by the server on complete; this client only reads IsAuthenticated via
// GetCurrentSession. No profile fields are collected or required.
// When SPA uses IdentitySessionAuthenticationStateProvider (default non-mock / non-Entra path),
// successful complete notifies that provider so AuthorizeView re-reads session without a full reload.
// Task 165: AuthenticateAsync/RegisterAsync accept preferHybrid — when true, the JS bridge sets
// WebAuthn hints to ["hybrid"] so Chrome prioritizes nearby-device / QR UI (server options already
// include client-device+hybrid soft hints for the default path).
#endregion

namespace TimeWarp.Architecture.Services;

using Microsoft.AspNetCore.Components.Authorization;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Foundation.Types;

public sealed class PasskeyCeremonyClient
{
  private readonly IWebServerApiService ApiService;
  private readonly IJSRuntime JsRuntime;
  private readonly AuthenticationStateProvider AuthenticationStateProvider;

  public PasskeyCeremonyClient
  (
    IWebServerApiService apiService,
    IJSRuntime jsRuntime,
    AuthenticationStateProvider authenticationStateProvider
  )
  {
    ApiService = apiService;
    JsRuntime = jsRuntime;
    AuthenticationStateProvider = authenticationStateProvider;
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
      await JsRuntime.InvokeAsync<string>(
        "Spa.WebAuthn.CreateCredential",
        cancellationToken,
        startResult.AsT0.OptionsJson,
        preferHybrid);

    using var document = JsonDocument.Parse(credentialJson);
    JsonElement root = document.RootElement;

    CompletePasskeyRegistration.Command completeCommand = new()
    {
      CredentialId = root.GetProperty("credentialId").GetString()!,
      ClientDataJson = root.GetProperty("clientDataJson").GetString()!,
      AttestationObject = root.GetProperty("attestationObject").GetString()!
    };

    OneOf<CompletePasskeyRegistration.Response, FileResponse, SharedProblemDetails> completeResult =
      await ApiService.GetResponse<CompletePasskeyRegistration.Response>(completeCommand, cancellationToken);

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
      await JsRuntime.InvokeAsync<string>(
        "Spa.WebAuthn.GetCredential",
        cancellationToken,
        startResult.AsT0.OptionsJson,
        preferHybrid);

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
      await ApiService.GetResponse<CompletePasskeyAuthentication.Response>(completeCommand, cancellationToken);

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

  /// <summary>List the caller's credentials (passkeys + agent keys). Task 104-005 / Settings UI.</summary>
  public async Task<OneOf<GetCredentials.Response, SharedProblemDetails>> ListCredentialsAsync(
    CancellationToken cancellationToken,
    bool includeRevoked = false)
  {
    OneOf<GetCredentials.Response, FileResponse, SharedProblemDetails> result =
      await ApiService.GetResponse<GetCredentials.Response>(
        new GetCredentials.Query { IncludeRevoked = includeRevoked },
        cancellationToken);

    return result.IsT0 ? result.AsT0 : ToProblem(result);
  }

  /// <summary>Soft-revoke one of the caller's credentials (cannot revoke the last active passkey).</summary>
  public async Task<OneOf<RevokeCredential.Response, SharedProblemDetails>> RevokeCredentialAsync(
    Guid credentialId,
    CancellationToken cancellationToken)
  {
    OneOf<RevokeCredential.Response, FileResponse, SharedProblemDetails> result =
      await ApiService.GetResponse<RevokeCredential.Response>(
        new RevokeCredential.Command { CredentialId = credentialId },
        cancellationToken);

    return result.IsT0 ? result.AsT0 : ToProblem(result);
  }

  /// <summary>
  /// Register an additional passkey on the authenticated principal (not a new account).
  /// Uses anonymous Start options + authenticated AddPasskey complete (task 104-005).
  /// </summary>
  public async Task<OneOf<AddPasskey.Response, SharedProblemDetails>> AddPasskeyAsync(
    CancellationToken cancellationToken,
    string? label = null)
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
      await JsRuntime.InvokeAsync<string>(
        "Spa.WebAuthn.CreateCredential",
        cancellationToken,
        startResult.AsT0.OptionsJson,
        false);

    using var document = JsonDocument.Parse(credentialJson);
    JsonElement root = document.RootElement;

    AddPasskey.Command completeCommand = new()
    {
      CredentialId = root.GetProperty("credentialId").GetString()!,
      ClientDataJson = root.GetProperty("clientDataJson").GetString()!,
      AttestationObject = root.GetProperty("attestationObject").GetString()!,
      Label = label
    };

    OneOf<AddPasskey.Response, FileResponse, SharedProblemDetails> completeResult =
      await ApiService.GetResponse<AddPasskey.Response>(completeCommand, cancellationToken);

    return completeResult.IsT0 ? completeResult.AsT0 : ToProblem(completeResult);
  }

  public static string FormatError(SharedProblemDetails problem) =>
    $"{problem.Title}: {problem.Detail}";

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
}
