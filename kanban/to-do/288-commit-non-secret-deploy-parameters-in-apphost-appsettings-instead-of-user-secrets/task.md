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

- [ ] Derived kebab template symbol + placeholder; smoke proves `Contoso.Shop` → `contoso-shop`
- [ ] AppHost `appsettings.json` `Parameters` section (3 identity values; registry-endpoint decision)
- [ ] dev CLI resolution env → user secret → appsettings, with source labels
- [ ] Tests + agreement test
- [ ] `tw-deploy` and Design regions reconciled; dead `ReverseProxy` section triaged
- [ ] Gates: `dev build` 0/0, dev-cli-tests, `dev publish kubernetes`, `dev template-smoke`,
      `ganda repo audit`

## Notes

- Workers must not touch the maintainer's user secrets, the kind cluster
  `kind-timewarp-architecture` (live deployment) or any container.

## Session

- Created: 2026-10-08 (cockpit, per Steve)
