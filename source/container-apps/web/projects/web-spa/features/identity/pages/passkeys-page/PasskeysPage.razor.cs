#region Purpose
// Code-behind for the /Passkeys technical demo page: register/authenticate via the shared
// PasskeyCeremonyClient against the identity-session cookie.
#endregion

#region Design
// Product human CTA lives on /Login (task 104-016). This page remains a discoverable technical
// demo under Nav → Pages so operators can exercise the raw ceremony without the product copy.
// Ceremony mapping is shared via PasskeyCeremonyClient — do not reintroduce Passwordless.dev or
// direct passwordless.* JS interop here.
// Mock mode: ceremony contracts have no GetMockResponseFactory; mock chain yields 501 and we
// surface it through ErrorMessage.
// RP-ID credential scoping (task 104-031): register and authenticate on the SAME host.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using TimeWarp.Architecture.Services;
using TimeWarp.Foundation.Types;

// Technical ceremony demo — product CTA is /Login. Nav + route gated to Developer (147-001).
[Page("/Passkeys", Policy = Policies.CanViewDeveloperPage)]
[Authorize(Policy = Policies.CanViewDeveloperPage)]
partial class PasskeysPage
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

  private async Task RegisterPasskey()
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

      StatusMessage = $"Passkey registered. PrincipalId: {result.AsT0.PrincipalId}";
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
      OneOf<CompletePasskeyAuthentication.Response, SharedProblemDetails> result =
        await Ceremony.AuthenticateAsync(CancellationToken.None);

      if (result.IsT1)
      {
        ErrorMessage = PasskeyCeremonyClient.FormatError(result.AsT1);
        return;
      }

      StatusMessage = $"Authenticated. PrincipalId: {result.AsT0.PrincipalId}";
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
