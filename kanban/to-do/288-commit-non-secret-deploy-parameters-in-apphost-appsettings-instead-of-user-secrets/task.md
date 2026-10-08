# Commit non-secret deploy parameters in AppHost appsettings instead of user secrets

## Description

286 made AppHost **user secrets** the home for every deploy parameter. None of the four Kubernetes
parameters is a secret (Steve, 2026-10-08: "they are not secrets after all"). Keeping them there
hides them in `~/.microsoft/usersecrets/…`, makes every machine or teammate set them again, and
implies they are sensitive.

**Split:**
- **`k8s-namespace`, `helm-release-name`, `registry-repository`** are the app's identity. Commit
  them in a `Parameters` section of the AppHost's `appsettings.json`, set to the **app's kebab
  name**, so every generated app gets correct values out of the box. This replaces 286's "don't
  hard-code defaults": the values identify the app and are not per-deployment settings.
- **`registry-endpoint`** genuinely differs per machine or target: `localhost:5001` for kind, an
  Azure Container Registry login server for AKS. It stays per machine, set with user secrets or
  `${env:Parameters__registry-endpoint}`. Decide whether to also commit `localhost:5001` as the
  kind default in `appsettings.json` and override it for AKS, then record the choice. Recommended:
  commit it. Then the kind recipe needs no configuration, and AKS overrides it per machine.
- **Real secrets** (`postgres-password`, Entra client secret) stay exactly where they are.

## Requirements

- **Template rename (must verify).** `sourceName` is `TimeWarp.Architecture`. The template engine's
  automatic forms do **not** include the kebab form `timewarp-architecture`, so a literal written
  into `appsettings.json` would ship unchanged to every generated app.
  - Add a derived template symbol, for example `appNameKebab` derived from `name`: lower-cased,
    with `.` and spaces turned into `-`.
  - It replaces a **dedicated placeholder token**, not the bare string `timewarp-architecture`.
    That string also appears in repository URLs and analyzer project names, which must not be
    rewritten.
  - Prove it with `dev template-smoke`: a generated app named, say, `Contoso.Shop` gets
    `contoso-shop` for all three values. Add an assertion to the smoke harness.
  - Kubernetes naming rules: the value must be a valid DNS-1123 label (≤ 63 characters, `[a-z0-9-]`,
    starting and ending alphanumeric). The derivation or a guard must ensure that. Fail template
    generation or smoke loudly if it can't.
- **dev CLI** (`services/aspire-deploy.cs`):
  - Resolution order is: environment variable, then user secret, then AppHost
    `appsettings.json`. That is the order Aspire itself applies, so what `dev deploy` prints
    matches what Aspire uses.
  - The plan's "from …" source label must name `appsettings.json` when that is the source.
  - The missing-parameter report should only ever list `registry-endpoint` (or nothing, if it is
    committed).
  - Read `appsettings.json` as plain JSON config, using
    `Microsoft.Extensions.Configuration` or a JSON read of the `Parameters` section. Do not
    reimplement Aspire.
- **Tests:**
  - resolution order and source labels;
  - the agreement test from 286 (required list vs the AppHost's value-less `AddParameter` calls)
    stays green or is updated;
  - the template-smoke assertion above.
- **Docs:**
  - `skills/tw-deploy`: rewrite the secrets and parameters section and the kind recipe. Remove the
    three `dotnet user-secrets set` lines. Use pwsh syntax, and coordinate with 287 if it has
    already merged.
  - Reconcile the Design regions in `aspire-deploy.cs`, `program.cs` and `constants.cs`.
- **Maintainer's machine.** After merge, the user secrets set on 2026-10-08 for these three values
  still win over `appsettings.json`, by precedence. Print, in the PR test plan, the pwsh commands
  that remove them: `dotnet user-secrets remove 'Parameters:k8s-namespace' --project …`, and the
  same for the other two. Do not run them.
- **Boyscout, check only.** The AppHost's `appsettings.json` carries a `ReverseProxy` section, with
  routes and clusters pointing at `https://api` / `https://web` / `https://grpc`. The AppHost
  configures YARP in code, so this section looks dead. Confirm whether anything reads it. If
  nothing does, delete it in this PR and say so.

## Checklist

- [x] Derived kebab template symbol + placeholder; smoke proves `Contoso.Shop` → `contoso-shop`
- [x] AppHost `appsettings.json` `Parameters` section (3 identity values; registry-endpoint decision)
- [x] dev CLI resolution env → user secret → appsettings, with source labels
- [x] Tests + agreement test
- [x] `tw-deploy` and Design regions reconciled; dead `ReverseProxy` section triaged
- [x] Gates: `dev build` 0/0, dev-cli-tests, `dev publish kubernetes`, `dev template-smoke`,
      `ganda repo audit`

## Notes

- Workers must not touch the maintainer's user secrets, the kind cluster
  `kind-timewarp-architecture` (live deployment) or any container.

## Results

- **Template symbol.** `.template.config/template.json` adds the derived symbol `appNameKebab`
  (`valueSource: name`, `valueTransform: dnsLabel`). `dnsLabel` is a chain of custom forms:
  lower-case, `[^a-z0-9]+` → `-`, trim `-` from both ends, truncate to 63 characters, trim again.
  So the result is always a DNS-1123 label when one exists (`Contoso.Shop` → `contoso-shop`,
  `_My..App_Name.` → `my-app-name`).
  - **Placeholder decision:** the token is `timewarp-architecture`, scoped by `onlyIf` to text that
    directly follows `"k8s-namespace": "`, `"helm-release-name": "` and `"registry-repository": "`.
    The scoping makes it a dedicated token: repository URLs and analyzer project names that contain
    the same string are never rewritten, and smoke's package-id and build tiers confirm that. A
    separate placeholder string was rejected for one reason: the monorepo's own committed values
    must stay `timewarp-architecture`. That is the live kind deployment's namespace, release and
    repository, so after the leftover user secrets are removed, a redeploy targets the same release
    instead of creating a second one.
  - The template engine cannot fail generation on its own. The loud failure is the smoke assert
    `AssertDeployParametersUseAppKebabName`: each value must equal the expected kebab name AND be a
    DNS-1123 label (`^[a-z0-9]([-a-z0-9]{0,61}[a-z0-9])?$`).
- **AppHost `appsettings.json`.** New `Parameters` section: `k8s-namespace`, `helm-release-name`
  and `registry-repository` are set to `timewarp-architecture` (each generated app gets its own
  kebab name).
  - **registry-endpoint decision:** committed as `localhost:5001`, the kind recipe's registry, so
    the kind recipe needs no configuration. AKS overrides it per machine with a user secret or
    `${env:Parameters__registry-endpoint}`. With all four committed, the missing-parameter report
    is empty on a fresh checkout; it only appears if someone blanks a value.
  - `program.cs` still calls `AddParameter(name)` with no value. Aspire reads `Parameters:<name>`
    from configuration, so the published Helm chart is unchanged (checked: no committed value
    appears in `artifacts/aspire-output/kubernetes`).
- **ReverseProxy triage:** the AppHost `appsettings.json` `ReverseProxy` section was dead and is
  deleted.
  - Nothing in `aspire-app-host` reads it: its only configuration reads are `Publish:Target`,
    `Authentication:UseMock`, `Postgres:UseDataVolume` and `Ingress:*`. YARP is configured in code
    (`WithConfiguration`), and the yarp project reads its own `appsettings.Development.json`.
  - Its cluster ids and addresses (`Api.Server`, `https://api`) predate the `ServiceNames`
    resource names.
- **dev CLI** (`services/aspire-deploy.cs`, `aspire-deploy-preflight.cs`).
  `ResolveParameters` follows Aspire's configuration order:
  1. the `Parameters__<name>` environment variable;
  2. the AppHost user secret;
  3. `appsettings.Production.json` (`DeployEnvironment` is Production; the template ships none);
  4. `appsettings.json`.

  Details:
  - The appsettings files are read as plain JSON (`System.Text.Json` over the `Parameters`
    section, keys case-insensitive, last duplicate wins, comments and trailing commas allowed),
    not by reimplementing Aspire.
  - Source labels are `AppHost appsettings.json` / `AppHost appsettings.Production.json`, so the
    plan prints `(from the AppHost appsettings.json)`.
  - The missing-parameter refusal now points at the committed `Parameters` section first, then at
    user secrets or env vars.
  - The Design regions in `aspire-deploy.cs`, `aspire-deploy-preflight.cs`, `deploy-command.cs`,
    `program.cs` and `constants.cs` are reconciled.
- **Tests** (`tests/tools/dev-cli-tests/aspire-deploy-tests.cs`, 152/152):
  - appsettings-only resolution and its label;
  - full order env → secret → `appsettings.Production.json` → `appsettings.json`;
  - JSON parsing edge cases (case, duplicates, comments, blank, absent, invalid);
  - a blank committed value is still reported missing;
  - a new agreement test: the real AppHost `appsettings.json` resolves every kubernetes required
    parameter;
  - the 286 value-less agreement test is unchanged and green.

  Template smoke: every matrix app asserts its kebab name (`smokedefault`, …), plus a
  generate-only `Contoso.Shop` → `contoso-shop` step.
- **Docs:** `skills/tw-deploy` rewrites the deploy-configuration bullet and the Secrets and
  parameters section (committed `Parameters` JSON example, pwsh override for an ACR
  `registry-endpoint`, precedence, `user-secrets remove` for leftovers). The kind recipe loses its
  four `dotnet user-secrets set` lines; step 5 is now just `dev deploy`. Task 287 has not merged,
  so there was nothing to coordinate.
- **Gates:**
  - `dev build` 0 warnings / 0 errors;
  - dev-cli-tests 152/152;
  - aspire-tests `KubernetesPublish` 9/9;
  - `dev publish kubernetes` production-safe (helm lint clean);
  - `dev template-smoke` SUCCEEDED (Contoso.Shop → contoso-shop, plus all three matrix apps);
  - `ganda repo audit` passes.

### Maintainer cleanup (do not run from a worker)

The user secrets set on 2026-10-08 for the three identity values still beat `appsettings.json`.
Remove them (pwsh, from the repo root):

```powershell
dotnet user-secrets remove 'Parameters:k8s-namespace' --project source/container-apps/aspire/projects/aspire-app-host/aspire-app-host.csproj
dotnet user-secrets remove 'Parameters:helm-release-name' --project source/container-apps/aspire/projects/aspire-app-host/aspire-app-host.csproj
dotnet user-secrets remove 'Parameters:registry-repository' --project source/container-apps/aspire/projects/aspire-app-host/aspire-app-host.csproj
# Optional: registry-endpoint is committed as localhost:5001 too
dotnet user-secrets remove 'Parameters:registry-endpoint' --project source/container-apps/aspire/projects/aspire-app-host/aspire-app-host.csproj
```

### How to validate

**Smoke:**

```powershell
cd tests/tools/dev-cli-tests; dotnet test -c Release; cd -
dotnet run tools/dev-cli/dev.cs -- template-smoke
# after the maintainer cleanup above, against the live kind context (read-only plan, answer "no"):
dev deploy --target kubernetes
```

**Expect:**
- dev-cli-tests: 152 passed, 0 failed.
- template-smoke: the log shows `AppHost deploy parameters use the app's kebab name (contoso-shop).`
  for the generate-only `Contoso.Shop` step, and the same line with `smokedefault` /
  `smokenopostgres` / `smokenoapi` for the matrix, then `Template smoke SUCCEEDED`.
- `dev deploy --target kubernetes` prints
  `Parameter:   k8s-namespace=timewarp-architecture (from the AppHost appsettings.json)` (likewise
  helm-release-name, registry-repository and registry-endpoint=localhost:5001) and reports no
  missing deploy parameter.

- **Review disposition:** `accepted-exceptions` — 1 round, effort 2, roster `general`
  (Claude subagent). Final counts: bug 0; suggestion 1 wontfix (M1 — `onlyIf`-scoped bare-name
  replacement kept so the monorepo's committed values stay deployable; smoke guards drift);
  nit 1 fixed (M2 — AppHost program.cs comment re-wrapped, names all committed parameters).
  Artifacts: `review/review-framework.md`, `review/round-1/merged.md`, `review/disposition.md`.

## Session

- Created: 2026-10-08 (cockpit, per Steve)
- 2026-10-08 implementer (ganda task work): implemented per Results; every gate green
  (`dev build` 0/0, dev-cli-tests 152/152, `dev publish kubernetes`, `dev template-smoke`, `ganda repo audit`).
- 2026-10-08 review oracle (Claude Opus 5.5, general reviewer subagent ae1c94e3f85ac4543):
  round 1, disposition accepted-exceptions (0 open).
