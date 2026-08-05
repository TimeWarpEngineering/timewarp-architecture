# Use TimeWarp.Multiavatar for avatars and fix header avatar size and profile menu position

## Description

The GetProfile handler currently HTTP-fetches `https://api.multiavatar.com/{userId}.svg` per
request and base64s it into a data URI, with a hand-rolled fallback SVG on network failure
(`source/container-apps/web/features/profile/get-profile/get-profile-handler-application.cs`).
Replace that with the first-party **TimeWarp.Multiavatar** NuGet package
(nuget.org, latest `1.0.0-beta.13`; repo TimeWarpEngineering/timewarp-multiavatar), which
generates the same avatars locally and deterministically:

```csharp
using TimeWarp.Multiavatar;
string svg = MultiavatarGenerator.Generate(seed);
```

Also fix two chrome issues visible in the app header:

- The header avatar (`Profile.razor`) uses `AvatarSize.Size48` inside the 56px-tall appbar —
  it dominates the header. Use `Size32` (or 28). The Profile page card avatar may stay 48.
- The profile FluentMenu popup is smashed against the right viewport edge — fix menu
  anchoring/alignment and/or trailing spacing on `.twe-appbar__actions`
  (`TimeWarpPage.razor` appbar styles).

## Requirements

- No runtime network dependency on api.multiavatar.com anywhere (server or SPA).
- Avatar generation seeded by the user id (stable per user across requests).
- The contract mock response (`GetProfile.GetMockResponseFactory()` in
  `get-profile-contracts.cs`) must return a real generated avatar from a fixed seed instead of
  the current grey `<rect fill="#888"/>` placeholder, so demo/anonymous mode is not a grey box.
- Header avatar visually fits the 56px appbar; menu popup no longer clipped/flush against the
  right edge.
- Follow `tw-blazor-css-strategy` for any CSS changes; `tw-web-api-contracts` if the contract
  mock changes shape (it should not — only the mock avatar value changes).

## Checklist

- [ ] Add `TimeWarp.Multiavatar` to `Directory.Packages.props` (CPM) and reference it from the
      web application layer project
- [ ] Replace `GetAvatarDataUriAsync` HTTP fetch with local `MultiavatarGenerator.Generate`,
      seeded by user id; remove the `HttpClient` ctor dependency, the resilience catch, the
      `BuildFallbackAvatarDataUri` helper, and the `LogMultiavatarFailed` logger message
      (generation is local — no failure mode to fall back from)
- [ ] Update the mock factory avatar in `get-profile-contracts.cs` to a generated avatar
      (fixed seed) — keep it a precomputed data-URI constant if the contracts project should
      not reference the package; document the choice in the Design region
- [ ] Update `get-profile-tests.cs` expectations (fallback tests likely deleted; add a
      determinism assertion: same user id → same avatar data URI)
- [ ] Header: `FluentAvatar` size to `Size32` (or 28) in
      `web-spa/features/profiles/components/Profile.razor`
- [ ] Menu: fix FluentMenu popup alignment so it is not flush against the right viewport edge
- [ ] Reconcile `#region Purpose` / `#region Design` in every touched file (D6 wording about
      multiavatar network resilience is obsolete after this change)
- [ ] `dev build` 0/0
- [ ] Visual check via Aspire run: header avatar sized correctly, menu popup positioned with
      breathing room, avatar renders a real multiavatar image when signed in

## Notes

- Screenshots from 2026-08-05 session: header avatar oversized grey circle; profile menu
  popup flush against right edge.
- The grey avatar itself is the contract mock (`alias: "alias"`, grey rect) — the server
  handled GetProfile as anonymous. Why the SPA shows authorized while the server sees no
  principal is task 150, not this task.
- Related: task 148 (profile feature) introduced the current handler; its D6 resilience
  decision (never fail on multiavatar network errors) becomes moot with local generation.

## Session

- Created: Claude (2026-08-05)
- Planning: orchestration 2026-08-05 — plan locked D1–D10

## Finalized plan (orchestration 2026-08-05)

**Locked:** CPM TimeWarp.Multiavatar 1.0.0-beta.13 on web-application only; seed userId.ToString("D"); local Generate→data URI; remove HttpClient/fallback/logger; mock = precomputed data URI (seed GetProfile.Mock) no package on contracts; header Size32; menu CSS padding-inline-end on .twe-appbar__actions (Fluent v5 has no HorizontalAlignment); Profile page Size56 unchanged; tests determinism, drop network fallback test.

**Order:** CPM+ref → handler → mock constant → tests → Size32 → appbar CSS → Design regions → dev build 0/0.

