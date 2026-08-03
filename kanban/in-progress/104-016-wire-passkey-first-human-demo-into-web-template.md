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

- [ ] UI + server wiring
- [ ] Manual smoke
- [ ] Purpose/Design on UI entrypoints

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

## Session

- Created: 2026-07-16
