# Propagate FluentUI v5 + plain-CSS changes to the TimeWarp.Architecture template

Parent: 059. Last — after 001–006 land in the live `source/` app.

## Context

There are two copies of this codebase:
- **`source/container-apps/web/web-spa`** — the live/dev app (FluentUI `4.14.2`). Primary
  target of 059.
- **`TimeWarp.Architecture/`** — the packaged `dotnet new` template (FluentUI `4.13.1`,
  separate `Directory.Packages.props`).

The template is what users scaffold from, so it must end up clean too — but it carries
template plumbing (`#if(api/grpc/web/...)` directives, `dotnet new` template config) that the
live app doesn't.

## Tasks

- [ ] Decide cadence with Steven: does the template move in lockstep, or lag behind the live
      app until the v5 migration is proven? (Default: template follows once `source/` is
      verified.)
- [ ] Port the v5 package bump in `TimeWarp.Architecture/Directory.Packages.props`.
- [ ] Port the provider/`FluentProviders` + `AddFluentUIComponents` wiring (059-003).
- [ ] Port the per-component v5 API changes (059-004), respecting the template's `#if`
      feature-flag directives (`api`, `grpc`, `web`, `yarp`, `cosmosdb`).
- [ ] Port `tokens.css` + the plain-CSS rewrites (059-001, 059-005).
- [ ] Remove the Tailwind/npm/TS toolchain from the template (059-006 equivalent), including
      any `RunTailwind.ps1` etc. under `TimeWarp.Architecture/`.
- [ ] Verify `dotnet new timewarp-architecture -n MyApp` produces a building, running app
      with no Tailwind/npm and FluentUI v5.

## Notes

If the divergence between the two trees is large, consider whether the template should be
regenerated from the live `source/` tree rather than hand-ported.
