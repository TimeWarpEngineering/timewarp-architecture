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

- [ ] Pins → 12.0.0-beta.6; new analyzer findings fixed
- [ ] `AddActionCatalog` registered
- [ ] User-facing actions tagged; per-action decisions listed
- [ ] Tests
- [ ] Gates; no AppHost

## Notes

- Parent 239. Consumed by 239-003. Folds in the State beta.6 pin bump (Steve, 2026-09-30).
- Cockpit session: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED

## Session

- Created: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED (2026-09-30)
