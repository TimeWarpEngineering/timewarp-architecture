# Round 2 — general
**Date:** 2026-10-25
**Scope reviewed:** fix delta `1745d1b84..689d4f2af` (commits `7bb4c3c0a`, `689d4f2af`), checked against
round-1 M1–M6 and the call sites of the changed members

## Summary

The fix pass closes all six round-1 findings. Publish now passes the `entra-enabled` parameter
through as `SignInOptions__IncludeMicrosoft365` and writes no `SignInOptions__Methods__N` index.
The web-server appends `Microsoft365` from that key in both `ApplyConfiguredMethods` and
`ConfigurationLists`, so scheme registration, Entra validation, and the gate all see the same list.
`dev entra setup` appends at the next free index of the merged list. Disable removes only
user-secret `Microsoft365` entries. Settings hides the passkey buttons unless `PasskeyOffered` is
true. `CompleteEntraBootstrapExisting` is in the gate, with a 403 case. The stale copy is reworded.
The login card shows a failure line and a retry. I found no new defects in the delta.

Re-verified in this worktree: `./bin/dev build` 0 warnings, 0 errors. Suites:
`PasskeySignInMethodGate` 1/1, `EntraSchemeRegistration` 4/4, `SignInChoices_Should` 6/6,
`SignInOptions_` 5/5, `SettingsPagePasskeyButtons_Should_` 2/2, `SignInActions` 16/16,
`Microsoft365AccountChoice` 2/2, dev-cli `Entra` 54/54, `Microsoft365MethodList_Given_` 5/5,
`ComposePublish_Given_` 11/11, `KubernetesPublish_Given_` 12/12, `AcaPublish_Given_` 14/14,
`AcaNeonPublish_Given_` 7/7. `ganda repo audit` exit 0 (only the `kanban-landed` warning about
other tasks' claims). `rg 'Entra Enabled|Entra:Enabled|Entra__Enabled|Enabled/AllowBootstrap'`
over `source tests tools skills` is empty. Across the delta the seeder runfile keeps mode `100755`.
I did not re-run `template-smoke`; the implementer reports it passed after the fix.

## Prior findings re-verified

- **M1 — fixed.** `aspire-app-host/program.cs:435` is
  `.WithEnvironment("SignInOptions__IncludeMicrosoft365", entraEnabledParameter)`. That is a
  parameter reference, not a value resolved at publish time. No index is forced. Compose
  (`${ENTRA_ENABLED}` plus an `.env` key), Helm (`.Values`), ACA, and ACA Neon (unquoted parameter)
  tests pin that the value is a parameter and that `Methods__0`/`__1` are absent. `tw-deploy`
  SKILL.md and the 070-003 comment describe the new key.
- **M2 — fixed.** `SettingsPage.razor:118` wraps both buttons in `SignInState.PasskeyOffered is true`.
  The page fetches the offered list in `OnInitializedAsync`. The new render test covers both
  states.
- **M3 — fixed.** `CompleteEntraBootstrapExisting.Command` is in `PasskeyCommands`. The gate test
  asserts its 403. The Design region explains why.
- **M4 — fixed.** `Microsoft365MethodKeyToAdd` returns null when the merged checked-in + secrets list
  already has Microsoft365. Otherwise it returns the index after both. Passkey is no longer
  written. Disable removes every secret whose value is `Microsoft365` and writes nothing else.
  Status lists the secret method keys it finds.
- **M5 — fixed.** The banner names `SignInOptions:Methods (Microsoft365)` and
  `Authentication:Entra:AllowBootstrap`. The three Design comments are reworded.
- **M6 — fixed.** `OfferedMethodsFailed` is set only by a completed failed read and reset by
  `Reset`. `SignInChoices` shows "Sign-in options could not be loaded." plus Try again only when
  the read failed and no list is loaded. Not-yet-loaded still paints nothing.

## Issues

None.

## Notes (not findings)

- `EntraSetup.PasskeyMethodKey` / `Microsoft365MethodKey` (`tools/dev-cli/services/entra-setup.cs:40`)
  no longer have production callers. Only tests still use them, and one test stores
  `Microsoft365` under `PasskeyMethodKey`. They are harmless, so I am noting them rather than
  opening a finding. A later touch could inline them as `SignInMethodsPrefix + "0"`.
- Disable leaves a checked-in `Microsoft365` entry in place. It says so, and the message is
  limited to user secrets. That matches the documented scope.
