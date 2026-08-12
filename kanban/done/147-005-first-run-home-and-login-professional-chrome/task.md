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
- **No Learn-more link** (maintainer 2026-08-06: not needed on our site — the passkeys.io
  explainer/#How-to-use-a-passkey was reference material for us, not a feature to copy).
- **Cross-device QR option MUST be available** (maintainer requirement): the browser-native
  hybrid dialog ("use a phone or tablet" → QR code) is what passkeys.io shows — it is browser
  UI, not site UI, and a site can only BREAK it. Verify the ceremony options do not suppress
  it: creation must NOT pin `authenticatorSelection.authenticatorAttachment` to `"platform"`,
  sign-in must not restrict transports in `allowCredentials` (discoverable-credential flow with
  empty allowCredentials preferred). Inspect `passkey-ceremony-client.cs` + the server ceremony
  options; fix if restricted. Acceptance includes SEEING the QR/phone option in a real browser
  dialog for both create and sign-in.
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

- [x] Focused-shell variant (empty layout pattern) for auth screens
- [x] LoginPage rebuilt per locked design (no email, no learn-more, redirect-if-authed)
- [x] Cross-device QR: code path verified hybrid-safe (no ceremony fix); **manual browser QR still maintainer smoke**
- [x] Home: anonymous vs signed-in differentiation
- [x] "Try it" card relocated behind Developer gating (TestPage)
- [x] ChangePasswordPage deleted (after route/reference check)
- [x] Tests: `web-spa-integration-tests` green; `dev build` 0/0; no Playwright login suite
- [x] Design regions reconciled
- [x] Phase 4b review disposition clean (`review/`)
- [x] Results with How to validate

## Notes

- Reference screenshots reviewed with maintainer 2026-08-06: passkeys.io sign-in card (email
  path explicitly rejected) and its "What is a passkey?" explainer for the Learn-more copy tone.
- Related landed work: 156/157 (appbar logo), 162 (footer frame), 163 (home hero credits
  removed), 149 (avatar/menu), 150 (GetProfile identity fix — signed-in strip can trust
  ProfileState).

### Implementation plan (Phase 2, 2026-08-06)

**Architecture**

- Keep single empty `MainLayout` (`@Body` only). Do **not** fork layouts.
- New sibling shell: `components/TimeWarpFocusedPage.razor` (+ `.razor.css`) —
  `BaseComponent`, cascade, full-viewport calm bg, horizontal logo above centered
  content column. No nav/search/footer. Login (and optionally Logout) wraps this;
  product pages keep `TimeWarpPage`.

**Login**

- Rebuild card: "Sign in" → primary **Sign in with a passkey** (`data-qa=ContinueWithPasskey`)
  → subtle divider → "Don't have an account?" + **Create account** (`data-qa=CreatePasskey`).
- Drop session debug line; keep authed redirect (already present) + MessageBar errors.
- Ceremony: **no product code fix** — registration omits `authenticatorAttachment`; auth uses
  empty `allowCredentials`. Manual browser hybrid/QR still required for acceptance.

**Home**

- Welcome + Built-with stay.
- `AuthorizeView`: NotAuthorized = passkey CTA → `LoginPage.GetPageUrl()`;
  Authorized = strip with `ProfileState` avatar/alias + Settings + nested
  `AuthorizeView Policy=CanViewAdminSidebarNavSection` → Admin (`RolesListPage`).
- Remove Try-it from Home.

**Try-it relocate**

- Target: `features/debugger/pages/TestPage.razor` (`/Debugger/Test`, already Developer-gated).
- Restore `ApplicationState.TwoSecondTask` public wrapper (currently commented; needed to compile).

**Delete**

- `ChangePasswordPage.razor` + `.cs` — only refs are the page files themselves (safe).

**Tests**

- Re-run `login-return-url-tests` (no API change expected).
- Optional: identity unit test locking hybrid-safe options JSON.
- No Playwright login suite in-tree.

**Sequence:** focused shell → Login → Home → TestPage + TwoSecondTask → delete ChangePassword →
optional Logout focused + options test → `dev build` + spa integration + manual.

**Open questions:** none — implement as locked.

## Results

First-run surfaces now read as product chrome: focused passkey login (no shell nav/footer),
Home differentiated for anonymous vs signed-in, demo Try-it off Home behind Developer gate,
password-era ChangePassword page removed.

### What was implemented

- **`TimeWarpFocusedPage`** — auth-adjacent shell (logo + centered column); Login + Logout wrap it.
- **`LoginPage`** — locked card: Sign in with a passkey / Create account; `data-qa` preserved; no session debug.
- **`HomePage`** — AuthorizeView strip (avatar/alias/Settings/Admin policy); CTA via button + `ChangeRoute` + `CrossSliceReference(LoginPage)`.
- **`TestPage`** — Try-it demos relocated; TwoSecondTask via ActionSet generator (no hand-written wrapper — CS0111).
- **Deleted** ChangePasswordPage (no product/test refs).

### Files changed (primary)

- `components/TimeWarpFocusedPage.razor` (+ `.razor.css`) — created
- `features/account/pages/login-page/LoginPage.razor` (+ `.cs`)
- `features/account/pages/LogoutPage.razor` (+ `.cs`)
- `features/application/pages/HomePage.razor` (+ `.cs`)
- `features/debugger/pages/TestPage.razor` (+ `.cs`)
- `application-state.two-second-task.cs` — Design only
- `ChangePasswordPage.*` — deleted

### Key decisions / deviations

- TwoSecondTask: generator owns public wrapper; plan's "uncomment" would CS0111.
- Ceremony options: **no code change** (already hybrid-safe).
- Review M1: Home CTA fixed to Profile-style navigation (not NavLink>button).

### Review (Phase 4b)

- Effort 1, roster: general; 2 rounds
- Final: 0 open (1 suggestion fixed); disposition **clean**
- Paths: `review/review-framework.md`, `review/round-2/merged.md`, `review/disposition.md`

### Test outcomes

- `./bin/dev build` — 0/0
- `web-spa-integration-tests` — 15 passed, 1 skipped (pre-existing weather quarantine), 0 failed
- Playwright login suite — none in-tree

### How to validate

**Automated**

```bash
./bin/dev build
# expect: 0 Warning(s), 0 Error(s)

cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Release
# expect: all non-quarantined tests passed
```

**Smoke (UI)**

```bash
./bin/dev run
```

1. Signed out `/` → Welcome + Built with + Sign in CTA; **no** Try it card.
2. Click CTA → `/Login` focused chrome (logo + card, **no** nav/search/footer).
3. Buttons: "Sign in with a passkey" (`data-qa=ContinueWithPasskey`), "Create account" (`data-qa=CreatePasskey`); no email/password; no session debug line.
4. Signed in `/` → avatar/alias strip + Settings; Admin only if Administrator.
5. Visit `/Login` while signed in → redirect home (or safe returnUrl).
6. Developer role: `/Debugger/Test` → Try it buttons work (footer spinner / modal).
7. `/changePassword` → no page (not found).

**Ceremony hybrid (manual — not automated this session)**

1. Real browser, non-mock passkey path: Create account + Sign in.
2. Expect browser hybrid dialog can offer phone/tablet/QR (platform-dependent UI).
3. Code path: registration omits `authenticatorAttachment`; auth empty `allowCredentials`.

**Not in scope:** Live screenshots attached here; Playwright login e2e (no suite yet).

## Session

- Created: 2026-08-04 (empty placeholder).
- 2026-08-06 claude + Steve: spec'd — login design locked to adapted passkeys.io pattern;
  home-page defaults recorded for veto-in-review.
- 2026-08-06 refinement (Steve): drop the Learn-more link; REQUIRE the cross-device QR
  option (browser hybrid dialog) to be available in both ceremonies.
- 2026-08-06 grok orchestrate: Phase 1–2 plan (019fd53d…); Phase 4 implement (019fd541…);
  Phase 4b review (019fd546…); M1 fix + disposition clean; Results + done.
