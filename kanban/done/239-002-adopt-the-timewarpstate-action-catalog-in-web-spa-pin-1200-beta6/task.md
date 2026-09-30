# Adopt the TimeWarp.State action catalog in web-spa (pin 12.0.0-beta.6)

## Description

Child of 239. timewarp-state task 092 shipped the opt-in action catalog in **TimeWarp.State
12.0.0-beta.6** (on nuget.org 2026-09-30): `[CatalogAction(Description, Name, Permissions,
Visibility)]` on the nested `Action` class, a generated per-assembly registry,
reflection-free `ActionCatalogEntry.Execute(IStore, args, ct)`, `services.AddActionCatalog(assemblies)`,
`IActionCatalog`, analyzers TWS0004–0007 (docs: timewarp-state `documentation/topics/action-catalog.md`).

## Requirements

- Pin `TimeWarp.State` and `TimeWarp.State.Plus` (and any other TimeWarp.State.* pins) to
  `12.0.0-beta.6` in `Directory.Packages.props`. Read the beta.6 release notes; fix anything the new
  analyzers flag (no blanket suppressions).
- Register the catalog in web-spa (`AddActionCatalog(typeof(<web-spa marker>).Assembly)`, plus any
  other assembly that declares cataloged actions).
- Tag the user-facing actions with `[CatalogAction]`. Start from 238's stand-in roster
  (`kanban/done/238-evaluate-jev-for-ctrl-k-and-webmcp-action-catalog/research/catalog.json`): e.g.
  Theme.Update, Profile.SignOut, Credentials.AddPasskey / AddExistingPasskey,
  Role.CreateRole, SiteSettings.UpdateSiteSettings, Counter.IncrementCounter (demo). Use
  `PermissionIds` constants for `Permissions` (they are the SPA policy names). Exclude
  Fetch*/Clear*/Debug/inbound hub/template FiveSecondTask/TwoSecondTask/ThrowException. Actions
  whose parameters a palette cannot supply (ids, complex commands) get `Visibility.Agent` or are left
  out — decide per action and list the decisions in Results.
- Descriptions: one plain sentence each (TWS0007).
- Test (SPA integration): `IActionCatalog` enumerates the expected names; a parameterless entry
  (e.g. Theme or Counter) executes through the store and changes state; excluded actions are absent.
- Gates: `dev build` 0/0, `dev test`, `dev template-smoke` (generated apps restore beta.6).
- **Do not start an AppHost** (`dev run`, `aspire run`, `dotnet run` of aspire-app-host) — task worktrees share the master user-secrets id. Record the manual browser check as not performed.

## Checklist

- [x] Pins → 12.0.0-beta.6; new analyzer findings fixed
- [x] `AddActionCatalog` registered
- [x] User-facing actions tagged; per-action decisions listed
- [x] Tests
- [x] Gates; no AppHost

## Results

- `TimeWarp.State` + `TimeWarp.State.Plus` pinned to `12.0.0-beta.6` (the only TimeWarp.State.* pins).
  TWS0004–0007 raised nothing after tagging; no suppressions added.
- `services.AddActionCatalog(typeof(Web.Spa.IAssemblyMarker).Assembly)` in `web-spa/program.cs`;
  mirrored in `AspireSpaTestApplication` so tests see the production roster. Only web-spa declares
  cataloged actions (Plus declares none).
- Per-action decisions:

| Action | Visibility | Permissions | Why |
|--------|------------|-------------|-----|
| Counter.IncrementCounter | Both | DeveloperAccess | demo; `int amount` is palette/agent-suppliable; Counter page is DeveloperAccess |
| Profile.SignOut | Human | — | parameterless; browser-session action, not an agent tool |
| Credentials.AddPasskey | Human | CredentialManageSelf | WebAuthn ceremony needs a human gesture |
| Credentials.AddExistingPasskey | Human | CredentialManageSelf | WebAuthn ceremony + account merge |
| Credentials.RenameCredential | Agent | CredentialManageSelf | needs credential id + nickname |
| Credentials.RevokeCredential | Agent | CredentialManageSelf | needs credential id |
| Profile.UpdateProfile | Agent | ProfileWrite | complex profile fields |
| Role.CreateRole | Agent | AdminRolesManage | complex command |
| SiteSettings.UpdateSiteSettings | Agent | SettingsWrite | complex settings command |
| Theme.Update | not cataloged | — | declared in the TimeWarp.State.Plus package — cannot be tagged here; needs a `[CatalogAction]` upstream in timewarp-state (239-003 can surface theme toggle directly or follow up upstream) |
| Credentials.DismissPasskeySoftPrompt | not cataloged | — | banner-local UI dismissal, not a command |
| Fetch*/Clear*/Debug/inbound hub/FiveSecondTask/TwoSecondTask/ThrowException | not cataloged | — | excluded per brief |

- Tests: `tests/container-apps/web/web-spa-integration-tests/features/application/action-catalog-tests.cs`
  (SpaSessionFixture) — full roster, permissions/visibility, `Counter.IncrementCounter` executes via
  `ActionCatalogEntry.Execute` through the store (10 → 15), excluded names absent (11/11).
- Gates: `dev build` 0 warnings / 0 errors; `dev test` 21 suite runs, 0 failed; `dev template-smoke` SUCCEEDED
  (generated app restores beta.6; web-jaribu 212/212).
- No AppHost started. Manual browser check **not performed**.

- Review: 1 round, effort 1 (general). Final counts 0 bug / 0 suggestion / 0 nit (0 open, 0 fixed,
  0 wontfix). Disposition **clean**. Artifacts: `review/review-framework.md`,
  `review/round-1/merged.md`, `review/disposition.md`.

### How to validate

**Smoke:** `cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Release -- --filter-class ActionCatalog`

**Expect:** 11/11 pass — `IActionCatalog.Entries` names equal the 9-name roster above; `Counter.IncrementCounter`
with `["5"]` moves Count 10 → 15; `Theme.Update`, Fetch*/Clear*/template actions resolve `Find(...) == null`.
Then `dev build` reports 0 warnings / 0 errors.

## Notes

- Parent 239. Consumed by 239-003. Folds in the State beta.6 pin bump (Steve, 2026-09-30).
- Cockpit session: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED

## Session

- Created: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED (2026-09-30)
- 2026-09-30: implement oracle (ganda task work) — tagged actions, registered catalog, tests, gates green.
- 2026-09-30: review oracle (ganda task work) — effort 1 general review, disposition clean.
