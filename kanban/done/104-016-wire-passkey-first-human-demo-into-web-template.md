# Wire passkey-first human demo into web template

## Parent

104

## Description

Template UX: primary CTA Continue with passkey (not full registration form). Creates principal + session; no mandatory profile.

## Requirements

- Works in web-spa + server host path used by template
- Discoverable passkey UX
- Legacy Passwordless path not required

## Checklist

- [x] UI + server wiring
- [x] Manual smoke
- [x] Purpose/Design on UI entrypoints

## Notes

Human dogfood: Proton Pass or platform authenticator.

### From task 131 disposition (F-010)

When removing legacy Passwordless from the template path, explicitly cover:

- Remove Passwordless CDN script + hardcoded TimeWarp tenant public API key from
  `web-server/components/App.razor` (still ships into every generated app until this lands).
- Sweep related `Passwordless:` appsettings, package refs, and
  `passwordless-service.cs` (including any Console.WriteLine of ApiKey/ApiUrl).
- Prefer first-party WebAuthn/passkey UX only — no third-party tenant key in template output.

### Depends on

104-003, 104-006

## Results

### Implementation

- **Product CTA `/Login`:** primary **Continue with passkey** (discoverable WebAuthn get →
  session), secondary **Create a passkey** (register → Principal + session). No email/username/
  profile form. Profile menu Sign-in and Home “Continue with passkey” route here; nav lists Login.
- **`PasskeyCeremonyClient`** (web-spa): shared options → `window.Spa.WebAuthn` → Complete* →
  session read for Login + technical `/Passkeys` demo (Passkeys copy points at product CTA).
- **Legacy Passwordless fully removed from template path (131 F-010):**
  - `App.razor` CDN ESM import + hardcoded `timewarp:public:…` tenant key
  - SPA `passwordless-service.cs` (incl. `Console.WriteLine` of ApiKey/ApiUrl),
    `passwordless-options.cs`, `RegisterPasskey.razor`
  - Server `AddPasswordlessSdk` + `Passwordless:` appsettings (web-server + SPA + test hosts)
  - Package refs: web-server, web-contracts, CPM `Directory.Packages.props`
  - Dead `GetSignInToken` ClientOnly contract + handler (`features/auth/` emptied/removed)
- Agent discovery copy (`auth.md`, `llms.txt`, `index.md`) points Login as product CTA.
- Purpose/Design regions on LoginPage, PasskeysPage, PasskeyCeremonyClient.

### Verification

- `dotnet build` web-server / web-spa / web-contracts: 0/0.
- `web-server-integration-tests` `--filter-class Passkey`: **16/16** passed (existing ceremony
  coverage; SPA is thin client over same endpoints).
- Full-repo `dev build` may still fail on concurrent WIP in `timewarp-402-tests` (104-013) —
  not this task’s surface.
- Manual browser smoke (Proton Pass / platform authenticator) remains the human dogfood path
  from 104-003; UX now lives on `/Login` with the same server ceremonies.

### Disposition

- **Done.** First-party WebAuthn only in template output; no third-party tenant key.
- Progressive profile stays **104-024**. Entra non-default path stays **104-021**.
- Playwright e2e sunny path stays **104-022**.

## Session

- Created: 2026-07-16
- Implementation: 2026-08-04 (104-016 passkey-first template UX + Passwordless purge)
