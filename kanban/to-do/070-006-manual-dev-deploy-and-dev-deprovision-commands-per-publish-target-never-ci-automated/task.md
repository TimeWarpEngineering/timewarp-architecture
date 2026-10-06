# Manual dev deploy and dev deprovision commands per publish target (never CI-automated)

Child of [[070-wire-aspire-publish-for-portable-deploy-compose-kubernetes]].

## Description

Deploying a generated app is a **deliberate operator action**, never a side effect of a merge.
Add `dev deploy` and `dev deprovision` to the template's dev CLI as thin wrappers over
`aspire deploy` / `aspire destroy`, target-aware through the existing `Publish:Target` switch
(`compose` | `kubernetes`, plus `aca` once 070-007 lands).

Reference implementation: the crunchit repo (`tools/dev-cli/endpoints/deploy-command.cs`,
`deprovision-command.cs`). There, both commands have been run successfully against Azure
Container Apps. **Do not copy crunchit's CI `deploy` job.** The template ships no workflow step
that deploys anywhere.

## Requirements

- `dev deploy [--target compose|kubernetes]` (default: the `Publish:Target` default)
  - Preflight: Aspire CLI ≥ 13.6 (reuse `services/aspire-publish.cs`). For kubernetes, Helm ≥ 4.2
    on PATH and a current kubectl context, printed before deploying. Ask for confirmation unless
    `--yes`.
  - Runs `aspire deploy --apphost … --environment Production -- --Publish:Target=<target>`
    (`--non-interactive` with `--yes`).
  - kubernetes: any kubectl context works. A local **kind** cluster is just a context. The skill
    documents the kind recipe: a local registry as `registry-endpoint` and ingress-nginx installed
    as cluster infrastructure. The command does not create clusters.
- `dev deprovision [--target …]` runs `aspire destroy`.
  - `aspire destroy` only knows deployments recorded under `~/.aspire/deployments` on the machine
    that deployed. When it has no record, say so and print the manual removal for that target
    (`docker compose down -v`, or `helm uninstall` plus deleting the PVC). Never fall back to a
    destructive command silently. Require confirmation (`--yes`), because it deletes data.
- Runtime neutrality: no hard-coded `docker`. Compose deploy honours `ASPIRE_CONTAINER_RUNTIME`.
- No CI job, workflow step or `dev workflow` mode calls `dev deploy`. CI keeps publish plus
  safety checks only (070-003 and 070-004).
- dev-cli tests cover argument parsing, preflight refusals and the no-record message, without a
  real deploy.
- Update `skills/tw-deploy` to say how to deploy (`dev deploy` and `dev deprovision`, operator-run,
  never automated) and give the kind recipe. Point the AppHost Design region at it.

## Checklist

- [x] `dev deploy` with preflight, confirmation and target switch
- [x] `dev deprovision` with the no-record guidance and confirmation
- [x] dev-cli tests
- [x] `tw-deploy` skill: manual deploy, kind recipe, never CI-automated
- [x] Gates: `dev build` 0/0, dev-cli-tests, `ganda repo audit`, `dev template-smoke`

## Notes

- Workers never deploy, never start the maintainer's AppHost, and never touch clusters. A real
  deploy to kind or Azure is the maintainer's check after merge.
- Steve, 2026-10-07: "this should not be automated in architecture. `dev deploy` would be preferred."

## Results

### What changed

- `tools/dev-cli/endpoints/deploy-command.cs`: `dev deploy [--target compose|kubernetes] [--yes]`.
  It runs `aspire deploy --apphost <csproj> --environment Production [--non-interactive] --
  --Publish:Target=<t>`. The default target is compose, the AppHost's default. Preflight checks for
  Aspire CLI 13.6 or later. For kubernetes it also needs Helm 4.2 or later and a current kubectl
  context, and prints the context. For compose it prints the container runtime
  (`ASPIRE_CONTAINER_RUNTIME`, otherwise docker). It then shows the plan and prompts for
  confirmation. With `--yes` it skips the prompt and runs non-interactively. If there is no
  terminal and no `--yes`, it refuses.
- `tools/dev-cli/endpoints/deprovision-command.cs`: `dev deprovision [--target …] [--yes]`. It reads
  the local deployment record first. That record is `~/.aspire/deployments/<hash>/production.json`
  (or under `$ASPIRE_HOME`), plus the `.migration` companion when there is one.
  - **No record:** it says so, runs nothing, and prints the manual removal. For compose that is
    `<runtime> compose --project-name aspire-compose-<hash8> down --volumes`. For kubernetes it is
    `helm uninstall` plus `kubectl delete pvc postgres-data`. Exits 1.
  - **Record found, no `--yes`:** it prints the recorded section and what will be deleted. Exits 1.
  - **`--yes`:** it runs `aspire destroy … --non-interactive --yes -- --Publish:Target=<t>`. For
    kubernetes it then prints how to check the PVC.
- `tools/dev-cli/services/aspire-deploy.cs` holds the pure logic: targets, arguments, Helm and
  kubectl parsing, record lookup and operator text. `aspire-deploy-preflight.cs` holds the probes,
  which only read (`aspire --version`, `helm version --short`, `kubectl config current-context`).
- The record hash and section names mirror Aspire.Hosting 13.6. I checked them by decompiling
  `DistributedApplicationBuilder`, `FileDeploymentStateManager` and the Docker/Kubernetes destroy
  steps:
  - hash: SHA256 of the lower-cased full `.csproj` path;
  - sections: `DockerCompose:compose` and `Helm:k8s`.
  `aspire-tests` `DeploymentRecordModel_Given_` compile-includes the helper. It proves the hash,
  the compose project name and both environment names against the real AppHost model.
- Tests: `tests/tools/dev-cli-tests/aspire-deploy-tests.cs` covers argument parsing, preflight
  refusals, record lookup in flat, nested, migration and corrupt state, the no-record text and the
  refusal text. It also has a `NeverAutomated_Given_` guard: no `.github/workflows/*.yml` line and
  no `workflow-command.cs` line may invoke `dev deploy` / `dev deprovision` / `aspire deploy` /
  `aspire destroy`.
- `skills/tw-deploy/SKILL.md` has a new section, "Deploying: `dev deploy` and `dev deprovision`":
  operator-run, never CI, the local-record caveat, and the raw `aspire deploy` / `aspire destroy`
  equivalents. It also has a "Local Kubernetes with kind" recipe: local registry at
  `localhost:5001` as `registry-endpoint`, containerd `certs.d` mapping, ingress-nginx installed
  once as cluster infrastructure, port-forward, and teardown. The AppHost `program.cs` Design
  region now points at both.
- This branch merges `origin/task/070-005-…` (PR #438, still open), because `skills/tw-deploy`
  exists only there. If #438 merges first, this PR's diff shrinks to 070-006. If #438 changes
  again, merge it again.

### Notes for the maintainer

- The dev CLI (`tools/`) is **not packed** into the template. The template csproj content list
  is `source/`, `tests/`, `msbuild/`, `skills/` and the root files. So generated apps have no
  `dev deploy`. That is why the skill also gives the raw `aspire deploy` / `aspire destroy` lines.
  If generated apps should get the dev CLI, that needs its own task.
- I never ran a real deploy or destroy. Workers do not touch clusters. Running `dev deploy` with
  stdin redirected only showed the plan and then refused.

### How to validate

**Smoke**

```bash
cd tests/tools/dev-cli-tests && dotnet test -c Release            # 129/129, incl. aspire-deploy-tests
cd tests/container-apps/aspire/aspire-tests && dotnet test -c Release -- --filter-class DeploymentRecordModel_Given_   # 3/3
dotnet run tools/dev-cli/dev.cs -- deploy --target swarm            # refuses: valid targets compose, kubernetes
dotnet run tools/dev-cli/dev.cs -- deprovision                      # no record → manual compose down text, exit 1
dotnet run tools/dev-cli/dev.cs -- deprovision -t kubernetes        # prints kubectl context + helm, then no-record text
echo n | dotnet run tools/dev-cli/dev.cs -- deploy                  # prints plan, refuses without --yes (no terminal)
# Maintainer, after merge — real deploy to kind per skills/tw-deploy "Local Kubernetes with kind":
dev deploy --target kubernetes && dev deprovision --target kubernetes --yes
```

**Expect**

- Both test suites green. No CI workflow line invokes a deploy (`NeverAutomated_Given_`).
- An unknown target exits 1 with the list of valid targets.
- `deprovision` without a record exits 1. It prints the state path, says "nothing was run and
  nothing was removed", and gives the target's manual removal commands. It never runs them.
- `deploy` without `--yes` and without a terminal exits 1 after printing the AppHost,
  `Environment: Production`, `Publish:Target=<t>` and the preflight line.
- On kind, `dev deploy --target kubernetes` installs the Helm release in the printed context.
  Afterwards, `dev deprovision --target kubernetes` without `--yes` lists the recorded
  `ReleaseName` / `Namespace`. With `--yes` it uninstalls the release.

Gates run: `dev build` 0 warnings / 0 errors. dev-cli-tests 129/129. `dev template-smoke`
succeeded (SmokeDefault, SmokeNoPostgres, SmokeNoApi). `ganda repo audit` is clean (see the
commit).

### Review disposition

- **Rounds:** 2, at effort 3. Round 1 had three read-only reviewers (general, tests and plan_alignment). Round 2 was the oracle re-checking the fixes.
- **Final counts:** bug 0. Suggestions: 1 fixed and 3 wontfix. Nits: 2 fixed and 2 wontfix. **0 open.**
- **Disposition:** `accepted-exceptions`. The wontfix items are M1 (kubectl context not in Aspire's record), M2 (handler `--yes` gate not unit-tested), M3 (Nuru option binding), M7 (`.git` root marker) and M8 (preflight before no-record). The rationale for each is in `review/disposition.md`.
- **Fixed:**
  - The never-CI guard now scans `.github/**/*.yml|yaml` and has a positive-control test.
  - Answering "n" at the deploy prompt gets its own message (`DeployDeclined`).
  - One vacuous assertion was made specific.
  - dev-cli-tests 130/130.
- **Artifacts:** `review/review-framework.md`, `review/round-1/{general,tests,plan-alignment,merged}.md`, `review/round-2/merged.md`, `review/disposition.md`.

## Session

- Created: 2026-10-07 (cockpit, from the 070-005 Azure decision)
- 2026-10-07: implemented (headless implementer, ganda task work). Merged the 070-005 branch for
  `skills/tw-deploy`.
- 2026-10-07: implementation review (review oracle, effort 3), disposition accepted-exceptions.
