# Server enforces the sign-in mode: refuse passkey sign-in and account creation outside Passkey mode

## Description

Task 379-016 added `SignInOptions.Mode` (`Passkey` default, `Microsoft365Only`, `Staff`). The mode
only changes what the browser shows. `SignInOptions` is bound by the web-spa (`configuration/sign-in-options.cs`)
from two appsettings files (web-server and web-spa `wwwroot`), and no server code reads it. In
`Microsoft365Only` and `Staff` the server still serves:

- passkey sign-in (`features/identity/start-passkey-authentication`, `complete-passkey-authentication`),
- passkey account creation (`start-passkey-registration`, `complete-passkey-registration`),
- the add-existing-passkey flow (`start-add-existing-passkey`, `complete-add-existing-passkey`).

A caller who skips the login card can still use them. The shell's required-passkey gate is also
skipped outside Passkey mode, which is correct only if the server agrees passkeys are not offered.

Goal: the server owns the mode and refuses what the mode does not offer. The browser reads the mode
from the server, so the two cannot drift (AGENTS.md: when two things must agree, generate one from
the other).

## Decision

- The server binds the mode (web-server options, validated on start). Default stays `Passkey`, so
  existing apps behave the same. (default chosen for this task)
- The SPA gets the mode from the server, alongside the existing Microsoft 365 offer read
  (`get-entra-sign-in-offered` / `SignInState.FetchMicrosoft365Offered`), instead of its own
  `wwwroot/appsettings.json` section. Remove the SPA-side section and its "both files bind equal"
  test. (default chosen for this task)
- Outside `Passkey`, the passkey sign-in and registration start endpoints return a problem
  response (403 with a stable problem type), before any WebAuthn ceremony state is created.
  Complete endpoints refuse too. (default chosen for this task)
- Decide in the spec review whether existing passkey credentials keep working in
  `Microsoft365Only` (recommended: no; the mode means Microsoft 365 sign-in only) and whether
  `Staff` differs from `Microsoft365Only` on the server at all (recommended: same server rule;
  Staff is copy only). Steve decides.
- Agent keys / agent tokens are out of scope: they are not browser sign-in.

## Checklist

- [ ] Server-side `SignInOptions` (or equivalent) bound and validated on the web-server; one
      checked-in section.
- [ ] Endpoint for the SPA to read the mode (or extend the existing offer contract); SPA state
      reads it; SPA `SignInOptions` binding removed.
- [ ] Passkey start/complete authentication, start/complete registration, and add-existing-passkey
      refuse outside Passkey mode with one shared check and one problem type.
- [ ] Tests: server tests per mode for each refused endpoint and that Passkey mode is unchanged;
      SPA render tests updated for the server-read mode.
- [ ] Gates: `./bin/dev build` 0/0, `./bin/dev template-smoke`, `ganda repo audit`,
      `git diff origin/master -- source tests | rg -i "crunchit|copic"` empty.

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-24)

## Notes

- Found while checking PR #569 (379-012..017 stack) before merge. Steve: merge, file this follow-up.
- Pre-existing on master, not from 379: `features/identity/entra-sign-in-mode-application.cs`
  Design region names a client ("... 008-004 replacements"). Remove that name in this task.
- Do not rename the existing `EntraSignInMode` (an evaluation-mode enum); pick a name that does not
  collide.
