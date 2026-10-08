# dev deploy: deploy config and validation checks instead of Aspire parameter prompts

## Description

On 2026-10-08, `dev deploy --target kubernetes` failed at Aspire's `process-parameters` step with
`k8s-namespace: ❌ Failed to read input in non-interactive mode` (exit 6). The log is
`~/.aspire/logs/cli_20261008T071330_4f936a71.log`.

The cause is that the AppHost declares four deploy parameters with no value, and nothing supplies
them:
- `k8s-namespace`
- `helm-release-name`
- `registry-endpoint`
- `registry-repository`

They are declared in `source/container-apps/aspire/projects/aspire-app-host/program.cs:244-257`, and
the names are in `constants.cs:68-73`. There is no appsettings `Parameters` section, no user secret,
no `Parameters__*` env var, and the deployment state under `~/.aspire/deployments/` is empty.

Three things in the dev CLI make it worse:
- `tools/dev-cli/endpoints/deploy-command.cs:81-90` runs `aspire` through Amuru `PassthroughAsync`,
  which pipes stdin and gives Aspire no TTY. Aspire therefore can't prompt even when `--yes` is not
  passed. That contradicts the comment at lines 20-24, which says "Without --yes Aspire also prompts
  for any missing parameter."
- `dev deploy` has no way to forward `--Parameters:*`. `BuildDeployArguments` in
  `tools/dev-cli/services/aspire-deploy.cs:87-90` only emits `--Publish:Target=<target>` after `--`.
- Preflight checks that a kubectl context is present, but not that its API answers.

Direction from Steven: the dev CLI should make this easy. Don't hard-code defaults. Instead,
`dev deploy` should read explicit per-target deploy configuration and run validation checks that fail
fast with one clear message before Aspire runs.

## Requirements

- **Per-target deploy config.** `dev deploy` reads it and forwards each value to Aspire as
  `--Parameters:<name>=<value>` after `--`.
  - The source of truth must not ship per-deployment values in the template. The tw-deploy skill
    says per-deployment values are parameters, not literals, so there are no `WithNamespace("…")`
    literals and no committed `appsettings.Production.json` values.
  - Decide in this task where the config lives, and document the choice. Candidates:
    - AppHost user secrets `Parameters:*` (where `postgres-password` already lives);
    - a git-ignored local deploy config file;
    - a `dev deploy config set` / `dev deploy config show` command that writes user secrets.
- **Validation checks in preflight, before `aspire` is invoked:**
  - Every required parameter for the selected target is resolved. If any are missing, list them and
    give the exact command to set each one.
  - The kubectl current context is reachable (its API responds), not just present.
  - For kind contexts: the kind cluster exists (`kind get clusters`), and the local registry
    container (`kind-registry` / whatever `registry-endpoint` points at) is running.
  - Keep the helm and aspire version checks as they are today.
  - Anything missing produces one actionable report and a non-zero exit, never an Aspire prompt
    crash.
- **TTY passthrough.** Switch `PassthroughAsync` to `TtyPassthroughAsync` so any prompt Aspire still
  shows works interactively. Fix the misleading comment at `deploy-command.cs:20-24` either way.
- **pwsh syntax.** Show operator-facing commands in pwsh syntax, because Steven uses pwsh. If env
  vars are mentioned, write them as `${env:Parameters__k8s-namespace} = '...'`, not bash `export`.
- **Tests** in `tests/tools/dev-cli-tests/aspire-deploy-tests.cs` covering:
  - config resolution;
  - `--Parameters:*` argument forwarding (both `--yes` and interactive forms);
  - each validation refusal (missing parameter, unreachable context, missing kind cluster, registry
    not running).

  Keep the `NeverAutomated_Given_` guard: no test ever invokes a real deploy.
- **Docs.** Update the deploy section of `skills/tw-deploy/SKILL.md`: where config lives, how to set
  it, and what preflight checks.
- **Proof.** Before review, the PR must include test output, plus a preflight run that shows the
  refusal text and a successful plan.

## Checklist

- [x] Decide and document where the deploy config lives (user secrets, git-ignored file, or
      `dev deploy config` command)
- [x] Read the config and forward it as `--Parameters:<name>=<value>` after `--`
- [x] Preflight: required parameters resolved (missing list + exact pwsh/`dev` command to set each)
- [x] Preflight: kubectl current context API reachable
- [x] Preflight: kind cluster exists and local registry container running (kind contexts)
- [x] `PassthroughAsync` → `TtyPassthroughAsync`; comment at `deploy-command.cs:20-24` corrected
- [x] Tests in `aspire-deploy-tests.cs` (config, forwarding, each refusal); NeverAutomated guard
      intact
- [x] `skills/tw-deploy/SKILL.md` deploy section updated
- [x] Proof in PR: test output + preflight refusal text + successful plan

## Session

- Created: 3130576 (2026-10-08)
- 2026-10-08 implement (ganda task work): config in AppHost user secrets, preflight checks, forwarding, TTY passthrough, tests, skill; proof below.
- 2026-10-08 review (ganda task work, tw-implementation-review, effort 2, roster general): 2 rounds, disposition clean. Reviewer subagents aa1b8e5e244d9df8d (round 1) and ace0b5c0c3151748d (round 2); fix implementer a9716e483b7c14928.
- Review oracle: review by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 120 — 2026-10-08T08:22:25Z

## Results

**Decision — where deploy config lives:** AppHost user secrets, `Parameters:<name>`. It is the
same per-machine, never-committed store that already holds `postgres-password` and
`Azure:SubscriptionId`. It is set with plain `dotnet user-secrets set` (no new `dev deploy config`
verb, because wrapping one tool call adds nothing), and a `Parameters__<name>` env var overrides it
for one session. The required list per target is the AppHost's value-less parameters: for
kubernetes `k8s-namespace`, `helm-release-name`, `registry-endpoint`, `registry-repository`; for
compose and aca, none. Documented in the `aspire-deploy.cs` Design region and in `skills/tw-deploy`.

**What changed:**
- `tools/dev-cli/services/aspire-deploy.cs` (pure): `RequiredParameters`, `ResolveParameters`
  (env, then user secret), `BuildMissingParameterLines` (pwsh: `dotnet user-secrets set '…'` and
  `${env:Parameters__…} = '…'`), `BuildDeployArguments` now forwards `--Parameters:<name>=<value>`
  after `--`, plus kubectl reachability (`--context <ctx> get --raw /version --request-timeout=5s`),
  kind cluster (`kind get clusters`), registry `/v2/` probe URI and refusals, and a single
  `BuildPreflightReport`. The plan lists each forwarded parameter and its source.
- `aspire-deploy-preflight.cs`: kubernetes problems are collected into **one** report and the
  command exits 1 before `aspire` runs. A missing kind cluster is reported as the root cause in
  place of the unreachable API. The registry check is an HTTP GET, so the verbs still call no
  container CLI. `dev deprovision` keeps the cluster checks but skips parameters and the registry
  (`resolveParameters: false`).
- `deploy-command.cs` / `deprovision-command.cs`: `PassthroughAsync` → `TtyPassthroughAsync`, so
  Aspire's own prompts (deploy, and destroy's confirmation) reach the terminal. The misleading
  "Aspire also prompts" Design comment is rewritten.
- Tests: 17 new cases in `aspire-deploy-tests.cs` (`DeployParameters_Given_`, `ClusterChecks_Given_`,
  forwarding in both `--yes` and interactive forms). The `NeverAutomated_Given_` guard is unchanged.

### Proof (2026-10-08, this worktree)

Tests — `cd tests/tools/dev-cli-tests && dotnet test -c Release`:
```
Test run summary: Passed!  total: 134  failed: 0  succeeded: 134  skipped: 0
```
`dev build`: 0 Warning(s), 0 Error(s). `ganda repo audit`: passes all checks.

Refusal: the real stale `kind-simple` context, with nothing configured
(`dotnet run tools/dev-cli/dev.cs -- deploy --target kubernetes </dev/null`, exit 1):
```
dev deploy preflight failed (2 problems); nothing was run:
- Missing deploy parameters for kubernetes: k8s-namespace, helm-release-name, registry-endpoint, registry-repository. Set each once in the AppHost user secrets (per machine, never committed):
  dotnet user-secrets set 'Parameters:k8s-namespace' '<value>' --project '<repo>/source/container-apps/aspire/projects/aspire-app-host/aspire-app-host.csproj'
  … (one line per parameter)
or for the current pwsh session only:
  ${env:Parameters__k8s-namespace} = '<value>'
  … (one line per parameter)
- The kind cluster 'simple' (context kind-simple) does not exist — `kind get clusters` does not list it. Create it (see Local Kubernetes with kind in the tw-deploy skill), or delete the stale context: kubectl config delete-context kind-simple
```
With the four `Parameters__*` env vars set, the same context reports the kind cluster and also:
`- The container registry at localhost:5001 (registry-endpoint) does not answer. …`

Successful plan: a temporary `kind create cluster --name task286` plus a `registry:2` on
127.0.0.1:5001, env vars set, stdin redirected so it stops at the confirmation. Both were removed
afterwards and the context was restored to `kind-simple`.
```
dev deploy → aspire deploy (kubernetes)
  AppHost:     <repo>/source/container-apps/aspire/projects/aspire-app-host/aspire-app-host.csproj
  Environment: Production
  Target:      Publish:Target=kubernetes
  Parameter:   k8s-namespace=timewarp-architecture (from the Parameters__k8s-namespace environment variable)
  Parameter:   helm-release-name=timewarp-architecture (from the Parameters__helm-release-name environment variable)
  Parameter:   registry-endpoint=localhost:5001 (from the Parameters__registry-endpoint environment variable)
  Parameter:   registry-repository=timewarp-architecture (from the Parameters__registry-repository environment variable)
  kubectl context: kind-task286 (helm v4.3.0+gbec5b06)
Not deploying: no confirmation. Re-run with --yes to deploy non-interactively, or from a terminal to answer the prompt.
```
Registry stopped → only the registry refusal. Cluster node stopped →
`- The kubectl context kind-task286 does not answer (… The connection to the server 127.0.0.1:44697 was refused …)`.

### Review disposition

- **Process:** `tw-implementation-review`, effort 2, roster: general; 2 rounds.
- **Round 1:** 10 findings (0 bug, 5 suggestion, 5 nit), all fixed in 20de37319: probe timeouts
  (20s, Amuru 2.0.0-beta.2 `WithTimeout`); a failed `user-secrets list` is reported; a test checks the
  required list against the AppHost's value-less `AddParameter` calls; the combination rules moved into
  pure `Collect*Problems` functions with tests; deprovision forwards the parameters that are set,
  best-effort; the registry probe keeps Ctrl+C and reports the TLS reason; env lookup is
  case-insensitive; the real csproj path is printed; forwarded parameters must be non-secret (Design);
  the synopsis is updated.
- **Round 2:** M1–M10 re-verified as fixed, plus 2 new nits, both fixed: N1, the agreement test now
  covers `secret:` and rejects literal or named-argument forms; N2, the skill's timeout and
  user-secrets wording is corrected.
- **Final:** 0 open; bug 0, suggestion 5 fixed, nit 7 fixed; wontfix 0. **Disposition: clean.**
- **Tests after fixes:** dev-cli-tests 147/147.
- **Artifacts:** `review/review-framework.md`, `review/round-1/{general,merged}.md`,
  `review/round-2/{general,merged}.md`, `review/disposition.md`.

### How to validate

**Smoke:**
1. `cd tests/tools/dev-cli-tests && dotnet test -c Release`
2. From the repo root, with a kubectl context whose kind cluster is gone (or none configured), run
   `dotnet run tools/dev-cli/dev.cs -- deploy --target kubernetes </dev/null`
3. Optional: `kind create cluster --name t` and
   `docker run -d -p 127.0.0.1:5001:5000 --name t-reg registry:2`, then set the four parameters as
   `Parameters__*` env vars (pwsh: `${env:Parameters__k8s-namespace} = 'app'`, …;
   registry-endpoint `localhost:5001`) and re-run step 2. Clean up with `kind delete cluster --name t`
   and `docker rm -f t-reg`.

**Expect:**
1. 147/147 pass.
2. Exit 1 and one "preflight failed (N problems); nothing was run" report that lists every missing
   parameter with its `dotnet user-secrets set` and `${env:…}` command, plus the kind / reachability
   problem. `aspire` is never invoked.
3. The plan prints every `Parameter:` line and the kubectl context, then "Not deploying: no
   confirmation" (stdin redirected). Nothing deploys.

## Notes

- **Context from the 2026-10-08 diagnosis.** The `kind-simple` context pointed at a deleted cluster:
  `kind get clusters` was empty, the API at `127.0.0.1:40703` refused connections, and there was no
  `kind-registry` container. A reachability check would have caught this before Aspire ran.
- **Example kind values from the diagnosis.** These are examples, not defaults: namespace, release
  and repository `timewarp-architecture`, registry `localhost:5001` (the value the tw-deploy skill
  uses).
- **Namespace creation.** Helm installs with `--create-namespace`, so the namespace doesn't need to
  exist beforehand.
- **No `--set`.** Aspire 13.6 has no `--set name=value` (aspire issue #16091 is only a proposal).
  Forwarding `--Parameters:<name>=<value>` after `--` is the mechanism.
- **One-off workaround until this lands (pwsh):**
  `${env:Parameters__k8s-namespace} = 'timewarp-architecture'` (likewise for `helm-release-name`,
  `registry-endpoint`, `registry-repository`), then `dev deploy --target kubernetes --yes`.
- **Related.** 070-006 (manual `dev deploy` / `dev deprovision`) is done, so it isn't a dependency.
  285 (update Aspire to 13.6.1) touches the same area; nothing in 13.6.1 is known to change parameter
  resolution.