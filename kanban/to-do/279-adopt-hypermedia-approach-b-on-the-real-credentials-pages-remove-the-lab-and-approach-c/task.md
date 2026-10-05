# Adopt hypermedia approach B on the real Credentials pages; remove the lab and approach C

## Description

Steve decided on 2026-10-05, after reviewing task 275's evaluation (PR #430, merged), to **adopt
approach B**: server responses name catalog actions with arguments. The client runs them through
the action catalog and shows them as buttons and as Ctrl-K contextual rows. **C is rejected.** It
bypassed the domain actions and left `CredentialsState` stale, which conflicts with TimeWarp.State's
client-owned state and subscriptions, and its only allow-list was "same origin", so a response
could aim the user's token at any route in the app.

This task makes B real on the Credentials surfaces and removes the evaluation scaffolding. Read
task 275's "B vs C comparison" (in `kanban/done/275-hypermedia/task.md`) first.

### What 275 left on master

- **Keep:**
  - Identity's `CredentialRules` (`credential-rules-application.cs`: `CanRevoke`,
    `HoldsMicrosoft365`, `CanLinkMicrosoft365`), shared by `RevokeCredential.Handler` and
    `EntraTicketProcessor`;
  - the Ctrl-K context hook (`ICommandPaletteContextSource`, `CommandPaletteContext`, the
    contextual row kind, `RequiresInput`, the `Open` append);
  - the JSON → args binder (`ContextualActionArguments`).
- **Lab, to be removed:**
  - server: `features/hypermedia-lab/` (`get-credential-offers`, `get-credential-commands`,
    `lab-credential-snapshot-application.cs`);
  - SPA: `web-spa/features/hypermedia-lab/` (state, page, context source, rows,
    `app-relative-href.cs`, `followed-link-request.cs`);
  - their tests, and the NavMenu entry.
- **Client-side copies of server rules, to be removed:**
  - `CredentialsState.CanUnlink` / `ActiveCredentialCount` / `CanLinkMicrosoft365`
    (`credentials-state.cs`, `credentials-state.link-microsoft-365.cs`);
  - their use in `SettingsPage.razor(.cs)`, `PasskeysPage.razor` and `CredentialList.razor`.

## Requirements

1. **Server offers on the real contract.** The credentials read the Settings and Passkeys pages
   already use, `GetCredentials` or a sibling decided here and recorded, returns the **offered
   actions** per credential and page-level (Revoke, Rename, Link Microsoft 365, Add passkey where
   applicable) as `{ name, label, args }`.
   - The offers are computed from `CredentialRules` on the server.
   - Names come from `OfferedActionNames` constants, and a test pins that every emitted name
     resolves in the SPA `IActionCatalog`.
   - Contract house rules apply: serialization round-trips, mock factory, and an update to the
     existing round-trip tests.
2. **Client renders and runs offers through B.**
   - Buttons on Settings and Passkeys come from the offers in `CredentialsState`, not from
     client-computed `CanUnlink` / `CanLinkMicrosoft365`.
   - Running an offer goes through the catalog (`Find` → bind args → `Execute`), using the real
     `Credentials.*` actions with their notifications and `TrackAction`.
   - The follow-up refresh comes from the caller or runner, never a handler dispatching (TWS0002).
   - Delete the client-side rule copies and their tests. Update the Design regions that described
     them, including the "mirrors RevokeCredential.Handler" note.
3. **Ctrl-K:** the Settings and Passkeys pages contribute contextual rows through the existing
   context hook. Rows that need input, such as Rename's nickname, stay page-only, as in 275.
4. **Close 275's review M4.** Before `Execute`, check that a contextual or offered action's
   catalog entry has Visibility Human or Both and that its Permissions pass `IAuthorizationService`,
   **or** check it against an explicit offerable-names list. Choose one and record why. Unknown
   names, failed binding and failed checks are refused with a notification.
5. **Remove the lab:** delete the server and SPA lab slices, approach C (`FollowCommand`,
   `FollowedLinkRequest`, `AppRelativeHref`, the `Ignored` response type, and any C-specific
   generator workaround), the lab tests and the NavMenu or registry entry. No `/HypermediaLab`
   route remains.
6. **Optional, record either way:** consider a generator or analyzer that ties server offer
   argument names to the target action's constructor parameters at compile time; 275 noted that
   gap. File it as a follow-up if it isn't done here.
7. **Skill:** a short section in the owning SPA skill (`tw-blazor` or wherever state and actions
   are documented) describing the pattern: "server offers catalog actions; the client runs them
   through the catalog; never compute validity client-side when the server can offer it". Skills
   are public: write the rule and reasoning only.

## Checklist

- [ ] Real credentials contract carries offers from `CredentialRules`; name-resolution test
- [ ] Settings and Passkeys render and run offers through the catalog; rule copies deleted
- [ ] Ctrl-K contextual rows on Settings and Passkeys
- [ ] M4: Visibility/Permissions (or offerable-names) check before Execute; fail closed
- [ ] Lab + approach C removed (server, SPA, tests, nav); no `/HypermediaLab`
- [ ] Compile-time arg-name agreement: done or follow-up filed
- [ ] Skill section; Purpose/Design regions reconciled
- [ ] Tests:
      - offers match the server rules (one credential → no Revoke; two → Revoke; Microsoft 365
        offered only when allowed);
      - running an offer runs the real action;
      - a non-offered action can't be run;
      - the M4 check refuses hidden or unpermitted entries
- [ ] Gates: `dev build` 0/0, `dev test`, `dev template-smoke`, `ganda repo audit`
- [ ] Do **not** start an AppHost; record the browser check as not performed
- [ ] Implementation review; host `open-pr`

## Notes

- Not pursued: server-driven state, either Blazor Server rendering with TimeWarp.State on the
  server, or "C plus store slices" from a server-side store. It was discussed on 2026-10-05 as a
  possible future timewarp-state design question. It is not part of this task.
- Known 275 coverage gap: Microsoft 365 linking is only tested in the not-offered case, because
  the test server has Entra off. Close it if the test host can enable Entra; otherwise record it.
- Memory discipline: run builds serially; `dotnet build-server shutdown` before finishing.

## Session

- Created: 2026-10-05 (cockpit, per Steve's B decision on 275)

## Results

*(fill when done)*

### How to validate

*(required before done)*
