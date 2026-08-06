# First-run home and login professional chrome

## Parent

147 (professional app shell)

## Description

Make the first-run experience read like a product, not a dev tool. Two surfaces:

1. **Login page** — redesigned to the passkeys.io focused pattern (maintainer-selected
   reference, 2026-08-06), WITHOUT the email/identifier path: passkey is the only mechanism.
2. **Home page** — differentiate anonymous vs signed-in, and remove demo residue from the
   professional surface.

Appbar/footer chrome is NOT in scope — already landed (tasks 156/157/162: horizontal logo
lockup, 70px appbar, purple/blue slot frames).

## Design — Login page (locked by maintainer)

Reference: passkeys.io sign-in screen, adapted:

- **Focused chrome**: centered single card on a calm background; TimeWarp horizontal logo above
  the card. NO nav rail, NO search, NO footer chrome — login is not "a page in the shell".
  Use the empty-layout + shell pattern (`tw-blazor-layout`): either a minimal focused-shell
  component pages can wrap instead of `TimeWarpPage`, or a bare layout for this route. Do NOT
  fork MainLayout into per-page layouts — one focused variant, reusable for future
  auth-adjacent screens (logout confirmation, etc.).
- **Card content** (top to bottom):
  - Heading: "Sign in"
  - Primary button: **"Sign in with a passkey"** (existing ContinueWithPasskey action;
    keep `data-qa="ContinueWithPasskey"`)
  - Subtle divider
  - "Don't have an account?" line with **"Create account"** link/button (existing CreatePasskey
    ceremony; keep `data-qa="CreatePasskey"`). Wording is "Create account", not "Create a
    passkey" — the passkey IS the account, but first-run users think in accounts.
  - Small "What is a passkey? **Learn more**" link.
- **Learn more click** → disclosure (inline expander or FluentDialog — implementer's pick,
  favor whichever reads calmer) containing explainer copy in the spirit of passkeys.io
  (paraphrase, don't copy verbatim):
  > A passkey is a way to sign in that works completely without passwords. Using your device's
  > own security — Touch ID, Face ID, Windows Hello, or a hardware key — passkeys are more
  > secure and easier to use than passwords and current 2FA methods.
- **No email field, no username, no password** — anywhere on the screen.
- Remove the debug "Session: signed in / not signed in" line entirely.
- **Already-authenticated visitors to /Login are redirected Home** (no interstitial).
- Error surface: keep the FluentMessageBar pattern for ceremony failures.
- CSS per `tw-blazor-css-strategy` (isolation-first, `--twe-*` tokens; no hard-coded values).

## Design — Home page (maintainer defaults; veto in review if wrong)

- **Anonymous**: keep Welcome + Built-with cards; keep the passkey CTA card but its button goes
  to the new focused /Login (unchanged route). Copy stays honest about being a template.
- **Signed-in** (`AuthorizeView`): replace the Sign-in card with a signed-in strip — avatar +
  alias (ProfileState already has both) and quick links: Settings, and Admin when the principal
  holds Administrator (reuse the policy constants; no new policy plumbing).
- **"Try it" demo card** (task buttons + modal) moves OFF home: relocate to the
  Developer-gated demos area (consistent with 147-001's gating philosophy). Home carries no
  demo actions.
- **Delete `ChangePasswordPage`** (account feature) — password-era residue in a passkey-first
  product. Verify nothing routes to it (grep `/ChangePassword`, nav, tests) before deletion;
  if something routes there, stop and note it instead of force-deleting.

## Requirements

- Both compile modes / all template flags unaffected (pure web-spa work; no `#if` regions
  expected — verify none needed).
- Preserve e2e hooks: `data-qa="ContinueWithPasskey"` and `data-qa="CreatePasskey"` keep their
  values; update any web-spa integration/e2e tests that assert current login DOM.
- Reconcile `#region Purpose/Design` in touched files; new focused-shell component gets a
  Design region explaining when to use it vs `TimeWarpPage`.
- `dev build` 0/0; `web-spa-integration-tests` suite green; Playwright e2e (if any cover
  login) green or updated.

## Checklist

- [ ] Focused-shell variant (empty layout pattern) for auth screens
- [ ] LoginPage rebuilt per locked design (no email, Learn-more disclosure, redirect-if-authed)
- [ ] Home: anonymous vs signed-in differentiation
- [ ] "Try it" card relocated behind Developer gating
- [ ] ChangePasswordPage deleted (after route/reference check)
- [ ] Tests updated (web-spa integration; e2e if present); `dev build` 0/0
- [ ] Design regions reconciled
- [ ] Results with How to validate (screenshots encouraged)

## Notes

- Reference screenshots reviewed with maintainer 2026-08-06: passkeys.io sign-in card (email
  path explicitly rejected) and its "What is a passkey?" explainer for the Learn-more copy tone.
- Related landed work: 156/157 (appbar logo), 162 (footer frame), 163 (home hero credits
  removed), 149 (avatar/menu), 150 (GetProfile identity fix — signed-in strip can trust
  ProfileState).

## Session

- Created: 2026-08-04 (empty placeholder).
- 2026-08-06 claude + Steve: spec'd — login design locked to adapted passkeys.io pattern;
  home-page defaults recorded for veto-in-review.
