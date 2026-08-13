# Admin list loading via TrackAction not null collection

## Description

Roles and Principals list pages treat `State.X is null` as Loading. Fetch already has
`[TrackAction]`. COPIC lists use
`ActionTrackingState.IsAnyActive(typeof(XxxState.FetchXxxActionSet.Action))` as the
loading signal. Null collection is "no snapshot", not "in flight".

## Requirements

- RolesListPage Loading = `IsAnyActive(FetchRoles Action)`
- PrincipalsPage Loading = `IsAnyActive(FetchPrincipals Action)`
- Empty principals message stays for loaded-zero, not for in-flight
- Reconcile RoleState / PrincipalState Design (null ≠ loading UI)

## Checklist

- [x] RolesListPage
- [x] PrincipalsPage
- [x] Design regions
- [x] web-spa build
- [x] Commit

## Results

Admin Roles and Principals show Loading while the fetch Action is tracked
(`IsAnyActive(typeof(Fetch*ActionSet.Action))`). Empty principals is
not-in-flight and no rows. Null collection is no snapshot, not loading.

Left as-is (same smell): WeatherForecastsPage, SettingsPage, ProfilePage.

### How to validate

```bash
rg -n "Roles is null|Principals is null" \
  source/container-apps/web/projects/web-spa/features/admin/roles/pages/RolesListPage.razor \
  source/container-apps/web/projects/web-spa/features/admin/principals/pages/PrincipalsPage.razor
# Expect: Principals empty-state only (is null || Count == 0), not the Loading branch

rg -n "IsAnyActive" \
  source/container-apps/web/projects/web-spa/features/admin/roles/pages/RolesListPage.razor.cs \
  source/container-apps/web/projects/web-spa/features/admin/principals/pages/PrincipalsPage.razor.cs
# Expect: FetchRoles / FetchPrincipals Action types

dotnet build source/container-apps/web/projects/web-spa/web-spa.csproj -c Debug
# Expect: 0/0
```

Browser: `/Admin/Roles` and `/Admin/Principals` — Loading while fetch runs; table or
empty message after. Save refetch shows Loading again (Fetch* is tracked).

## Session

- Implementation: grok 2026-08-13
