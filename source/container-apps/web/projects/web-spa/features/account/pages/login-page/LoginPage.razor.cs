#region Purpose
// Code-behind for the template's passkey-first human sign-in page: Continue with passkey /
// Create a passkey against the first-party WebAuthn identity-session cookie (no profile form).
#endregion

#region Design
// Task 104-016 product CTA. Account = accepted public key (locked decision #1): primary action is
// discoverable passkey authentication (no email/username), secondary is registration that mints
// Principal + session with no mandatory profile. Progressive profile is 104-024 and stays out of
// this page. Ceremony plumbing lives in PasskeyCeremonyClient so the technical Passkeys demo and
// this page share one mapping of browser credential JSON → Complete* commands.
// Legacy Passwordless.dev (CDN client, tenant key, PasswordlessService, RegisterPasskey.razor) is
// removed under this task — only window.Spa.WebAuthn + identity contracts remain.
// Mock mode: ceremony contracts have no GetMockResponseFactory, so the mock chain yields 501 and
// we surface it through ErrorMessage (same as PasskeysPage).
// Task 153 redirect flow: an already-authenticated visitor is redirected away immediately, and a
// successful ceremony navigates to ?returnUrl (or home). returnUrl is honored only when local
// (GetSafeReturnUrl — open-redirect guard) and never points back at /Login itself. Credential
// management for signed-in users is a Settings/Security concern (104-024), NOT this page — the
// old "signed-in users may open it to add credentials" note was wrong: CreatePasskey mints a NEW
// Principal, so a signed-in user clicking it would create a second account, not add a credential.
#endregion

namespace TimeWarp.Architecture.Features.Account;

using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Services;
using TimeWarp.Foundation.Types;

// Public passkey entry. Anonymous; authenticated visitors are redirected away (task 153).
[Page("/Login")]
partial class LoginPage
{
  [Inject] private PasskeyCeremonyClient Ceremony { get; set; } = null!;
  [Inject] private NavigationManager NavigationManager { get; set; } = null!;

  [SuppressMessage
  (
    "Design",
    "CA1056:URI-like properties should not be strings",
    Justification = "SupplyParameterFromQuery binds strings; the raw value is validated by GetSafeReturnUrl."
  )]
  [Parameter] [SupplyParameterFromQuery(Name = "returnUrl")] public string? ReturnUrl { get; set; }

  private string? ErrorMessage;
  private bool IsBusy;
  private bool? IsAuthenticated;

  protected override async Task OnInitializedAsync()
  {
    await base.OnInitializedAsync();
    await RefreshSessionAsync();

    if (IsAuthenticated is true)
    {
      NavigateOnward();
    }
  }

  /// <summary>
  /// Collapses a requested return URL to a safe local destination: relative paths only
  /// (no absolute/protocol-relative URLs — open-redirect guard) and never /Login itself
  /// (redirect loop guard). Everything else falls back to home.
  /// </summary>
  internal static string GetSafeReturnUrl(string? returnUrl)
  {
    if (string.IsNullOrEmpty(returnUrl)
      || !returnUrl.StartsWith('/')
      || returnUrl.StartsWith("//", StringComparison.Ordinal)
      || returnUrl.StartsWith("/\\", StringComparison.Ordinal))
    {
      return "/";
    }

    string path = returnUrl.Split('?', '#')[0].TrimEnd('/');
    return path.Equals(GetPageUrl(), StringComparison.OrdinalIgnoreCase) ? "/" : returnUrl;
  }

  private void NavigateOnward() => NavigationManager.NavigateTo(GetSafeReturnUrl(ReturnUrl));

  private async Task ContinueWithPasskey()
  {
    ErrorMessage = null;
    IsBusy = true;
    try
    {
      OneOf<CompletePasskeyAuthentication.Response, SharedProblemDetails> result =
        await Ceremony.AuthenticateAsync(CancellationToken.None);

      if (result.IsT1)
      {
        ErrorMessage = PasskeyCeremonyClient.FormatError(result.AsT1);
        return;
      }

      NavigateOnward();
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

  private async Task CreatePasskey()
  {
    ErrorMessage = null;
    IsBusy = true;
    try
    {
      OneOf<CompletePasskeyRegistration.Response, SharedProblemDetails> result =
        await Ceremony.RegisterAsync(CancellationToken.None);

      if (result.IsT1)
      {
        ErrorMessage = PasskeyCeremonyClient.FormatError(result.AsT1);
        return;
      }

      NavigateOnward();
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
    IsAuthenticated = await Ceremony.GetIsAuthenticatedAsync(CancellationToken.None);
  }
}
