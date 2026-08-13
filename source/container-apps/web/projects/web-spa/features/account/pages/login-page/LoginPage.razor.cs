#region Purpose
// Exists only for the [Page("/Login")] route registration; markup and all behavior live in LoginPage.razor.
#endregion

#region Design
// Task 104-016 product CTA + 147-005 focused chrome. Account = accepted public key (locked
// decision #1): primary action is discoverable passkey authentication (no email/username),
// secondary is registration that mints Principal + session with no mandatory profile.
// Markup uses TimeWarpFocusedPage (logo + centered card) — not TimeWarpPage — so login is not
// "a page in the product shell". Progressive profile is 104-024 and stays out of this page.
// Ceremony plumbing lives in PasskeyCeremonyClient so the technical Passkeys demo and this page
// share one mapping of browser credential JSON → Complete* commands.
// Mock mode: ceremony contracts have no GetMockResponseFactory, so the mock chain yields 501 and
// we surface it through ErrorMessage (same as PasskeysPage).
// Task 153 redirect flow: an already-authenticated visitor is redirected away immediately, and a
// successful ceremony navigates to ?returnUrl (or home). returnUrl is honored only when local
// (GetSafeReturnUrl — open-redirect guard) and never points back at /Login itself.
// Create account mints a NEW Principal; after success (no returnUrl) navigate to /Settings so
// the user lands on the passkey list (passkeys.io post-create UX, task 167). Sign-in still
// uses returnUrl/home. Credential management is Settings, never this page.
// Hybrid/nearby device: one "Sign in with a passkey" button opens the browser modal. Server
// options include soft hints [client-device, hybrid] and empty allowCredentials (165) so the
// dialog can offer local managers and nearby-device/QR (e.g. after canceling Proton Pass).
// Conditional autofill + empty text field (166) was rejected — no field to type into.
// No second site button for nearby; the browser owns that path inside the same ceremony.
#endregion

namespace TimeWarp.Architecture.Features.Account;

// Public passkey entry. Anonymous; authenticated visitors are redirected away (task 153).
[Page("/Login")]
partial class LoginPage;
