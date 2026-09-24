# Passkey username: TimeWarp account · <account fingerprint> instead of the fixed placeholder

## Description

Every passkey created on the site carries the WebAuthn `user.name` / `user.displayName`
"TimeWarp user" (hard-coded `PlaceholderUserName` in
`source/container-apps/web/features/identity/start-passkey-registration/start-passkey-registration-handler-application.cs:28`,
passed to `WebAuthnRegistration.BuildOptionsJson`). The authenticator stores that value at
creation and it can never be edited afterwards (Proton Pass lets users edit its Title, not the
Username). Result: every account shows up in every password manager as the same "TimeWarp user",
so two accounts on one device are indistinguishable at sign-in.

Decision (Steve, 2026-09-24): username = `TimeWarp account · <account fingerprint>`, where the
**account** fingerprint is one value per principal (the same on every passkey of that account).
It is NOT the per-credential `Fingerprint` from 248-001 (that is derived from the credential id,
which the authenticator only produces during the ceremony, and it differs per passkey).
No new required signup field.

## Requirements

- **Account fingerprint.** Define it once in `timewarp-identity` (e.g. `PrincipalFingerprint.Compute(PrincipalId)`
  = last 8 hex of SHA-256 of the principal id bytes, same shape as `CredentialFingerprint`).
  Display-safe, never the raw id. Expose it on the current-session / profile response the SPA
  already reads (pick the one Settings uses for "Signed in"; record which).
- **Signed-in add passkey.** The add-passkey start path (the signed-in registration flow used by
  `AddPasskey` / Settings "Create a passkey") sends `user.name` = `user.displayName` =
  `TimeWarp account · <account fingerprint>` for the caller's principal.
- **New account.** The principal is currently minted at completion
  (`complete-passkey-registration-handler-application.cs:102`, `Principal.Create(PrincipalKind.Human)`).
  Pre-allocate the principal id at **start**: generate it in `StartPasskeyRegistration`, keep it with
  the issued challenge in the WebAuthn challenge store (`i-webauthn-challenge-store.cs` /
  `in-memory-challenge-store-core.cs` — extend the stored ceremony state, do not add a second
  store), send the username derived from it, and have Complete create the principal with that id
  (add a `Principal.Create` overload taking an id, keep the invariants). A challenge that is never
  completed allocates nothing persistent. Keep the existing account-resolution rule (credential
  handle, not name) unchanged; the pre-allocated id must not be trusted from the client — it lives
  only server-side with the challenge.
- Replace `PlaceholderUserName` everywhere; reconcile the Design regions (start + complete
  registration handlers, challenge store) — the "never persisted / fixed placeholder" text changes.
- **Settings.** Show the account fingerprint next to the signed-in identity so it matches the
  password-manager entry (e.g. "Signed in · TimeWarp account · 7f3a9c21").
- **Existing passkeys** keep "TimeWarp user" (immutable at the authenticator). Note it in Results.
- **Tests.** Start options carry the derived name for new and signed-in flows; the principal
  created at Complete has the pre-allocated id (so its fingerprint matches the name sent);
  two new accounts get different names; a second passkey on one account gets the same name as the
  first; an expired/unused challenge leaves no principal; tampering (client-supplied id) is
  impossible because the id is not on the wire. Contract round-trip for the new field.
- Gates: `dev build` 0/0, `dev test`, `dev template-smoke`.
- **Do not start an AppHost** (`dev run`, `aspire run`, `dotnet run` of aspire-app-host) — task
  worktrees share the master user-secrets id. Record the manual Proton Pass check as not performed.

## Checklist

- [ ] `PrincipalFingerprint` + exposure on the session/profile response
- [ ] Signed-in add passkey sends the account name
- [ ] New account: principal id pre-allocated at start, kept with the challenge, used at complete
- [ ] `PlaceholderUserName` removed; Design regions reconciled
- [ ] Settings shows the account fingerprint
- [ ] Tests
- [ ] `dev build` 0/0 · `dev test` · `dev template-smoke`; no AppHost started

## Notes

- Origin: Proton Pass save dialog screenshot, 2026-09-24.
- Related: 248-001 (per-credential fingerprint), 250 (Entra account hint).
- Cockpit session: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED

## Session

- Created: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED (2026-09-24)
