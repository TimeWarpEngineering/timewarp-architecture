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
#endregion

namespace TimeWarp.Architecture.Features.Account;

using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Services;
using TimeWarp.Foundation.Types;

[Page("/Login")]
partial class LoginPage
{
  [Inject] private PasskeyCeremonyClient Ceremony { get; set; } = null!;

  private string? ErrorMessage;
  private string? StatusMessage;
  private bool IsBusy;
  private bool? IsAuthenticated;

  protected override async Task OnInitializedAsync()
  {
    await base.OnInitializedAsync();
    await RefreshSessionAsync();
  }

  private async Task ContinueWithPasskey()
  {
    ErrorMessage = null;
    StatusMessage = null;
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

      StatusMessage = $"Signed in. PrincipalId: {result.AsT0.PrincipalId}";
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

  private async Task CreatePasskey()
  {
    ErrorMessage = null;
    StatusMessage = null;
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

      StatusMessage = $"Passkey created and signed in. PrincipalId: {result.AsT0.PrincipalId}";
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
    IsAuthenticated = await Ceremony.GetIsAuthenticatedAsync(CancellationToken.None);
  }
}
