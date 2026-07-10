# Decouple template feature flags (api/web/yarp/counter/eventstream/postgres)

Spun out of [[064-repoint-template-to-root-layout]]. 064 repointed the `dotnet new
timewarp-architecture` template to the root layout and verified the DEFAULT (all-features) template
builds 0 errors. This task makes the **feature-flag-OFF** combinations produce compiling solutions.

## Background

After 064, generating with a feature disabled correctly DROPS that feature's files and excludes its
projects from the `.slnx`. But other code still **references** the excluded feature without `#if`
guards, so the generated solution fails to compile. This is genuine feature coupling, not a template
artifact (the blank-line/IDE2000 and `.slnx`-conditional artifacts were already fixed in 064).

## Per-flag status (re-verified 2026-07-11)

Value assessment (2026-07-11, maintainer decision): flags are the **architecture axis** only.
`counter` and `eventstream` were demo-content flags — de-flagged; the demos ship unconditionally
and are removed by hand/agent (see HowToRemoveDemoFeatures.md). Surviving flags:
api / grpc / web / yarp / postgres.

| Flag (`--X false`) | Result |
|--------------------|--------|
| `grpc`             | ✅ builds clean (064) |
| `postgres`         | ✅ builds clean (guarded postgres-only global usings) |
| `yarp`             | ✅ builds clean (guarded app-host usings; MessagePack pin; IDE2000 blank line) |
| `api`              | ✅ builds clean (generator ref moved out of #if(api); TableHeader/Cell promoted to shared components; test fixtures decoupled) |
| `web`              | ✅ builds clean (headless API — test infra owns its client: TestApiService replaces the SPA client stack in timewarp-testing) |
| `counter`          | — de-flagged (demo ships unconditionally) |
| `eventstream`      | — de-flagged (demo ships unconditionally; its ~124-error coupling died with the flag) |

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

- [x] `--postgres false`
- [x] `--counter false` (fixed, then flag removed by value assessment)
- [x] `--yarp false`
- [x] `--api false`
- [x] `--web false` (test-owned TestApiService; reflection hack deleted)
- [x] `--eventstream false` → resolved by de-flagging (demo ships unconditionally)
- [ ] Re-verify ALL single-flag-off combos build clean; spot-check a couple multi-flag-off combos
- [ ] Confirm the real repo `dev build` stays green after each guard is added
- [x] Follow-ups filed: 087 (#if-in-comments guard), 088 (feature-isolation analyzer), 089 (DefineConstants↔template.json), 090 (BaseApiService body serialization)
- [x] HowToRemoveDemoFeatures.md (agent-driven demo removal guide)

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

## Results (2026-07-11)

**Every surviving flag combination generates a compiling solution.** Verified matrix
(pack → install → generate `--foundationPackages false` → build):

| Combo | Errors |
|-------|--------|
| Default (all-on; counter + eventstream demos present) | 0 |
| NoGrpc / NoApi / NoWeb / NoYarp / NoPostgres | 0 each |
| ApiOnly (`--web false --grpc false --yarp false`) — headless API | 0 |
| WebOnly (`--api false --grpc false --yarp false --postgres false`) | 0 |

Real repo `dev build` 0/0 throughout.

### Scope decision (maintainer, 2026-07-11)

Flags are the **architecture axis** only: api / grpc / web / yarp / postgres. `counter` and
`eventstream` (demo content) were **de-flagged** — demos ship unconditionally; removal is
agent/hand-driven (HowToRemoveDemoFeatures.md). Rationale: demo flags were the most expensive
guards (eventstream ≈124 errors) serving content users delete after reading; flag matrices rot
silently (this one went stale in 6 days). Coupling detection moves to build-time analyzers
(tasks 088/089) instead of a manual generation loop.

### Key changes

- **Blocker 086** (pre-req): template engine mangles `.cs` files with `#if` tokens in comments — 3 files reworded.
- **postgres**: guarded postgres-only namespaces' global usings (web-infrastructure, web-server).
- **counter** (pre-de-flag): StyleGuidePage TriggerException + nav link guards; later unwrapped.
- **yarp**: app-host Aspire.Hosting.Yarp usings; IDE2000 blank line in app-host program.cs;
  MessagePack 2.5.302 direct pin (StreamJsonRpc pulls vulnerable 2.5.192; only Aspire.Hosting.Yarp
  lifted it).
- **api**: real bug — the source-generator ProjectReference sat inside `#if(api)` in web-spa.csproj,
  so api-off apps lost ALL [Page]/[StateAccess] generation (116-error cascade). TableHeader/Cell
  promoted from weather-forecast to shared components/ (superhero used them cross-feature).
- **web**: test infrastructure decoupled from the SPA client stack. New `TestApiService`
  (timewarp-testing) implements foundation `IApiService` over HttpClient with
  ContractSerializationDefaults; all fixtures use it; WebApiTestService's reflection into
  BaseApiService's private transport deleted; dead IWebServerApiService registration dropped;
  yarp fixture's web/api members fully conditional (incl. conditional ctor comma).
- **De-flag**: counter/eventstream symbols + excludes removed from template.json; all their
  `#if` regions unwrapped (undefined symbols evaluate false — leftovers would strip the demos);
  DefineConstants cleaned.

### Test outcomes

- In-proc integration tests pass — CloneStateBehavior constructs all three swapped fixtures
  (yarp/web/api) and TestApiService end-to-end. Foundation suites 22/22.
- 13 aspire-path tests (web-spa aspire + api-server suites) failed **environmentally**: Docker
  daemon unhealthy in the dev environment; they die at DCP's docker health check before any
  changed code runs (pre-existing dependency). Rerun `dev test` with Docker Desktop up to confirm.

### Follow-ups filed

087 (#if-in-comments template guard) · 088 (feature-isolation analyzer) ·
089 (DefineConstants↔template.json agreement) · 090 (BaseApiService body serialized without seam
options — found writing TestApiService).

Commits: 5ef8d7b6 (086), 4dd8d201 (postgres/counter/yarp), a2ece8ca (api), 3d96a9ce (web),
721be7a0 (de-flag).
