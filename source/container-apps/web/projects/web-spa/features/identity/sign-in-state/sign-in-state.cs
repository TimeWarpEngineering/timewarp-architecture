#region Purpose
// SPA state for signing in: session presence, Microsoft 365 availability, and the passkey / Microsoft 365 ceremonies' outcomes.
#endregion

#region Design
// Every sign-in interaction on Login, Choose Microsoft 365 and the Passkeys demo is an ActionSet
// here, so the action catalog, State telemetry and headless tests see the same seam the buttons use.
// Pages dispatch and read; handlers own the ceremony (PasskeyCeremonyClient), the BFF calls, the
// Entra challenge navigation (forceLoad) and the post-sign-in navigation.
// ReturnPath on an action is re-collapsed by LoginPage.GetSafeReturnUrl inside the handler: an
// action can be dispatched by something other than the page (palette, agent, test), so the
// open-redirect guard sits where the navigation happens. StayOnPage ("") on the passkey actions
// means "stay here" (Passkeys demo) and publishes a success outcome instead of navigating.
// Outcomes go to NotificationState via OutcomeNotification / ProblemDetailsNotification (TWA0025).
// CeremonyFailed is the page-facing flag, set false at the start of each ceremony so callers can
// sequence follow-up actions only after a success.
// Microsoft365ChoiceValid: null = not checked yet, false = the parked Entra sign-in expired (load
// said so, or a completion problem mentioned "expired").
// Microsoft365Offered: null = not fetched, false = the server does not offer Microsoft 365
// (including a failed read), true = offered. The unavailable line renders only for false.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

[StateAccess]
public sealed partial class SignInState : State<SignInState>
{
  /// <summary>ReturnPath value that keeps the passkey ceremonies on the current page (no navigation).</summary>
  /// <remarks>Empty rather than null: the ActionSet method generator does not carry nullable parameters.</remarks>
  public const string StayOnPage = "";

  /// <summary>Session presence from GetCurrentSession; null until fetched or when the session call failed.</summary>
  public bool? IsAuthenticated { get; private set; }

  /// <summary>
  /// Whether the server offers Microsoft 365 sign-in (scheme registered and site policy on).
  /// Null until <c>FetchMicrosoft365Offered</c> finishes.
  /// </summary>
  public bool? Microsoft365Offered { get; private set; }

  /// <summary>Whether a parked Microsoft 365 sign-in is still waiting for create / already-have; null until checked.</summary>
  public bool? Microsoft365ChoiceValid { get; private set; }

  /// <summary>True when the last sign-in ceremony failed; the failure itself is on NotificationState.</summary>
  public bool CeremonyFailed { get; private set; }

  /// <summary>Credential minted by the last successful create-account ceremony.</summary>
  public Guid? LastRegisteredCredentialId { get; private set; }

  /// <summary>Provider name of the last registered passkey (nickname prefill).</summary>
  public string? LastRegisteredProviderLabel { get; private set; }

  public override void Initialize()
  {
    IsAuthenticated = null;
    Microsoft365Offered = null;
    Microsoft365ChoiceValid = null;
    CeremonyFailed = false;
    LastRegisteredCredentialId = null;
    LastRegisteredProviderLabel = null;
  }
}
