# Decouple template feature flags (api/web/yarp/counter/eventstream/postgres)

Spun out of [[064-repoint-template-to-root-layout]]. 064 repointed the `dotnet new
timewarp-architecture` template to the root layout and verified the DEFAULT (all-features) template
builds 0 errors. This task makes the **feature-flag-OFF** combinations produce compiling solutions.

## Background

After 064, generating with a feature disabled correctly DROPS that feature's files and excludes its
projects from the `.slnx`. But other code still **references** the excluded feature without `#if`
guards, so the generated solution fails to compile. This is genuine feature coupling, not a template
artifact (the blank-line/IDE2000 and `.slnx`-conditional artifacts were already fixed in 064).

## Per-flag status (verified 2026-06-26, clean project names)

| Flag (`--X false`) | Result |
|--------------------|--------|
| `grpc`             | ✅ builds clean (done in 064) |
| `api`              | ❌ ~4 CS0234 + ~20 RZ10012 (Razor components ref weather/api) |
| `web`              | ❌ CS0234 + CS0246 |
| `yarp`             | ❌ ~4 CS0234 |
| `counter`          | ❌ ~2 CS0246 |
| `eventstream`      | ❌ ~124 CS0234 (pervasive coupling — biggest) |
| `postgres`         | ❌ ~2 CS0234 |

## Approach

For each failing flag, generate `dotnet new timewarp-architecture -n NoX --X false`, build, and for
each error either:
- add `#if(X)` / `#endif` guards around the referencing code (using statements, DI registrations,
  Razor `@using`/component refs, route registrations), or
- add the referencing file to the `(!X)` exclude list in `.template.config/template.json` if it's
  wholly feature-specific.

Mind the blank-line rule: don't leave blank lines immediately around feature `#if` blocks (the
engine strips them and IDE2000 fails the generated build) — see the `web-spa/global-usings.cs` fix.

## Checklist

- [ ] `--postgres false` (smallest, ~2 errors) — likely a couple of unguarded refs
- [ ] `--counter false` (~2)
- [ ] `--yarp false` (~4)
- [ ] `--api false` (~4 CS0234 + ~20 RZ10012 razor)
- [ ] `--web false` (CS0234 + CS0246)
- [ ] `--eventstream false` (~124 — pervasive; investigate the coupling first)
- [ ] Re-verify ALL single-flag-off combos build clean; spot-check a couple multi-flag-off combos
- [ ] Confirm the real repo `dev build` stays green after each guard is added

## Notes

- Test names MUST be underscore-free (`-n NoApi`, not `No_api`) — `-n` becomes the namespace and
  CA1707 flags underscores, producing false-positive errors.
- Verify loop: `dotnet pack` the template csproj → `dotnet new install --force` → `dotnet new
  timewarp-architecture -n NoX --X false` → `dotnet build`.
- ~~Blocked by #086~~ — RESOLVED 2026-07-08: template generation mangled any `.cs` file with an
  `#if`-family token in a comment (CS1513 in every generated app, any flag). Three files reworded;
  verification loop now runs. See done/086.

## Implementation Plan

**Verification loop (per flag):**
1. `dotnet pack` the template csproj in `timewarp-templates/`
2. `dotnet new install --force`
3. `dotnet new timewarp-architecture -n NoX --X false` (name must be underscore-free)
4. `dotnet build` the generated solution and capture errors
5. For each error:
   - If referencing code is feature-specific → add `#if(X)` / `#endif` guard (no blank lines immediately adjacent to the block)
   - If file is wholly feature-specific → add it to the `(!X)` exclude list in `.template.config/template.json`
6. Repeat until `dotnet build` succeeds clean

**Flag order (smallest → largest):**
- `--postgres false`
- `--counter false`
- `--yarp false`
- `--api false`
- `--web false`
- `--eventstream false` (investigate pervasive coupling first)

**Final verification:**
- Re-run ALL single-flag-off combinations
- Spot-check 2–3 multi-flag-off combinations
- Confirm `dev build` on the real repo stays green after every guard added

**Constraints:**
- Preserve existing `#if` regions and preprocessor structure
- Never leave blank lines around injected `#if` blocks (IDE2000)
- Keep test names underscore-free to avoid CA1707 noise

The existing checklist and per-flag status table remain the source of truth; this plan simply codifies the mechanical loop and ordering.

## Session

- Created: 2026-06-26 (spun out of 064)
