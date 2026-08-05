# Show profile fields on Profile page

## Description

Replace the Profile SPA page stub (`Profile — coming soon.`) with a real read-only (or
edit-ready) view of the signed-in user's profile fields.

Today:

- `web-spa/pages/ProfilePage.razor` is a single card: "Profile — coming soon."
- Chrome avatar menu already calls `ProfileState.FetchProfileData()` and shows `Alias` + avatar.
- Domain aggregate `Profile` has: **DisplayName**, **Language**, **Region**, **Theme**,
  **Notifications** (+ `Version`) mapped under `profiles.profiles`.
- `GetProfile` contract/handler only returns **Alias** + **Avatar** and does **not** load from
  EF — authenticated path synthesizes alias and fetches multiavatar.com each request (TODOs in
  handler).

Goal: Profile page shows the profile information users care about (at least all domain fields
that exist), loaded through the existing BFF/profile state path — not a hard-coded stub.

## Requirements

1. **UI:** Replace `ProfilePage.razor` stub with a layout that displays profile fields (FluentUI /
   existing card patterns). Minimum fields to show when data is available:
   - Display name (or Alias if that remains the chrome-facing name)
   - Language, Region, Theme
   - Notifications (on/off)
   - Avatar (existing data URI / image)
2. **Data:** Page loads via TimeWarp.State (`ProfileState` / `FetchProfileData` or dedicated
   action). Do not call HttpClient from the page.
3. **Contract/handler:** Expand `GetProfile` Response (and handler) as needed so the page can
   show those fields. Prefer reading a real `Profile` from `PostgresDbContext` when postgres is
   on and a row exists; document dual-mode if anonymous/mock paths stay.
4. **Auth:** Profile page stays gated (`Policies.CanViewOwnProfile` / existing `[Page]` policy).
   Align endpoint auth with real persistence (today `[EndpointAllowAnonymous]` is deliberate for
   dual-mode demo — revisit when loading durable profiles for authenticated users only).
5. **No "coming soon"** left on this page for the happy path.
6. **Tests:** Co-located Jaribu or existing suite coverage for GetProfile happy path when
   persistence is wired; SPA smoke optional if already covered elsewhere.

## Non-goals

- Full profile edit/save UX (unless trivial; prefer read display first)
- Multi-tenant / multi-profile
- Replacing multiavatar strategy end-to-end (may keep temporary avatar until persisted)

## Context / anchors

| Path | Role |
|------|------|
| `web-spa/pages/ProfilePage.razor` (+ `.razor.cs`) | Stub UI |
| `web-spa/features/profiles/profile-state/*` | SPA state (Alias, Avatar today) |
| `web/features/profile/get-profile/*` | Contract + handler (TODO: DB) |
| `web/features/profile/profile-domain.cs` | Aggregate fields |
| `profiles.profiles` | EF table (migrations path, 147-007) |

## Checklist

- [x] Inventory ProfileState vs domain fields vs GetProfile Response; decide field names on the wire
- [x] Expand GetProfile contract/handler (+ tests) for display fields
- [x] Load Profile from EF when authenticated + postgres (create/seed policy if missing row — document)
- [x] Profile page UI shows all required fields; remove "coming soon"
- [x] Fetch on page enter (or reuse state); empty/loading/error states honest
- [x] Build 0/0; relevant tests green


## Notes

### Finalized plan (orchestration 2026-08-05)

**Locked decisions**
- **D1** Create-if-missing Profile on first authenticated GetProfile (`ProfileId = UserId`, defaults Member/en-US/US/system/false)
- **D2** Wire keeps `Alias` ← domain `DisplayName`; add Language, Region, Theme, Notifications, Avatar
- **D3** Keep `[EndpointAllowAnonymous]` dual-mode; page stays `CanViewOwnProfile`
- **D4** `IProfileStore` dual-mode (in-memory + EF swap in PostgresDbModule) — no DbContext in application
- **D5** `Profile.Create(ProfileId id, …)` for 1:1 user key
- **D6** Avatar stays non-EF; multiavatar + resilient fallback
- **D7–D8** ProfileState fields + page `OnInitializedAsync` + FluentUI read-only UI (no coming soon)
- **D9–D10** Mock factory + get-profile-tests + domain/store coverage

**Order:** Domain Create(id) → IProfileStore DI → expand contract → handler → ProfileState → ProfilePage → tests → dev build 0/0

**STOP if:** ProfileId≠UserId required; forced application→infrastructure DbContext ref; force EndpointAuthorize without mock strategy; create-if-missing rejected with no seed; avatar must be durable this task.

### Prior notes
- **Logged-in after DB wipe:** identity-session cookie lives in browser, not Postgres.
- Parent/context: dogfood after 147-007; Profile is teaching aggregate.

## Session

- Created: 2026-08-05 — user request after migrations cutover; Profile page still stub
- Planning: orchestration 2026-08-05 — plan locked D1–D10; no clarifying questions
- Implement: 2026-08-05 — D1–D10 landed (IProfileStore dual-mode, create-if-missing, contract/state/page, tests). `./bin/dev build` 0/0; get-profile-tests 10/10; web-domain-tests 28/28.
- Review: 2026-08-05 — effort 1 general, round 1, disposition clean (0 findings)

## Results

### What shipped
- Dual-mode `IProfileStore` (in-memory default; EF when Postgres connection present)
- `Profile.Create(ProfileId, …)` for 1:1 principal/user id
- `GetProfile` Response: Alias, Language, Region, Theme, Notifications, Avatar
- Handler: dual-mode anonymous mock vs authenticated store + create-if-missing + avatar fallback
- `ProfileState` expanded; `ProfilePage` shows all fields (no "coming soon"); loads via FetchProfileData
- Co-located `get-profile-tests.cs`; domain fixed-id Create tests

### Commits
- `dac592cf` dual-mode IProfileStore + Profile.Create fixed id
- `c520cb25` GetProfile store-backed fields + create-if-missing + tests
- `4116b7ff` Profile page UI + ProfileState
- `9ae36bae` checklist update

### Review
- Effort 1, roster: general; rounds: 1
- Counts: all open 0
- Disposition: **clean** (`review/disposition.md`)
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`

### How to validate

**Smoke (UI)**
1. `./bin/dev run` with postgres connected (migrations applied).
2. Sign in (passkey or mock principal).
3. Open Profile menu → Profile (or navigate `/Profile`).
4. **Expect:** Display name **Member**, Language **en-US**, Region **US**, Theme **system**, Notifications **Off**, avatar image (multiavatar or fallback SVG).
5. Refresh page: same values; `profiles.profiles` has a row with `Id` = principal/user Guid.

**Automated**
```bash
./bin/dev build   # 0/0
dotnet run source/container-apps/web/features/profile/get-profile/get-profile-tests.cs
cd tests/container-apps/web/web-domain-tests && dotnet test -c Release
```

**Depends on:** 147-007 migrations path for EF tables.  
**Not in scope:** edit/save profile, durable avatar storage, EndpointAuthorize-only GetProfile.
