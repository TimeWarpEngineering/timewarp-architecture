# Remaining SPA pages load via TrackAction not null state

## Description

Task 187 fixed Roles/Principals only. Same mistake remains on WeatherForecasts,
Settings, Profile, Superhero. Superhero also lacks `[TrackAction]` and its
`Superheros == null` check is dead (list is never null).

## Requirements

- WeatherForecastsPage: `IsAnyActive(FetchWeatherForecasts)`
- SettingsPage: `IsAnyActive(FetchCredentials)` — not Credentials is null
- ProfilePage: `IsAnyActive(FetchProfileData)` — not Alias is null
- Superhero: add `[TrackAction]`; page uses `IsAnyActive(FetchSuperhero)`
- Reconcile Design regions that still say null = loading UI

## Checklist

- [x] Weather
- [x] Settings
- [x] Profile
- [x] Superhero
- [x] Design regions
- [x] web-spa build
- [x] Commit

## Results

All SPA pages that showed Loading from a null field now use
`IsAnyActive(typeof(Fetch*ActionSet.Action))`. Superhero fetch gained
`[TrackAction]` (the null check was dead). AssemblyInfoModal still says
Loading while reading local assembly metadata — not a backend Action.

### How to validate

```bash
rg -n "Loading" --glob '*.razor' \
  source/container-apps/web/projects/web-spa
# Expect: IsLoading branches on Roles/Principals/Weather/Settings/Profile/Superhero;
# AssemblyInfoModal is local metadata, not TrackAction.

rg -n "WeatherForecasts == null|Superheros == null|Alias is null|Credentials is null" \
  source/container-apps/web/projects/web-spa --glob '*.{razor,cs}'
# Expect: Settings OnAfterRender fetch-once guard only; no Loading predicates

dotnet build source/container-apps/web/projects/web-spa/web-spa.csproj -c Debug
# Expect: 0/0
```

## Session

- Implementation: grok 2026-08-13
