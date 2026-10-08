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

- [ ] Decide and document where the deploy config lives (user secrets, git-ignored file, or
      `dev deploy config` command)
- [ ] Read the config and forward it as `--Parameters:<name>=<value>` after `--`
- [ ] Preflight: required parameters resolved (missing list + exact pwsh/`dev` command to set each)
- [ ] Preflight: kubectl current context API reachable
- [ ] Preflight: kind cluster exists and local registry container running (kind contexts)
- [ ] `PassthroughAsync` → `TtyPassthroughAsync`; comment at `deploy-command.cs:20-24` corrected
- [ ] Tests in `aspire-deploy-tests.cs` (config, forwarding, each refusal); NeverAutomated guard
      intact
- [ ] `skills/tw-deploy/SKILL.md` deploy section updated
- [ ] Proof in PR: test output + preflight refusal text + successful plan

## Session

- Created: 3130576 (2026-10-08)

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