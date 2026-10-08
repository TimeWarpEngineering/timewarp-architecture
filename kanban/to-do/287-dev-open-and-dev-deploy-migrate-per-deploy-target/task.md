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

- [ ] `dev open` (kubernetes forward/LB, compose, aca) + browser opening incl. WSL
- [ ] `dev deploy migrate` (compose, kubernetes, aca with guaranteed firewall cleanup)
- [ ] Tests + NeverAutomated guard extended
- [ ] `tw-deploy` updated (new verbs, kind recipe in pwsh)
- [ ] Gates: `dev build` 0/0, dev-cli-tests, `ganda repo audit`, `dev template-smoke`

## Notes

- **Workers must not create clusters, containers or Azure resources**, and must not touch the
  maintainer's kind cluster `kind-timewarp-architecture`, which has a live deployment. Proof comes
  from tests plus `--help` and refusal output. The maintainer runs the real `dev open` /
  `dev deploy migrate` against kind after merge.
- **Live state on 2026-10-08:** the app is deployed to kind (`timewarp-architecture` namespace) and
  the database has **not** been migrated yet.

## Session

- Created: 2026-10-08 (cockpit, per Steve)
