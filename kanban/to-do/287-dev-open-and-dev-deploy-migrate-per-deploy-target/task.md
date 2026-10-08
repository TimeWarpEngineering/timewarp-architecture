# `dev open` and `dev deploy migrate` per deploy target

## Description

After `dev deploy`, two manual steps remain, both copy-pasted from `skills/tw-deploy`:

1. **Open the deployed app.** For example, on kind:
   `kubectl port-forward -n ingress-nginx service/ingress-nginx-controller 8080:80`, then open a
   browser.
2. **Apply database migrations.** You pipe `efmigrations/web-migrations.sql` into `psql` through
   `docker compose exec` or `kubectl exec`, or run the bundle against ACA through a temporary
   firewall rule.

Wrap both in the dev CLI, keyed on the same `--target compose|kubernetes|aca` switch as
`dev deploy` / `dev deprovision`. Both are operator-run and never CI-automated. The existing
`NeverAutomated_Given_` guard must also cover `dev deploy migrate`.

The verb is **`dev open`**, not `dev run`, because `dev run` already means "start the AppHost
locally" (Steve, 2026-10-08).

## Rules (from 070-006 / 284 — read before designing)

- **Wrap the tool and react to its exit code.** Never reconstruct a tool's private state or
  internals (no decompiled Aspire layouts, no guessed record paths). Values come from the deploy
  configuration (`Parameters:*` resolved exactly as `dev deploy` resolves them in
  `services/aspire-deploy.cs`), the published output under `artifacts/aspire-output/<target>`, or a
  documented CLI call (`kubectl`, `az`, `helm`).
- Show operator-facing commands in **pwsh syntax**.
- Use a real TTY (`TtyPassthroughAsync`) wherever a child process might prompt or stream.
- Never hard-code `docker`. Compose honours `ASPIRE_CONTAINER_RUNTIME`.

## Requirements

### `dev open [--target compose|kubernetes|aca] [--port <n>] [--no-browser]`

- **kubernetes:**
  - Preflight is reused from `dev deploy`: the context answers, and a kind cluster exists.
  - Run `kubectl port-forward -n <controller-namespace> service/<controller-service> <port>:80` in
    the foreground, open the browser at `http://localhost:<port>`, and stop the forward on Ctrl+C.
  - The controller namespace and service default to the `tw-deploy` kind recipe (`ingress-nginx`,
    `ingress-nginx-controller`) and can be overridden by options.
  - If the controller service has an external address (LoadBalancer), print and open that instead
    of forwarding.
  - Default port 8080; if it is taken, say so and suggest `--port`.
- **compose:** open `http://localhost:<INGRESS_PORT>`, where the port comes from the published `.env`
  or the `ingress-port` parameter. No forwarding.
- **aca:** open the ingress container app's FQDN, via `az containerapp show … --query
  properties.configuration.ingress.fqdn`, using the resource group / subscription resolution
  `dev deploy` already does.
- Browser opening is cross-platform, including WSL (`wslview` / `explorer.exe` when present). If
  nothing can open a browser, print the URL instead.

### `dev deploy migrate [--target …] [--yes]`

- It applies the published idempotent migrations for the target and is safe to re-run.
- If the published output is missing, run the target's `dev publish <target>` first, or refuse with
  the exact command. Decide which, and document the choice.
- **compose:** `<runtime> compose exec -T postgres psql …` fed `efmigrations/web-migrations.sql`.
- **kubernetes:** `kubectl exec -i -n <k8s-namespace> statefulset/postgres-statefulset -- psql …`,
  fed the same script.
- **aca:** run the published bundle with `--connection` through a temporary
  `operator-migrate` firewall rule for the operator's IP. The rule is **always deleted** afterwards,
  including on failure (`try`/`finally`). Read the server and credentials via `az`, as the skill
  describes. Never print the password.
- Prompt for confirmation unless `--yes` is passed. Exit with the underlying tool's exit code.
- Print a single summary line saying what ran against which target.

### Tests (`tests/tools/dev-cli-tests`)

Cover:
- argument and option parsing;
- command construction per target (port-forward, exec + script, bundle + firewall rule);
- the firewall-rule cleanup on failure;
- the LoadBalancer-vs-forward decision;
- refusal texts;
- `NeverAutomated_Given_` extended to `deploy migrate`.

Tests never touch a real cluster, Docker or Azure.

### Docs

- In `skills/tw-deploy`, replace the copy-paste open and migrate commands with `dev open` /
  `dev deploy migrate`, keeping the raw commands as "what it runs" for generated apps without the
  dev CLI.
- While in that file, give the "Local Kubernetes with kind" recipe pwsh syntax next to the bash.
  Steve uses pwsh, and 286 only converted the preflight output.
- Point the AppHost Design region at the new verbs where it describes the operator flow.

## Checklist

- [x] `dev open` (kubernetes forward/LB, compose, aca) + browser opening incl. WSL
- [x] `dev deploy migrate` (compose, kubernetes, aca with guaranteed firewall cleanup)
- [x] Tests + NeverAutomated guard extended
- [x] `tw-deploy` updated (new verbs, kind recipe in pwsh)
- [x] Gates: `dev build` 0/0, dev-cli-tests, `ganda repo audit`, `dev template-smoke`

## Notes

- **Workers must not create clusters, containers or Azure resources**, and must not touch the
  maintainer's kind cluster `kind-timewarp-architecture`, which has a live deployment. Proof comes
  from tests plus `--help` and refusal output. The maintainer runs the real `dev open` /
  `dev deploy migrate` against kind after merge.
- **Live state on 2026-10-08:** the app is deployed to kind (`timewarp-architecture` namespace) and
  the database has **not** been migrated yet.

## Results

- **`dev open`** (`tools/dev-cli/endpoints/open-command.cs`): kubernetes reads the ingress
  controller Service (`ingress-nginx` / `ingress-nginx-controller`, overridable with
  `--controller-namespace` / `--controller-service`). With a LoadBalancer address it opens that;
  otherwise it checks the port is free (default 8080, `--port`), runs `kubectl --context <ctx>
  port-forward` on a real TTY, opens the browser once the port listens, and Ctrl+C stops the forward.
  Compose takes the port from `--port`, then `INGRESS_PORT` in the published `.env`, then the
  `ingress-port` parameter, then the AppHost default 8080. Aca opens `https://<fqdn>` from
  `az containerapp show --name ingress`. The browser opener is explorer.exe / open / wslview →
  explorer.exe → xdg-open, and falls back to printing the URL (also `--no-browser`).
- **`dev deploy migrate`** (`endpoints/deploy-migrate-command.cs`, under the new `DeployGroup`;
  `dev deploy` itself still routes to DeployCommand, checked by hand). **Decision:** if the
  published output is missing it *refuses* with the exact `dev publish <target>` command and never
  publishes itself (reason recorded in the `services/deploy-operate.cs` Design region). Per target:
  - compose: `<runtime> compose ls --format json` (runtime = `ASPIRE_CONTAINER_RUNTIME`) picks the
    running project. Order: `--project-name`, then the project from this checkout, then the only one.
    It then runs `compose --project-name … --file … exec -T postgres sh -c 'PGPASSWORD=… psql …'`
    with the script on stdin.
  - kubernetes: `kubectl --context <ctx> exec -i --namespace <k8s-namespace>
    statefulset/postgres-statefulset -- sh -c '…psql…'`.
  - aca: the resource group comes from `--resource-group`, then `Azure__ResourceGroup`, then the user
    secret `Azure:ResourceGroup`. It needs exactly one Flexible Server, and reads the connection
    string from the `postgres-kv` Key Vault secret. That value is never printed; without the role it
    refuses with the pwsh grant command. The IP comes from `--client-ip`, else api.ipify.org.
    `RunAcaBundleAsync` creates `operator-migrate` inside `try`, runs the bundle on a TTY, and deletes
    the rule in `finally` with an uncancellable token. A failed delete prints the delete command.
  - A confirmation is required unless `--yes` (redirected stdin is refused). One summary line; exits
    with the tool's exit code.
- **Shared preflight** gained a `PreflightScope` (Deploy / Deprovision / Open / Migrate). Open and
  migrate skip the aspire, helm and registry checks and keep the context/kind and az-login checks;
  `DeployPreflight` now carries `KubectlContext` / `AzureSubscriptionId`.
- **Tests:** `tests/tools/dev-cli-tests/deploy-operate-tests.cs` covers options/port parsing, scopes,
  each target's commands, LoadBalancer vs forward, compose project choice, the firewall cleanup on a
  failed create, a failed bundle, an exception, cancellation and a failed delete, the refusal texts,
  the browser order and WSL detection, and agreement with AppHost `constants.cs` and the ingress-port
  default. `NeverAutomated_Given_` now also covers `dev deploy migrate` and `dev open`.
- **Docs:** `skills/tw-deploy` adds the new verbs, keeps the raw commands as "what it runs", and
  shows the kind recipe in pwsh next to bash. The AppHost Design region points at the new verbs.
  The `dev deploy` success line names the next steps.
- **Gates (2026-10-09):** `./bin/dev build` 0 warnings / 0 errors; dev.cs runfile build clean;
  dev-cli-tests 192/192; `ganda repo audit` passes; `dev template-smoke` SUCCEEDED; `dev check-version`
  is new (no bump needed). No cluster, container or Azure resource was touched. The only aspire
  runs were file-only `aspire publish` runs to `/tmp`, used to confirm the output layout (since
  removed).

### How to validate

**Smoke**

```pwsh
cd tests/tools/dev-cli-tests; dotnet test -c Release; cd ../../..
dotnet run tools/dev-cli/dev.cs -- deploy migrate --help
dotnet run tools/dev-cli/dev.cs -- open --help
dotnet run tools/dev-cli/dev.cs -- deploy migrate   # compose, without artifacts/aspire-output/compose
dotnet run tools/dev-cli/dev.cs -- open --port abc --no-browser
# Maintainer, against the live kind deployment after merge:
dev publish kubernetes; dev deploy migrate --target kubernetes; dev open --target kubernetes
```

**Expect**

- dev-cli-tests: `total: 192 … failed: 0`.
- Both `--help` outputs list the options above (`--project-name`, `--resource-group`, `--client-ip`;
  `--port`, `--no-browser`, `--controller-namespace`, `--controller-service`).
- Migrate without published output refuses with `No published migrations for compose:
  …/artifacts/aspire-output/compose/efmigrations/web-migrations.sql does not exist … dev publish
  compose`, exit 1 (kubernetes and aca give the same refusal after their preflight).
- `--port abc`: `--port 'abc' is not a TCP port (1-65535).`, exit 1.
- On kind: migrate prints the plan, asks `Migrate? [y/N]`, psql output, then `dev deploy migrate:
  applied web-migrations.sql with psql to kubernetes (context kind-…, namespace timewarp-architecture).`;
  open forwards `localhost:8080`, the browser shows the app, and Ctrl+C ends the forward.

## Session

- Created: 2026-10-08 (cockpit, per Steve)
- 2026-10-09: implemented under `ganda task work` (implement oracle); gates above.
