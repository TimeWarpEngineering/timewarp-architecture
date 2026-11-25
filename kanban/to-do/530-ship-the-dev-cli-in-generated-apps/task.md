# Ship the dev CLI in generated apps

## Description

A generated app has no dev CLI. The template pack is an allow-list
(`timewarp-templates/source/timewarp-architecture-template/timewarp-architecture-template.csproj`):
`source/`, `tests/`, `msbuild/`, `skills/`, `.template.config/`, the kanban skeleton and the root
build files. `tools/` is not on it, so `tools/dev-cli/` never reaches the output.

The skills the pack does ship tell the app to use that CLI: `tw-deploy` says the first deploy is
`dev deploy --target <t>`, and it names `dev publish`, `dev deploy migrate`, `dev open` and
`dev deprovision`. An instance cannot follow its own skills.

Ship the dev CLI in the generated app, without the commands that only make sense in the template repo.

The first instance repo (see task 491) will be regenerated from this template and needs `dev build`,
`dev test`, `dev run`, `dev db`, `dev publish` and `dev deploy` on day one.

## Propagation

template-only

## Requirements

- `dotnet new timewarp-architecture -n X` output contains `tools/dev-cli/` and it builds and runs
  there (`dotnet run tools/dev-cli/dev.cs -- --help`, and the self-install that produces `bin/dev`).
- The generated CLI has the commands an app uses: build, test, run, db, publish, deploy (with
  migrate), open, deprovision, entra, workflow, regenerate-generated when it applies.
- Template-only commands do not ship, or are excluded by `template.json`: template-smoke,
  template-install, template-publish-smoke, check-client-names, lint-skills, check-version and the
  NuGet release path. Decide each one and list the decision in Results.
- Flag-off generation leaves no dead command: a command for api, grpc, yarp or postgres is excluded
  or compiles and refuses cleanly when that flag is off.
- `sourceName` rewriting does not break the CLI (project paths, solution name, AppHost path).
- `dev template-smoke` builds the generated CLI and runs at least `dev build` through it, for the
  default variant and in `--matrix`.

## Checklist

- [ ] Confirm the gap first: generate from the packed template and list the output root.
- [ ] Add `tools/dev-cli/**` to the pack allow-list; keep `tools/agent-identity-cli` out.
- [ ] `template.json` excludes for template-only endpoints and services, and per-flag excludes.
- [ ] `tests/tools/dev-cli-tests` is excluded today; decide whether an app-side subset ships.
- [ ] `.timewarp/dev.jsonc` and anything else the CLI reads at the repo root ships too.
- [ ] `dev template-smoke` assertion and matrix coverage.
- [ ] `readme.md` and `kanban/readme.md`: what a generated app contains.

## Notes

- Found 2026-11-25 while scoping the instance CI workflow (task 531) and the release deploy job
  (task 529). Both call `dev` commands and depend on this.
- `template.json` already excludes `tools/agent-identity-cli/**` and `tests/tools/dev-cli-tests/**`
  but not `tools/dev-cli/**`. That only matters for a repo-root install; the NuGet pack never
  includes `tools/` at all.
- The first instance repo has its own older `tools/dev-cli`. After this task it takes the template's.

## Session

- Created: f7eccb81-87c9-4fb4-a0c4-0fb7417d4a2f (2026-11-25)

## Results (required before done)

### How to validate (required before done)
