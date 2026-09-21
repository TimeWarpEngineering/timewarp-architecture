# XML docs policy: silence hollow RCS now; later populate package surface OR strip

## Description

This repo **is** `dotnet new timewarp-architecture` — docs decisions ship to every generated app.

**Now (done with 172/177 editorconfig):** turn off hollow/completeness XML analyzer noise so CI and
agents are not steered into empty `<param></param>` shells or delete-only fix-alls.

**Later (this task):** choose and execute one end state:

| Path | What |
|------|------|
| **A — Populate (preferred if we keep NuGet packages)** | Real `///` on **published** package public APIs (`TimeWarp.Foundation.*`, `TimeWarp.Identity`, Attributes, etc.) for consumer IntelliSense. Leave template demo / app code on Purpose regions only. |
| **B — Strip** | Remove hollow and non-essential `///` from template/app code (and optionally packages if we truly do not care). No XML doc build gates. |

Do **not** enable RCS1141/1228 alone — they fight (add empty vs remove empty). If we enforce docs,
we enforce **substance** (summary text + accurate param names), not shell generators.

## Do docs help agents?

| Source | Helps agents? |
|--------|----------------|
| `#region Purpose` / `Design` | **Yes** — house SSOT for agents (TWA0004) |
| Skills / AGENTS.md / contracts | **Yes** |
| Hollow `/// <param name="x"></param>` | **No** — noise |
| Real package XML on public NuGet surface | **Sometimes** — mainly human/IDE IntelliSense for consumers; agents can read source |

**Conclusion:** for day-to-day agent work in this template, Purpose/skills win. XML is optional
product surface for **package consumers**, not the agent context layer.

## Decision (2026-09-21)

Path **A for published packages, B for template/app code**, plus a build gate so it does not decay.

The task's "docs don't help agents" conclusion holds only inside this monorepo. Generated apps are
package-mode only: an agent working there sees `TimeWarp.Foundation.*` / `TimeWarp.Identity` /
Attributes only as metadata plus the shipped `.xml`. There the XML doc IS the platform context
layer. The nupkgs already ship `.xml` files, so today consumers get IntelliSense for roughly half
the surface (foundation: 56 public types in 54 files, 31 files with zero `///`; attributes: 4 of 6
files undocumented) and nothing for the rest.

## Requirements

**A — populate the packable surface** (every project with `IsPackable=true` / `PackageId`:
`source/foundation/**`, `source/libraries/timewarp-identity`, `timewarp-modules`, `timewarp-402`,
`source/analyzers/timewarp-architecture-attributes`, and the two analyzer packages' public types):
- Real `<summary>` on every **public** type and public member. Substance, not shells: say what it
  is for and when to use it; name the contract (fail-closed, one-way, etc.) when there is one.
  `<param>` / `<returns>` / `<typeparam>` only when they add information beyond the name.
- Purpose/Design regions stay the SSOT for *why*; the XML summary is the *what* for a consumer
  who cannot see the region. Do not duplicate a Design region into XML — link intent in one line.
- Fill or delete the existing hollow `<param name="x"></param>` shells in foundation (7).

**B — strip hollow shells from template/app code** (`source/container-apps/**`, `tools/**`,
`tests/**`): delete empty `<param>`/`<returns>` elements (13 today: web-spa api-service,
api-handler, counter-state.debug, tests). Leave real summaries alone. Do not add docs here.

**Gate** — CS1591 (missing XML comment on public member) as **warning** only where
`'$(IsPackable)' == 'true'`; everywhere else it stays in NoWarn. Put the switch in root
`Directory.Build.props` next to the existing NoWarn line (task 170 comment) so both modes are
visible in one place. Warnings are errors, so a new public package API without a summary fails
`dev build`. Do **not** re-enable RCS1141/1228 (shell generators).

**Docs** — one-liner in AGENTS.md (Documentation section): packages carry real XML on public
surface (CS1591 gated on IsPackable); template/app code uses Purpose/Design regions only.

## Checklist

- [x] Explicitly silence RCS1138–1142, RCS1228 (completeness + hollow) — editorconfig
- [x] Leave RCS1263 as warning (invalid doc refs when `///` exists)
- [x] CS1591 remains NoWarn for non-package projects
- [x] Decide Path **A** (packages) + **B** (template/app) — see Decision
- [x] CS1591 warning gated on `IsPackable` in root `Directory.Build.props`; verify a package
      project with an undocumented public member fails `dev build`, and a container-app one does not
- [x] A: every public type/member in packable projects has a real summary; foundation hollow
      shells filled or removed
- [x] B: hollow `<param>`/`<returns>` shells removed from container-apps / tools / tests
- [x] `dev build` 0/0; `dev template-smoke` (packages ship into generated apps)
- [x] AGENTS.md one-liner
- [x] Reconcile the `.editorconfig` comment block (lines ~299–320) with the final policy

## Related

- Task **171** — TW0002 off (XML→markdown nag)
- Task **172** — style policy; RCS1138/1139 first silenced there
- Roslynator: [RCS1141](https://josefpihrt.github.io/docs/roslynator/analyzers/RCS1141/) add param, [RCS1228](https://josefpihrt.github.io/docs/roslynator/analyzers/RCS1228/) unused element, [RCS1263](https://josefpihrt.github.io/docs/roslynator/analyzers/RCS1263/) invalid reference

## Session

### Immediate decision (2026-08-06)

- **WHEN:** not now for full docs quality.
- **Now:** silence completeness/hollow cluster so template builds and agents stay on Purpose regions.
- **Later:** this task — A (populate package XML) or B (strip). Default lean: **A for packages, no force on template demo code.**

- Implementer: grok session (2026-09-21) — Path A packages + Path B template/app + CS1591 gate

## Notes

- No Roslynator rule specifically “add `<returns>`”; returns mainly appear under RCS1228 empty-element cleanup.
- Enforcing “good docs” ≠ enabling RCS1141 (empty shells).
- CS1591 add-to-NoWarn lives in `Directory.Build.targets` (`PackableXmlDocs`) because child props/csproj flip `IsPackable` after root `Directory.Build.props` runs. Both modes are documented next to the NoWarn list in props (task 170 comment).
- TypedId generated BCL (`Value`/`New`/`From`/parse/compare) carries XML in `TypedIdSourceGenerator.EmitBcl` so packable identity ids stay documented.

## Results

Path **A** (published packages) + **B** (template/app) executed. Packable public surface has real `<summary>` text; template/app hollow `<param>`/`<returns>`/`<typeparam>` shells are gone; CS1591 is a warning only when `IsPackable=true` (TreatWarningsAsErrors fails a new undocumented public package API). RCS1141/1228 stay off.

**Files changed (high level)**
- Gate: `Directory.Build.props`, `Directory.Build.targets` (`PackableXmlDocs` + `IAssemblyMarker` summary), `source/foundation/Directory.Build.props` comment
- Policy docs: `AGENTS.md` Documentation section, `.editorconfig` XML-analyzer comment block
- A — packable XML: `source/foundation/**`, `source/libraries/timewarp-{identity,402,modules}`, `source/analyzers/timewarp-architecture-{attributes,analyzers,convention-analyzers}`, TypedId generator emit
- B — hollow strip: web-spa `api-server-api-service`, `api-handler`, `base-handler`, `counter-state.debug`; tests `timewarp-testing` (3 files). No tools/ hits.

**Key decisions**
- CS1591 only (not 1570–1592 completeness cluster). Empty param tags deleted rather than filled when they added nothing beyond the name.
- Generated TypedId members documented in the generator, not copied onto each user partial.
- Analyzer XML sits above `[DiagnosticAnalyzer]`/`[Generator]` (house order).

**Test outcomes**
- `dotnet run --file tools/dev-cli/dev.cs -- build` — 0 warning(s), 0 error(s)
- `dotnet run --file tools/dev-cli/dev.cs -- template-smoke` — SUCCEEDED (defaults + SmokeNoApi)
- Probe: undocumented public type in `foundation-domain` → CS1591 error; `web-contracts` (not packable) builds with cs1591=0
- Hollow-shell rg across `*.cs` → none remaining

### How to validate

**Smoke**
```bash
# 1) Full solution must be 0/0
dotnet run --file tools/dev-cli/dev.cs -- build
# expect: "Build succeeded." then "0 Warning(s)" / "0 Error(s)"

# 2) Packable CS1591 gate (temporary public type must fail)
# add a public type with no /// to source/foundation/foundation-domain/, then:
dotnet build source/foundation/foundation-domain/foundation-domain.csproj -c Release
# expect: error CS1591 on that type; delete the probe file after

# 3) Template/app stays silent
dotnet build source/container-apps/web/projects/web-contracts/web-contracts.csproj -c Release
# expect: exit 0, no CS1591

# 4) No hollow shells
rg --glob '*.cs' '<param name="[^"]*"></param>|<returns></returns>|<typeparam name="[^"]*"></typeparam>'
# expect: no matches

# 5) Packages still pack with XML (template-smoke)
dotnet run --file tools/dev-cli/dev.cs -- template-smoke
# expect: "Template smoke SUCCEEDED"
```

**Expect**
- Packable projects (`IsPackable=true`) fail the build on a new undocumented public member (CS1591 as error via TreatWarningsAsErrors).
- Container-app / tests / tools projects do not report CS1591.
- Generated apps from `template-smoke` restore platform nupkgs and build 0/0 (package-mode; XML ships in those nupkgs for consumer IntelliSense).
- RCS1141/RCS1228 remain `severity = none` in `.editorconfig`.

**Automated gate**
```bash
dotnet run --file tools/dev-cli/dev.cs -- build
dotnet run --file tools/dev-cli/dev.cs -- template-smoke
```

**Not in scope:** enabling RCS1141/1228; adding XML to template demo/app code; quality nits on existing real summaries that already satisfied CS1591.
