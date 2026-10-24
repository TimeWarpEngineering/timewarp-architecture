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

## Goal (implementer restatement)

The server owns which browser sign-in methods are enabled. `SignInOptions.Methods` lists
`Passkey` and `Microsoft365`. Listing `Microsoft365` registers Entra and requires its settings.
Leaving it off does not. The browser draws one button per method the server currently offers.
Passkey ceremonies, including the Entra "use an existing passkey" completion, answer 403 when
`Passkey` is not listed. The shell's required-passkey gate runs only when that read says Passkey
is offered.

Round 2 closes the review findings on that behavior:

- Publish keeps `entra-enabled` as a deploy-time parameter. It appends `Microsoft365` to the
  checked-in list and does not write a literal `SignInOptions__Methods__0=Passkey`. Compose,
  Kubernetes, and Azure Container Apps publish tests pin that.
- Settings hides Create and Add existing passkey unless `PasskeyOffered` is true, with a render case.
- `dev entra setup` appends `Microsoft365` only when the merged list lacks it. Disable removes
  those user-secret entries and does not write `Passkey`.
- The admin drift banner and the three stale Design comments name `SignInOptions:Methods` /
  Microsoft365.
- A failed offered-methods read shows "Sign-in options could not be loaded." and a retry. A read
  that has not finished yet still paints nothing.

Acceptance: those six findings are implemented, `./bin/dev build` exits 0, the targeted suites
below pass, `./bin/dev template-smoke` passes, and `ganda repo audit` exits 0.

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

- [x] `SignInMethod` enum and `SignInOptions.Methods`; `SignInMode`, `Microsoft365Only`, `Staff` removed
- [x] Web-server binds and validates `SignInOptions` on start (non-empty, no duplicates, Microsoft365 needs the Entra settings)
- [x] `EntraAuthenticationOptions.Enabled`, `Authentication:Entra:Enabled` and `UseEntra` removed; listing Microsoft365 registers the Entra scheme; every caller (AppHost, appsettings, dev CLI entra setup/disable, tests) moved to the list
- [x] One server read returns the offered methods; the SPA renders buttons from it; SPA binding and `wwwroot` section removed
- [x] Passkey endpoints return 403 with one problem type when `Passkey` is not listed, through one shared check
- [x] Tests: each refused passkey endpoint (including an existing passkey) when Passkey is not listed;
      both-listed unchanged; Microsoft365 listed without Entra fails validation; SPA render tests per method list
- [x] Remove the client name from the Design region of
      `features/identity/entra-sign-in-mode-application.cs` (the "... 008-004 replacements" sentence).
      It predates 379.
- [x] Gates: `./bin/dev build` 0/0, `./bin/dev template-smoke` succeeded, `ganda repo audit` exit 0.
      The crunchit diff is not empty; see Results.
- [x] Publish keeps `entra-enabled` as `SignInOptions__IncludeMicrosoft365` and does not write a literal method index
- [x] Settings hides Create and Add existing passkey unless Passkey is offered
- [x] `CompleteEntraBootstrapExisting` is in the passkey gate
- [x] `dev entra setup` appends Microsoft365 and does not force `Methods:0=Passkey`; disable removes those keys
- [x] Admin drift banner names `SignInOptions:Methods` / Microsoft365
- [x] A failed offered-methods read shows a message and a retry

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-24)
- Implementation: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-25)
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session not reported, max-turns 500, Done — 2026-10-24T20:05:17Z
- Review oracle round 1: general reviewer (claude, model claude-opus-5-5), effort 3, session not reported — 2026-10-25. 6 open (1 bug, 4 suggestion, 1 nit); fix pass requested.
- Review oracle: review by reviewer-claude (claude, model claude-opus-5-5), session not reported, max-turns 200 — 2026-10-24T20:12:34Z
- Fix pass for review round 1: implement-fix-2 (grok, model grok-4.7), session not reported — 2026-10-25. M1–M6 implemented on this branch. Review round 2 has not run. The first fix commit dropped the executable bit on `site-settings-seeder-tests.cs`; restored `100755` so the shebang runfile stays executable.

## Results

`SignInMethod` (`Passkey`, `Microsoft365`) and `SignInOptions.Methods` replace `SignInMode`.
The web-server binds that list with `ValidateOnStart`. `ApplyConfiguredMethods` replaces the
collection from the raw `SignInOptions:Methods` section, because the configuration binder
appends onto a collection that already has an initializer. A missing section keeps both
methods. The checked-in web-server section is `[ "Passkey" ]`, so a host with empty Entra
settings still boots. Listing `Microsoft365` registers the Entra scheme and requires the
existing Entra field checks. `Authentication:Entra:Enabled`, `UseEntra`, and
`EntraAuthenticationOptions.Enabled` are gone. `EntraSignInEnabled` still decides whether a
listed Microsoft 365 method is offered, and the site-settings read still always runs.

`GET api/identity/entra/offered` returns the offered method list. The SPA draws one button
per entry in enum order and no longer binds `SignInOptions`. When `Passkey` is absent,
`PasskeySignInMethodGateBehavior` returns 403 with
`https://timewarp.software/problems/sign-in-method-disabled` for the six ceremony routes
before WebAuthn state is created. Credential rows stay. The shell required-passkey gate
runs only when `Passkey` is offered. The sample role named Staff stays.
`EntraSignInMode` stays; it is the Entra evaluation result.

`ganda repo audit` exited 0. `kanban-landed` warned that eight other live claims have no
usable worktree (070-001, 104-024, 104-025, 104-026, 123, 141, 198, 244). Those claims are
not this task.

The acceptance search for `SignInMode|StaffNote|Staff only|Microsoft365Only|Entra:Enabled|Entra__Enabled|EntraEnabledKey|UseEntra` still matches `EntraSignInMode` in
`entra-sign-in-mode-application.cs`, `i-entra-sign-in-policy-application.cs`,
`entra-ticket-processor-application.cs`, `site-settings-entra-sign-in-policy-application.cs`,
`challenge-entra-endpoint-server.cs`, and `entra-sign-in-policy-tests.cs`. The Notes say to
leave that name. `StaffNote`, `Staff only`, `Microsoft365Only`, and the Entra enabled keys
are gone from `source`, `tests`, `tools`, and `skills`.

`git diff origin/master -- source tests | rg -i "crunchit|copic"` matches two places: the
deleted Design sentence in `entra-sign-in-mode-application.cs` (the checklist asked to
remove it), and the existing `dev entra setup --tenant crunchitfs.com` example in
`wwwroot/auth.md`, which stays because a nearby Entra-key edit pulled that line into the hunk.
No `copic` hit.

The sign-in pages were not opened in a browser. Render tests cover the button markup.

### Fix pass (review round 1)

Publish writes `SignInOptions__IncludeMicrosoft365` from the `entra-enabled` parameter
(`${ENTRA_ENABLED}` on Compose, a Helm `.Values` reference, an unquoted ACA parameter). It does
not write `SignInOptions__Methods__0` or `__1`, so a checked-in Microsoft 365 list stays at
index 0 until the parameter is true, and then Microsoft365 is appended. `dev entra setup`
does the same append against the merged checked-in list and user secrets, and does not write
Passkey. Disable removes secret keys whose value is `Microsoft365` and leaves a checked-in
entry in place. Settings shows Create and Add existing passkey only when `PasskeyOffered` is
true. `CompleteEntraBootstrapExisting` is in the passkey gate. The admin drift banner names
`SignInOptions:Methods` and `Authentication:Entra:AllowBootstrap`. A finished offered-methods
read that failed sets `OfferedMethodsFailed` and the login card shows "Sign-in options could
not be loaded." plus Try again. A read that has not finished still paints nothing.
`PasskeyOffered` stays null on that failure, so the required-passkey shell gate stays off.

`./bin/dev build` 0/0 in 00:00:05.98. `./bin/dev template-smoke` succeeded (generated web Jaribu 405/405, api Jaribu 9/9, timewarp-testing 14/14). Targeted suites: SignInChoices 6, SignInOptions 5,
SettingsPagePasskeyButtons 2, SignInActions 16, Microsoft365MethodList 5, PasskeySignInMethodGate 1,
Compose publish 11, Kubernetes publish 12, ACA publish 14, ACA Neon publish 7. No browser pass.
The content edit of `site-settings-seeder-tests.cs` dropped its executable bit (`100755` to `100644`). That shebang runfile is executable again.

### How to validate

**Smoke**

```bash
./bin/dev build
dotnet tests/container-apps/web/web-server-integration-tests/bin/Release/net11.0/web-server-integration-tests.dll --filter-class PasskeySignInMethodGate
dotnet tests/container-apps/web/web-spa-integration-tests/bin/Release/net11.0/web-spa-integration-tests.dll --filter-class SignInChoices_Should
dotnet tests/container-apps/web/web-spa-integration-tests/bin/Release/net11.0/web-spa-integration-tests.dll --filter-class SignInOptions_
dotnet tests/container-apps/web/web-spa-integration-tests/bin/Release/net11.0/web-spa-integration-tests.dll --filter-class SettingsPagePasskeyButtons_Should_
dotnet tests/container-apps/aspire/aspire-tests/bin/Release/net11.0/aspire-tests.dll --filter-class ComposePublish_Given_
dotnet tests/tools/dev-cli-tests/bin/Release/net11.0/dev-cli-tests.dll --filter-class Microsoft365MethodList_Given_
```

With the web-server running and `SignInOptions:Methods` set to `[ "Microsoft365" ]` (user secrets or a temporary appsettings edit you revert):

```bash
curl -ski -X POST https://localhost:63611/api/identity/passkey/authenticate/options -H 'content-type: application/json' -d '{}'
curl -ski https://localhost:63611/api/identity/entra/offered
```

`63611` is the web-server launch profile. Through the Aspire ingress the same paths are on `https://localhost:63610`.

**Expect**

- `./bin/dev build` ends with `0 Warning(s)` and `0 Error(s)`.
- `PasskeySignInMethodGate` passes. The ceremony routes, including `CompleteEntraBootstrapExisting`, answer 403 and the body contains `https://timewarp.software/problems/sign-in-method-disabled`. The body does not contain `optionsJson`. `GetCredentials` still reports the existing passkey as active. Restoring `Passkey` to the list makes `POST api/identity/passkey/authenticate/options` return 200 again.
- `SignInChoices_Should` passes: a null list paints no buttons; a completed failure paints "Sign-in options could not be loaded." and Try again; an empty list paints the work-account lead and the Microsoft 365 unavailable copy; Passkey is the primary button and stays ahead of Microsoft 365.
- `SignInOptions_` passes: the checked-in section becomes `[Passkey]` after `ApplyConfiguredMethods`; `IncludeMicrosoft365=true` appends Microsoft365 beside Passkey and `false` does not replace a Microsoft365 index 0; an unknown name fails validation.
- `SettingsPagePasskeyButtons_Should_` passes: offered Passkey shows Create and Add existing; offered Microsoft365 hides both and still shows the Passkeys section.
- `ComposePublish_Given_` passes: web-server `SignInOptions__IncludeMicrosoft365` is `${ENTRA_ENABLED}` and `SignInOptions__Methods__0` is absent. Kubernetes and ACA publish suites pin the same parameter and the same absence.
- `Microsoft365MethodList_Given_` passes: empty secrets beside checked-in Passkey append at index 1; a secret `Methods:0=Microsoft365` is not rewritten to Passkey.
- The authenticate-options curl returns HTTP 403 and `sign-in-method-disabled`. The offered curl returns a JSON `methods` array that contains `Microsoft365` when the site toggle is on and does not contain `Passkey`.

**Automated gate**

```bash
./bin/dev build
./bin/dev template-smoke
ganda repo audit
dotnet tests/container-apps/web/web-server-integration-tests/bin/Release/net11.0/web-server-integration-tests.dll --filter-class PasskeySignInMethodGate
dotnet tests/container-apps/web/web-server-integration-tests/bin/Release/net11.0/web-server-integration-tests.dll --filter-class EntraSchemeRegistration
dotnet tests/container-apps/web/web-spa-integration-tests/bin/Release/net11.0/web-spa-integration-tests.dll --filter-class SignInChoices_Should
dotnet tests/container-apps/web/web-spa-integration-tests/bin/Release/net11.0/web-spa-integration-tests.dll --filter-class Microsoft365AccountChoice
dotnet tests/container-apps/web/web-spa-integration-tests/bin/Release/net11.0/web-spa-integration-tests.dll --filter-class SignInOptions_
dotnet tests/tools/dev-cli-tests/bin/Release/net11.0/dev-cli-tests.dll --filter-class Entra
```

This session: `./bin/dev build` 0/0 in 00:00:23. `./bin/dev template-smoke` succeeded (generated web Jaribu 405/405, api Jaribu 9/9, timewarp-testing 14/14). `ganda repo audit` exit 0 with the advisory above. Targeted suites earlier in the same implementation: passkey gate 1, Entra scheme registration 4, Entra public origin 2, Entra OIDC round trip 1, Entra options 12, Entra challenge 18, credential availability 16, site-settings seed 3, protected page (script exit 0), GetEntraSignInOffered 5, site-settings seeder 8, GetSiteSettings 5, UpdateSiteSettings 30, last-used 4, seed-on-read 11, Entra sign-in policy 10, dev CLI Entra 49, Aspire publish Compose 10 / Kubernetes 11 / ACA 13 / ACA Neon 7, SPA sign-in choices 5, sign-in options 4, Microsoft 365 account choice 2, sign-in state 16, style guide 2, settings page 1.

**Not in scope:** a browser pass of the login page, a hardware WebAuthn authenticator, and agent keys or agent tokens.

### Review

Round 1 (general reviewer, effort 3) found 1 bug, 4 suggestions, and 1 nit. The fix pass
implemented M1–M6. Review round 2 has not run, so there is no disposition yet. Paths:
`review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`.

## Learnings

- No misread: the `## Goal (implementer restatement)` matches the task. The task itself conflicts:
  Shape says the template default is `[ "Passkey", "Microsoft365" ]`, while Decisions make listing
  Microsoft365 without Entra settings fail start, and the checked-in `ClientId` is empty. The
  implementer checked in `[ "Passkey" ]` (the old effective default) and disclosed it in Results.
  The review accepts this. why: review oracle round 1, 2026-10-25
- The acceptance `rg` only catches `Entra:Enabled`. Copy that writes `Authentication:Entra Enabled`
  survived (M5). Search the prose form too when a key is retired. why: review oracle round 1, 2026-10-25

## Review

- Round 1: findings M1–M6 implemented in the fix pass. Review round 2 has not closed them.

## Notes

- Found while checking PR #569 (the 379-012..017 stack) before merge. Steve: merge, file this follow-up.
- Steve 2026-10-24: Staff "sounds like UX to me not something we put in a template". Removed.
- Steve 2026-10-25: Login with X and Google are coming, so the shape is a collection of
  `SignInMethod` enum values, not a single mode and not strings.
- Do not rename the existing `EntraSignInMode` in `entra-sign-in-mode-application.cs`. It is an
  evaluation result (Challenge / SyncHit / BootstrapCreate / Link), not a sign-in method.
