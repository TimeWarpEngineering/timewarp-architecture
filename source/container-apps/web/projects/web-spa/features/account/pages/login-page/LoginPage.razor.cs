#region Purpose
// Code-behind for the template's passkey-first human sign-in page: Sign in with a passkey /
// Create account against the first-party WebAuthn identity-session cookie (no profile form).
#endregion

#region Design
// Task 104-016 product CTA + 147-005 focused chrome. Account = accepted public key (locked
// decision #1): primary action is discoverable passkey authentication (no email/username as
// identity), secondary is registration that mints Principal + session with no mandatory profile.
// Markup uses TimeWarpFocusedPage (logo + centered card) — not TimeWarpPage — so login is not
// "a page in the product shell". Progressive profile is 104-024 and stays out of this page.
// Ceremony plumbing lives in PasskeyCeremonyClient so the technical Passkeys demo and this page
// share one mapping of browser credential JSON → Complete* commands.
// Mock mode: ceremony contracts have no GetMockResponseFactory, so the mock chain yields 501 and
// we surface it through ErrorMessage (same as PasskeysPage).
// Task 153 redirect flow: an already-authenticated visitor is redirected away immediately, and a
// successful ceremony navigates to ?returnUrl (or home). returnUrl is honored only when local
// (GetSafeReturnUrl — open-redirect guard) and never points back at /Login itself. Credential
// management for signed-in users is a Settings/Security concern (104-024), NOT this page —
// CreatePasskey mints a NEW Principal, so a signed-in user clicking it would create a second
// account, not add a credential.
// Task 166 — conditional UI (the hanko/passkeys.io "Passkeys from a Nearby Device" path):
// On interactive load we start navigator.credentials.get({ mediation: "conditional" }) once the
// browser supports it. That request stays pending until the user focuses an input with
// autocomplete="username webauthn" and picks a passkey OR "Passkeys from a Nearby Device" from
// the browser autofill menu. The menu item is browser-owned — we only enable conditional get +
// the autofill anchor. The field value is never sent as an identifier (discoverable credentials).
// Modal "Sign in with a passkey" aborts conditional first (only one get at a time), then runs
// the standard modal get. After modal cancel/fail we re-arm conditional when still on the page.
// Task 165 hybrid hints remain on server options (client-device + hybrid soft preference).
#endregion

namespace TimeWarp.Architecture.Features.Account;

using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Services;
using TimeWarp.Foundation.Types;

// Public passkey entry. Anonymous; authenticated visitors are redirected away (task 153).
[Page("/Login")]
partial class LoginPage : IAsyncDisposable
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
  private bool ShowAutofillAnchor;
  private CancellationTokenSource? ConditionalLoopCts;
  private bool ConditionalLoopStarted;

  protected override async Task OnInitializedAsync()
  {
    await base.OnInitializedAsync();
    await RefreshSessionAsync();

    if (IsAuthenticated is true)
    {
      NavigateOnward();
    }
  }

  protected override async Task OnAfterRenderAsync(bool firstRender)
  {
    await base.OnAfterRenderAsync(firstRender);

    // Conditional get requires a live browser + interactive circuit (JS interop).
    if (!firstRender || ConditionalLoopStarted || IsAuthenticated is true)
    {
      return;
    }

    if (!RendererInfo.IsInteractive)
    {
      return;
    }

    ConditionalLoopStarted = true;

    try
    {
      ShowAutofillAnchor = await Ceremony.IsConditionalMediationAvailableAsync(CancellationToken.None);
    }
    catch (JSException)
    {
      ShowAutofillAnchor = false;
    }

    if (ShowAutofillAnchor)
    {
      await InvokeAsync(StateHasChanged);
      ConditionalLoopCts = new CancellationTokenSource();
      _ = RunConditionalAuthenticationLoopAsync(ConditionalLoopCts.Token);
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
    // Modal path — abort pending conditional get so the browser can open a modal dialog.
    try
    {
      await Ceremony.AbortConditionalAsync(CancellationToken.None);
    }
    catch (JSException)
    {
      /* ignore */
    }

    await AuthenticateModalAsync();

    // Re-arm autofill if we stayed on the page (error / cancel).
    if (ShowAutofillAnchor && IsAuthenticated is not true && ConditionalLoopCts is { IsCancellationRequested: false })
    {
      _ = RunConditionalAuthenticationLoopAsync(ConditionalLoopCts.Token);
    }
  }

  private async Task AuthenticateModalAsync()
  {
    ErrorMessage = null;
    IsBusy = true;
    try
    {
      OneOf<CompletePasskeyAuthentication.Response, SharedProblemDetails> result =
        await Ceremony.AuthenticateAsync(CancellationToken.None, preferHybrid: false);

      if (result.IsT1)
      {
        ErrorMessage = PasskeyCeremonyClient.FormatError(result.AsT1);
        return;
      }

      NavigateOnward();
    }
    catch (JSException jsException)
    {
      // User cancel surfaces as NotAllowedError — keep message short and non-alarming.
      ErrorMessage = jsException.Message.Contains("NotAllowed", StringComparison.OrdinalIgnoreCase)
        ? null
        : $"The browser could not complete the passkey ceremony: {jsException.Message}";
    }
    finally
    {
      IsBusy = false;
    }
  }

  private async Task RunConditionalAuthenticationLoopAsync(CancellationToken cancellationToken)
  {
    while (!cancellationToken.IsCancellationRequested)
    {
      try
      {
        OneOf<CompletePasskeyAuthentication.Response, SharedProblemDetails>? result =
          await Ceremony.AuthenticateConditionalAsync(cancellationToken);

        if (cancellationToken.IsCancellationRequested)
        {
          return;
        }

        // null = aborted for modal / dispose — stop loop; modal path restarts if needed.
        if (result is null)
        {
          return;
        }

        if (result.Value.IsT1)
        {
          ErrorMessage = PasskeyCeremonyClient.FormatError(result.Value.AsT1);
          await InvokeAsync(StateHasChanged);
          // Challenge expired / server rejected — mint a fresh conditional start next iteration.
          continue;
        }

        NavigateOnward();
        return;
      }
      catch (JSException jsException) when (!cancellationToken.IsCancellationRequested)
      {
        // NotAllowedError: user dismissed autofill without selecting — re-arm with a new challenge.
        if (jsException.Message.Contains("NotAllowed", StringComparison.OrdinalIgnoreCase)
          || jsException.Message.Contains("AbortError", StringComparison.OrdinalIgnoreCase))
        {
          continue;
        }

        ErrorMessage = $"The browser could not complete the passkey ceremony: {jsException.Message}";
        await InvokeAsync(StateHasChanged);
        return;
      }
      catch (OperationCanceledException)
      {
        return;
      }
    }
  }

  private async Task CreatePasskey()
  {
    try
    {
      await Ceremony.AbortConditionalAsync(CancellationToken.None);
    }
    catch (JSException)
    {
      /* ignore */
    }

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
      if (ShowAutofillAnchor && IsAuthenticated is not true && ConditionalLoopCts is { IsCancellationRequested: false })
      {
        _ = RunConditionalAuthenticationLoopAsync(ConditionalLoopCts.Token);
      }
    }
  }

  private async Task RefreshSessionAsync()
  {
    IsAuthenticated = await Ceremony.GetIsAuthenticatedAsync(CancellationToken.None);
  }

  public async ValueTask DisposeAsync()
  {
    if (ConditionalLoopCts is not null)
    {
      await ConditionalLoopCts.CancelAsync();
      ConditionalLoopCts.Dispose();
      ConditionalLoopCts = null;
    }

    try
    {
      await Ceremony.AbortConditionalAsync(CancellationToken.None);
    }
    catch
    {
      /* page may already be gone */
    }

    GC.SuppressFinalize(this);
  }
}
