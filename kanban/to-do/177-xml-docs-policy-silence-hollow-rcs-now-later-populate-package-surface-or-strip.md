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
- [ ] CS1591 warning gated on `IsPackable` in root `Directory.Build.props`; verify a package
      project with an undocumented public member fails `dev build`, and a container-app one does not
- [ ] A: every public type/member in packable projects has a real summary; foundation hollow
      shells filled or removed
- [ ] B: hollow `<param>`/`<returns>` shells removed from container-apps / tools / tests
- [ ] `dev build` 0/0; `dev template-smoke` (packages ship into generated apps)
- [ ] AGENTS.md one-liner
- [ ] Reconcile the `.editorconfig` comment block (lines ~299–320) with the final policy

## Related

- Task **171** — TW0002 off (XML→markdown nag)
- Task **172** — style policy; RCS1138/1139 first silenced there
- Roslynator: [RCS1141](https://josefpihrt.github.io/docs/roslynator/analyzers/RCS1141/) add param, [RCS1228](https://josefpihrt.github.io/docs/roslynator/analyzers/RCS1228/) unused element, [RCS1263](https://josefpihrt.github.io/docs/roslynator/analyzers/RCS1263/) invalid reference

## Session

### Immediate decision (2026-08-06)

- **WHEN:** not now for full docs quality.
- **Now:** silence completeness/hollow cluster so template builds and agents stay on Purpose regions.
- **Later:** this task — A (populate package XML) or B (strip). Default lean: **A for packages, no force on template demo code.**

## Notes

- No Roslynator rule specifically “add `<returns>`”; returns mainly appear under RCS1228 empty-element cleanup.
- Enforcing “good docs” ≠ enabling RCS1141 (empty shells).
