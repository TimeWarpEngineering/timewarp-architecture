# Investigate configurable layer separator in feature filename grammar

## Description

Today the axis-1 feature filename grammar is:

```text
<name>[-<function>]-<layer>.cs
```

The **layer boundary uses the same character as kebab-case name segments** (`-`). That makes
files hard to parse by eye and by tools: the last `-`-delimited token is the layer, any
registered function sits immediately before it, and everything else is the name — but nothing
visually marks where the product name ends and the grammar keys begin.

**Example:** `create-role-contracts.cs`

| Reading | Ambiguity |
|---------|-----------|
| name=`create-role`, layer=`contracts` | Correct escape-hatch form |
| name=`create`, function=`role`, layer=`contracts` | Looks plausible until you know `role` is not a registered function |
| name=`create-role-contracts` | Wrong — no layer |

A different **layer separator** (and possibly a separate function separator) would make the
grammar self-evident, e.g.:

| Candidate | Example | Notes |
|-----------|---------|-------|
| `.` (dot before layer) | `create-role.contracts.cs` | Common “name.kind.ext” pattern; layer is the last stem segment before `.cs` |
| `.` with function | `create-role.handler.application.cs`? or `create.handler.application.cs` | Need a clear rule for multi-segment names vs function |
| `__` (double underscore before layer) | `create-role__contracts.cs` | Strong visual break; name stays kebab; no multi-dot basename; uncommon in existing tree |
| `_` (single underscore before layer) | `create-role_contracts.cs` | Distinct from kebab names; weaker boundary than `__`; uglier |
| double hyphen / other | `create-role--contracts.cs` | Visually noisy; rare in tooling |
| Keep `-`, document only | status quo | Cheap; does not fix scannability |

This task is an **investigation / design spike**, not an implement commit. Outcome should be a
recommendation: keep `-`, switch separator(s), and whether the separator is **hard-coded** or
**registry-configurable** (e.g. a `layerSeparator` key in `feature-filename-grammar.json`).

## Requirements

- Document how the current `-` boundary is enforced end-to-end (analyzers, generated props,
  membership targets, docs/skills, `ganda repo audit` kebab rules, TW0001).
- Enumerate separator candidates with concrete before/after filenames from real slices
  (`create-role-contracts.cs`, `create-role-handler.cs`, `get-weather-forecasts-tests.cs`,
  escape-hatch forms like `role-store-application.cs`).
- Call out **breaking-change surface**: every co-located feature/platform file under
  `features/` and `platform/`, MSBuild `**/*-{layer}.cs` globs, TWA0015/0016 messages, skills
  (`tw-feature-placement`), AGENTS.md, template smoke exemplars.
- Decide whether “configurable” means (a) a registry field agents/codegen read so future
  renames are one JSON edit, or (b) only a one-time fixed new convention — and recommend one.
- Compatibility with **registered-unrouted** layer `tests` (task 135) and function→layer map
  (`handler`→application, `endpoint`→server).
- Note OS/tooling constraints: multi-dot basenames (`.contracts.cs`) are fine on Linux/Windows;
  confirm no clash with Blazor `.razor.cs` exceptions or double-extension assumptions elsewhere.
- Deliverable: written recommendation in this task’s Notes (or a short ADR draft if the change
  is adopted later) + a go/no-go for a follow-up implement task. **Do not rename files in this
  task** unless the recommendation is trivial “keep `-`”.

## Checklist

- [ ] Map all consumers of the `-` layer suffix: `feature-filename-grammar.json` → `.g.cs` /
      `feature-filename-grammar.g.props` (web/api/grpc), analyzer parse path (TWA0015/0016),
      membership targets, docs/skills, any scripts
- [ ] Sketch parse rules for top candidates (especially `.` as layer separator) including
  multi-hyphen names and optional function segment
- [ ] Evaluate “configurable in JSON” vs hard-coded new separator (cost of dual-mode /
  migration window if any)
- [ ] Sample rename matrix for one web slice + one unrouted `*-tests.cs` + one escape-hatch file
- [ ] Flag interactions: TW0001 kebab paths, `kebab-path-names` audit, template packaging,
  `dev template-smoke` exemplars
- [ ] Write recommendation (separator choice, configurability, migrate vs dual-accept window)
- [ ] If go: spawn follow-up implement task; if no-go: record rationale and close this task

## Notes

### Motivation (from create)

`create-role-contracts.cs` uses `-` for both name segments and the layer key, so the eye cannot
tell the grammar boundary. Alternatives floated: `create-role.contracts.cs` (dot),
`create-role__contracts.cs` (double underscore), or something else.

### SSOT touchpoints (starting list)

- `source/analyzers/timewarp-architecture-convention-analyzers/feature-filename-grammar.json`
- `feature-filename-grammar-analyzer.cs` (longest-suffix layer match; function closed set)
- Generated: `feature-filename-grammar.g.cs`,
  `source/container-apps/{web,api,grpc}/msbuild/feature-filename-grammar.g.props`
- Skills: `tw-feature-placement`; AGENTS.md axis-1 section
- Tests: `tests/analyzers/.../feature-filename-grammar-analyzer-tests.cs`

### Open questions for the spike

1. Is the separator only before **layer**, or also before **function**?
2. Should function stay kebab-adjacent (`create-role-handler.application.cs`) or also use the
   new separator (`create-role.handler.application.cs`)?
3. Migration: big-bang rename in-repo + template, or temporary dual-accept of old and new forms?
4. Does a `.` layer separator conflict with any existing non-grammar multi-dot `.cs` names?
5. Does `__` collide with any private/dunder-style naming we care about, or with shell/glob
   patterns that treat `_` specially?

## Session

- Created: 2026-07-30 (investigation request: configurable layer separator / scannability of
  `create-role-contracts.cs`-style names)
