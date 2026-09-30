# Page registry: generate an enumerable list of [Page] destinations

## Description

Child of 239 (Ctrl-K palette). The palette must index navigation destinations from one source,
not a second hand-copied route list. Today `PageSourceGenerator`
(`source/analyzers/timewarp-architecture-analyzers/generators/page-source-generator.cs`) emits
per-class members only (`[Route]`, `INavigableComponent`/`IStaticRoute`, `GetPageUrl`, `Policy`);
`Title`/`NavIcon` are hand-written static members required by `INavigableComponent`; NavMenu is
hand markup (`TimeWarpNavLink TPage=…`). Nothing aggregates pages.

## Requirements

- Extend the `[Page]` generator to emit one per-assembly registry (e.g. `PageRegistry.All`) of
  entries with: route template, `GetPageUrl` for static routes, `Title`, `NavIcon`, `Policy`,
  and a navigation/palette opt-in. Opt-in shape: a `[Page]` property (e.g.
  `Navigable = true` / `InPalette = true`) or a small separate attribute — decide and record in
  the generator Design region. Parameterized routes (no `IStaticRoute`) are excluded from the
  registry in v1 (they need arguments).
- Reflection-free (generated list referencing the static members); AOT/trim safe.
- Optional but preferred: NavMenu renders its product entries from the registry so the menu and
  the palette cannot drift. If NavMenu keeps hand markup (e.g. for category grouping), add a
  build-time check or test that every NavMenu `TimeWarpNavLink` target is in the registry.
- Generator tests (analyzer test project): registry contents, opt-in, parameterized-route exclusion,
  policy carried. SPA test: registry lists the expected demo pages.
- Update `tw-blazor-layout` / the analyzer Design region if the NavMenu rule changes.
- Gates: `dev build` 0/0 (generator change ⇒ full rebuild), `dev test`, `dev template-smoke`.
- **Do not start an AppHost** (`dev run`, `aspire run`, `dotnet run` of aspire-app-host) — task worktrees share the master user-secrets id. Record the manual browser check as not performed.

## Checklist

- [ ] Registry generated with title/icon/policy/url and opt-in
- [ ] Parameterized routes excluded
- [ ] NavMenu from registry, or a drift check
- [ ] Tests
- [ ] Gates; no AppHost

## Notes

- Parent 239. Consumed by 239-003.
- Cockpit session: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED

## Session

- Created: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED (2026-09-30)
