# Sign-in methods: the server owns the list of enabled methods and refuses the rest

## Description

Task 379-016 added `SignInOptions.Mode` (`Passkey` default, `Microsoft365Only`, `Staff`). That shape
does not scale: Google and X sign-in are coming, and a single mode would need a value for every
combination. The mode also only changes what the browser shows. The web-spa binds `SignInOptions`
(`configuration/sign-in-options.cs`) from two appsettings files (web-server and web-spa `wwwroot`),
and no server code reads it. With passkey hidden, the server still serves:

- passkey sign-in (`features/identity/start-passkey-authentication`, `complete-passkey-authentication`),
- passkey account creation (`start-passkey-registration`, `complete-passkey-registration`),
- the add-existing-passkey flow (`start-add-existing-passkey`, `complete-add-existing-passkey`).

`Staff` is not a sign-in method. It is Microsoft-365-only plus a hard-coded "Staff only." line,
which is app wording and does not belong in the template.

Goal: one enum names the sign-in methods, configuration lists the enabled ones, the server owns
that list and refuses any method not in it, and the browser renders one button per enabled method
from the server's answer. Adding Google or X later is one enum value, its settings, its button, and
its endpoints behind the same check.

## Shape

```csharp
public enum SignInMethod
{
  Passkey,
  Microsoft365
}
```

```json
"SignInOptions": {
  "Methods": [ "Passkey", "Microsoft365" ]
}
```

- `Methods` is a collection of `SignInMethod`, not strings. Configuration binding turns the names
  into enum values, so an unknown name fails on start.
- The template default is `[ "Passkey", "Microsoft365" ]`, which matches today's default behaviour:
  passkey, plus Microsoft 365 when it is offered.
- Microsoft 365 only is `[ "Microsoft365" ]`.
- The browser shows buttons in a fixed order defined in code (the enum order), not the config order.

## Decisions

- Replace `SignInMode` with `SignInMethod` and a `Methods` collection. Remove `SignInMode`,
  `Microsoft365Only` and `Staff`.
- The web-server binds and validates `SignInOptions` on start (`ValidateOnStart`), from one
  checked-in section in the web-server appsettings. The validator rejects an empty list and
  duplicates.
- **The list is the only configuration switch for Microsoft 365** (Steve, 2026-10-25). Listing
  `Microsoft365` in `SignInOptions.Methods` registers the Entra OIDC scheme. Not listing it means
  no scheme. Remove `EntraAuthenticationOptions.Enabled`, the `Authentication:Entra:Enabled` key,
  and the obsolete `UseEntra` synonym, with all their handling:
  `MockAuthenticationDefaults.IsEntraEnabled` / `EntraEnabledKey` / `UseEntraKey` /
  `UsedObsoleteUseEntraKey`, `EntraObsoleteKeyPostConfigure`, and the `entraEnabled` flag in
  web-server `program.cs` `ConfigureAuthentication`. The admin's runtime toggle in site settings
  (`EntraSignInEnabled`) stays.
- Microsoft 365 is offered when it is listed and the site-settings toggle is on.
  `EntraSignInOffer.IsOfferedAsync` reads the list in place of `options.Enabled`. Keep its
  store-read-always rule (the read re-seeds an emptied table).
- Listing `Microsoft365` without the Entra settings (tenant, client id and the rest the existing
  validator checks) fails validation on start. Not listing it skips the Entra settings
  validation, as `Enabled = false` does today.
- Callers of the removed key move to the list. Find them with
  `rg -n 'Entra:Enabled|Entra__Enabled|EntraEnabledKey|UseEntra' source tests tools skills`.
  Known ones:
  - the Aspire AppHost `program.cs`;
  - web-server `appsettings.json` / `appsettings.Development.json`;
  - `web-spa.csproj` and `wwwroot/auth.md`;
  - `identity-session-defaults-server.cs`, `site-settings-seeder-application.cs`, and
    `timewarp-identity/persistence/i-site-settings-store.cs` (doc comments);
  - the dev CLI `entra-setup.cs` and `entra-disable-command.cs`. Those write `SignInOptions:Methods`
    into user secrets: setup adds `Microsoft365`, and disable removes it, keeping `Passkey`.
  - tests: `entra-scheme-registration-tests.cs`, `entra-oidc-handler-round-trip-tests.cs`,
    `entra-public-origin-tests.cs`, `settings-page-microsoft-365-tests.cs`, `entra-setup-tests.cs`,
    and `get-site-settings-tests.cs`, which constructs `Enabled = true`.
  - `GetSiteSettings`' Configuration* fields: report whether Microsoft365 is listed, in place of
    `Enabled`.
- The SPA gets the enabled, currently offered methods from the server. Replace or extend
  `get-entra-sign-in-offered` / `SignInState.FetchMicrosoft365Offered` with one read that returns
  the offered `SignInMethod` list. Pick whichever is less code, and update `GetCredentials`'
  `CanLinkMicrosoft365` to keep using the same rule. Remove the SPA's own `SignInOptions`
  binding, its `wwwroot/appsettings.json` section, and the test that both files bind equal.
- When `Passkey` is not listed, every passkey endpoint above returns 403 with one stable problem
  type, through one shared check (not a copy per endpoint). The start endpoints refuse before any
  WebAuthn ceremony state is created.
- Existing passkey credentials stop working while `Passkey` is not listed. The rows stay, and
  listing `Passkey` again makes them work.
- The shell's required-passkey gate runs only when `Passkey` is listed.
- Agent keys and agent tokens are out of scope. They are not browser sign-in.

## Remove Staff and the mode

- `configuration/sign-in-mode.cs`: replace with `SignInMethod`. The file name follows the type.
- `configuration/sign-in-options.cs`: `Methods` replaces `Mode`; drop `StaffNote`. It moves to the
  web-server side, or a shared project both sides can read. Follow `tw-feature-placement`.
- `SignInChoices.razor`, `Microsoft365AccountChoice.razor`, `LoginPage.razor.cs`,
  `ChooseMicrosoft365Page.razor.cs`: render from the method list. Drop the staff line and every
  `Mode` / `Staff` branch.
- `StyleGuidePage.razor`: show the sign-in choices for one method and for both. Drop the Staff
  example and "Staff only.".
- Tests: `sign-in-choices-render-tests.cs`, `microsoft-365-account-choice-render-tests.cs`,
  `configuration/sign-in-options-tests.cs`, `style-guide-page-contract-tests.cs`. Replace the mode
  cases with method-list cases and drop the Staff assertions.
- Keep the sample role named "Staff" in the role card, TimeWarpPage and style guide role tests.
  That is example data, not sign-in.
- Update the Purpose and Design regions that mention the mode or Staff.
- After the change, `rg -n 'SignInMode|StaffNote|Staff only|Microsoft365Only|Entra:Enabled|Entra__Enabled|EntraEnabledKey|UseEntra' source tests tools skills`
  is empty.

## Checklist

- [ ] `SignInMethod` enum and `SignInOptions.Methods`; `SignInMode`, `Microsoft365Only`, `Staff` removed
- [ ] Web-server binds and validates `SignInOptions` on start (non-empty, no duplicates, Microsoft365 needs the Entra settings)
- [ ] `EntraAuthenticationOptions.Enabled`, `Authentication:Entra:Enabled` and `UseEntra` removed; listing Microsoft365 registers the Entra scheme; every caller (AppHost, appsettings, dev CLI entra setup/disable, tests) moved to the list
- [ ] One server read returns the offered methods; the SPA renders buttons from it; SPA binding and `wwwroot` section removed
- [ ] Passkey endpoints return 403 with one problem type when `Passkey` is not listed, through one shared check
- [ ] Tests: each refused passkey endpoint (including an existing passkey) when Passkey is not listed;
      both-listed unchanged; Microsoft365 listed without Entra fails validation; SPA render tests per method list
- [ ] Remove the client name from the Design region of
      `features/identity/entra-sign-in-mode-application.cs` (the "... 008-004 replacements" sentence).
      It predates 379.
- [ ] Gates: `./bin/dev build` 0/0, `./bin/dev template-smoke`, `ganda repo audit`,
      `git diff origin/master -- source tests | rg -i "crunchit|copic"` empty

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-24)

## Notes

- Found while checking PR #569 (the 379-012..017 stack) before merge. Steve: merge, file this follow-up.
- Steve 2026-10-24: Staff "sounds like UX to me not something we put in a template". Removed.
- Steve 2026-10-25: Login with X and Google are coming, so the shape is a collection of
  `SignInMethod` enum values, not a single mode and not strings.
- Do not rename the existing `EntraSignInMode` in `entra-sign-in-mode-application.cs`. It is an
  evaluation result (Challenge / SyncHit / BootstrapCreate / Link), not a sign-in method.
