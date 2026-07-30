# Migrate weather-forecast Jaribu host to SetupOnce CleanUpOnce on beta.14

## Description

Downstream of Jaribu task 029 / GitHub #19 (`SetupOnce`/`CleanUpOnce` shipped in
`TimeWarp.Jaribu` 1.0.0-beta.14). Replace the `Lazy<ApiTestServerApplication>` SharedHost
workaround in the co-located weather-forecast integration runfile with class-scoped hooks and
deterministic host dispose. Bump the repo CPM pin so co-located/template consumers resolve
beta.14+.

## Checklist

- [x] Bump `Directory.Packages.props` `TimeWarp.Jaribu` → `1.0.0-beta.14`
- [x] Migrate `get-weather-forecasts-tests.cs` to `SetupOnce` / `CleanUpOnce` + `DisposeAsync`
- [x] Update `#region Design` (remove Lazy-never-dispose / issue-19-open narrative)
- [x] Note real-host pattern on the skill exemplar line (`tw-feature-placement`)
- [x] Confirm no other `Lazy<ApiTestServerApplication>` SharedHost copies remain
- [x] Run standalone weather-forecast Jaribu tests and confirm pass + dispose
- [x] Commit and mark task done

## Notes

### Upstream

- Jaribu #19 / task 029: class-scoped fixture hooks published in **1.0.0-beta.14**.
- Hooks: `public static Task SetupOnce()` / `public static Task CleanUpOnce()`; lazy before first
  executing test; `CleanUpOnce` only if `SetupOnce` ran; author disposes explicitly.

### Scope

- Only the real-host weather-forecast exemplar used the Lazy SharedHost workaround.
- `create-role-tests.cs` is host-free (no change).
- Historical notes under `kanban/done/134-*` left as-is (historical record).

## Session

- Implementation: 2026-07-31 (TWA follow-through after Jaribu beta.14)

## Results

- CPM pin: `TimeWarp.Jaribu` **1.0.0-beta.14**
- `get-weather-forecasts-tests.cs`: `SetupOnce` creates `ApiTestServerApplication`; `CleanUpOnce`
  awaits `DisposeAsync` (no more `Lazy` SharedHost)
- Standalone run: **2/2 passed**; console shows `TestApplication.DisposeAsync` / host shutdown
  after both tests
- Skill exemplar line notes SetupOnce/CleanUpOnce + beta.14 floor