# Round 1 — general
**Date:** 2026-08-05
**Scope reviewed:** commits dac592cf..4116b7ff Profile feature

## Summary

Implementation matches locked plan D1–D10. Dual-mode `IProfileStore` keeps the application layer free of `PostgresDbContext`; authenticated `GetProfile` create-if-missing uses `ProfileId = UserId` with documented defaults; race handling re-finds after duplicate `Add`; contract/state/page wire Alias ← DisplayName plus Language/Region/Theme/Notifications/Avatar; avatar stays non-EF with multiavatar + deterministic fallback; SPA loads via `ProfileState.FetchProfileData` (no page `HttpClient`); page gate remains `CanViewOwnProfile` with endpoint dual-mode anonymous; Design regions and co-located Jaribu + domain tests cover the happy paths claimed by the task.

### Plan checklist (verified in source)

| Decision | Status | Evidence |
|----------|--------|----------|
| **D1** create-if-missing `ProfileId=UserId`, defaults Member/en-US/US/system/false | OK | `get-profile-handler-application.cs` Find → Create(id,…) → Add; re-find on `InvalidOperationException` |
| **D2** Alias ← DisplayName; Language/Region/Theme/Notifications/Avatar | OK | Response ctor + handler mapping; mock factory expanded |
| **D3** `[EndpointAllowAnonymous]` dual-mode; page `CanViewOwnProfile` | OK | contracts reason string; `ProfilePage.razor.cs` `[Page]`/`[Authorize]` |
| **D4** `IProfileStore` dual-mode; no DbContext in application | OK | port + InMemory in application; `EfProfileStore` in infrastructure; `InMemoryProfileStoresModule` + `PostgresDbModule` Replace; handler ctor takes only `IProfileStore` |
| **D5** `Profile.Create(ProfileId id, …)` | OK | domain factory + empty-id reject; domain tests |
| **D6** avatar non-EF; multiavatar + resilient fallback | OK | `GetAvatarDataUriAsync` + `BuildFallbackAvatarDataUri`; network failures do not fail GetProfile |
| **D7–D8** ProfileState fields + page load + FluentUI read-only UI | OK | state props + `FetchProfileData` map; `OnInitializedAsync` + interactive guard; no “coming soon” |
| **D9** mock factory | OK | `GetMockResponseFactory` returns full Response shape |
| **D10** tests | OK | co-located get-profile-tests (contract, store, handler create/existing/fallback/anonymous); domain fixed-id Create tests |

### Layering & DI

- Handler depends on `ICurrentUserService`, `IProfileStore`, `HttpClient`, `ILogger` only — no EF.
- Zero-infra: `InMemoryProfileStoresModule` → singleton `InMemoryProfileStore` (always registered from `web-server/program.cs`).
- Postgres connected: `PostgresDbModule` `RemoveAll<IProfileStore>()` + scoped `EfProfileStore` (same gate as principal stores).
- Skip-mode (postgres flag on, no connection string): module returns early; in-memory remains.

### Create-if-missing race

- In-memory: `TryAdd` failure → `InvalidOperationException`.
- EF: pre-exists check + unique violation (`23505` / message heuristic) → same exception type; entity Detached on unique fail.
- Handler catches that, re-finds; throws only if re-find is still null (hard failure, correct fail-closed).

### Contract / page load

- Response fields guarded non-empty (except bool Notifications).
- Page: `Alias is null` → “Loading…”; else avatar + PropertyDisplay rows; Notifications On/Off.
- Fetch path is `ProfileState.FetchProfileData` only; matches chrome/`AuthenticationStateListener`.
- Endpoint auth dual-mode and page policy align with D3.

### Tests

- Contract round-trip, empty-alias deserialize rejection, mock defaults.
- InMemory Find/Add/duplicate.
- Handler: anonymous mock; authenticated create defaults + store assert; existing seeded fields; avatar failure → data-URI fallback.
- Domain: `Create(fixedId)` and empty-id throw.

## Issues

None.
