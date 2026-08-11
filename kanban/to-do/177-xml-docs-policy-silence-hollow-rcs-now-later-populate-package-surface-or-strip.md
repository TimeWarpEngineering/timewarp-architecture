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

## Checklist

- [x] Explicitly silence RCS1138–1142, RCS1228 (completeness + hollow) — editorconfig
- [x] Leave RCS1263 as warning (invalid doc refs when `///` exists)
- [x] CS1591 remains NoWarn (GenerateDocumentationFile is for IDE0005 enablement, not public-doc gate)
- [ ] Decide Path **A** (populate package surface) vs **B** (strip)
- [ ] If A: inventory public package APIs missing real summaries; populate; optional warning only on package projects
- [ ] If B: strip empty/orphaned `///` from template tree; do not re-enable completeness RCS
- [ ] Document choice in AGENTS.md or developer standards one-liner

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
