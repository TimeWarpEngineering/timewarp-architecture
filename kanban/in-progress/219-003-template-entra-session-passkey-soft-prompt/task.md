# Template: Entra-session passkey soft-prompt

## Parent

219

## Description

Fold-in of architecture RFC 219 **D8** (098-006 shape). After an Entra-issued identity-session, if `GetCredentials` has no `Passkey`, show a dismissible “Add a passkey” prompt that drives existing `AddPasskey`. Never a gate.

## Requirements

- Detect Entra-only (or Entra-without-passkey) via `GetCredentials` Type list — not a new TrustTier, not quarantine
- Banner/CTA on post-login home or profile; dismissible for the session (and optionally persisted as “later”)
- Uses existing StartPasskeyRegistration + AddPasskey
- Template only; crunchit may reuse the same pattern in its auth child
- Tests: Entra-only principal sees prompt; principal with passkey does not; dismissing does not block routes

## Checklist

- [x] Soft-prompt UI wired to existing add-passkey ceremony
- [x] Not a route/session gate
- [x] Tests for shown/hidden/dismissed

## Notes

RFC: `rfc/rfc.md` §4 D8, §7.1. Fork 1: passkey is a prompt afterward, never a gate.

## Depends on

- 219-002

## Results

Template SPA shows a dismissible “Add a passkey” banner after an Entra-issued identity-session when `GetCredentials` has an active `EntraAccount` and no active `Passkey`. The CTA dispatches existing `CredentialsState.AddPasskey` (`StartPasskeyRegistration` + browser WebAuthn + authenticated `AddPasskey`). Dismissing is UX only: Home stays `Policies.Anonymous`; Profile/Settings stay their permission policies. No TrustTier, no quarantine, no route/session gate.

**Files**

- `source/container-apps/web/projects/web-spa/features/identity/passkey-soft-prompt.cs` — Type-list predicate
- `source/container-apps/web/projects/web-spa/features/identity/credentials-state/credentials-state.cs` — `ShouldShowPasskeySoftPrompt` / dismiss flag
- `source/container-apps/web/projects/web-spa/features/identity/credentials-state/credentials-state.dismiss-passkey-soft-prompt.cs`
- `source/container-apps/web/projects/web-spa/features/identity/components/AddPasskeyPrompt.razor` (+ `.razor.css`)
- `source/container-apps/web/projects/web-spa/components/TimeWarpPage.razor` — banner above page content (Home and Profile share the shell; Login uses `TimeWarpFocusedPage` and does not)
- `source/container-apps/web/projects/web-spa/features/identity/AuthenticationStateListener.razor` — fetch credentials on sign-in; clear later-key on sign-out
- `tests/container-apps/web/web-spa-integration-tests/features/identity/passkey-soft-prompt-tests.cs`

**Decisions**

- Detect via `GetCredentials` `Type` + `IsActive` only (`PasskeySoftPrompt.ShouldShow`). AgentKey does not count as a passkey. Null snapshot hides the banner (no flash before fetch).
- Compose in `TimeWarpPage` (Outside → product is free under TWA0009) so post-login Home and Profile both see it without duplicating chrome.
- “Later” persists in `sessionStorage` (`twe-passkey-soft-prompt-later`) so a refresh of the same tab does not nag; logout removes the key so the next principal on the same tab is not suppressed.
- CTA is `CredentialsState.AddPasskey` — no new ceremony.

**Tests:** 8/8 passed (`PasskeySoftPrompt_` unit facts). `dotnet run tools/dev-cli/dev.cs -- build` 0/0.

### How to validate

**Automated**

```bash
cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Release -- --filter-class PasskeySoftPrompt
# expect: 8 passed (Entra-only shows; passkey hides; dismissed hides; Home/Profile/Settings policies are not passkey gates)
```

**Smoke**

```bash
dotnet run tools/dev-cli/dev.cs -- build
# expect: Build succeeded, 0 Warning(s), 0 Error(s)
```

With Entra enabled (`Authentication:Entra:Enabled=true`) and a trusted tenant, sign in via Login → Continue with Microsoft 365, land on `/` (or `/Profile`):

- Entra-only principal: banner `data-qa="AddPasskeySoftPrompt"` with “Add a passkey” / “Later”
- Principal that already has a passkey: banner absent
- Click Later: banner gone; `/`, `/Profile`, `/Settings` still load (Home remains anonymous; no passkey policy)
- Click Add a passkey: existing WebAuthn create + `AddPasskey` attach (same path as Settings “Create a passkey”)

**Not in scope:** live Entra tenant or hardware WebAuthn in CI; crunchit auth-child reuse.

## Session

- Created: 3425584 (2026-09-14)
- Implementer: grok session 01a0a351-717d-7011-ba51-a459f249ea9a (2026-09-15)
