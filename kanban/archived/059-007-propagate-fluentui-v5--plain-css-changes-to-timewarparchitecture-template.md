# Propagate FluentUI v5 + plain-CSS changes to the TimeWarp.Architecture template

Parent: 059. Last — after 001–006 land in the live `source/` app.

> **SUPERSEDED by 047 (2026-06-24).** The premise below ("two copies; port v5/CSS to the template
> web-spa") is stale: 047 already migrated the app source OUT of `TimeWarp.Architecture/Source/` to
> root `source/`, and 047's end-state is that **root `source/` *becomes* the template**. The root app
> is already on FluentUI v5 + plain CSS (001–006), so there is nothing to "port" — the propagation is
> automatic once 047 lands. The one unique requirement ("the template must ship v5 + plain CSS, with
> NO Tailwind/npm — drop the dead `Run*.ps1` scripts, the `template.json` npm `postAction`, and the
> devcontainer/CLAUDE.md refs") has been folded into 047 as an acceptance criterion. Archiving this task.

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

## Carried over from 059-006 (npm toolchain in the template)
- [ ] Delete the dead npm/Tailwind scripts under `TimeWarp.Architecture/`:
      `RunTailwind.ps1`, `RunNpmInstall.ps1`, `NpmOutdated.ps1` (and any docs/dev-cli references).
      web-spa already dropped its npm toolchain (059-006); the template copies should follow.
