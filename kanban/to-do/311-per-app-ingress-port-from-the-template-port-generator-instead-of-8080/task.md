# Per-app ingress port from the template port generator instead of 8080

## Description

On 2026-10-12, `dev open --target kubernetes` refused with "Port 8080 on localhost is already in
use". 8080 is a shared default that many tools grab. It is hard-coded in two places that must
agree:

- the AppHost publish parameter: `program.cs` `AddParameter(IngressPortParameterName, "8080",
  publishValueAsDefault: true)`, which is the Compose host port;
- the dev CLI: `services/deploy-operate.cs` `DefaultForwardPort = 8080` and
  `DefaultIngressPort = "8080"`, which are the `dev open` defaults for kubernetes and compose.

Today a comment and a test keep them in step. They should come from one committed value, and each
generated app should get its **own** port.

## Decision (Steve, 2026-10-12)

- **This repo's port is 36371.** That is `ganda generate-port timewarp-architecture`, a
  deterministic hash of the repo name.
- **Generated apps get a port chosen at template generation**, using the `dotnet new` built-in port
  generator (`"type": "generated", "generator": "port"`, constrained to a sensible high range such
  as `low` 20000 / `high` 60000). ganda is private and never ships, so neither the template nor the
  dev CLI may call it.

## Requirements

- **One committed value.** Add `"ingress-port": "36371"` to the `Parameters` section of the AppHost's
  `appsettings.json`, next to the 288 values.
  - `program.cs` declares `ingress-port` the same way 288 declares its parameters: no code literal,
    value from configuration. Keep the publish-time behaviour, so the published `.env` still
    carries the port as the default. If `publishValueAsDefault` needs a value at declaration time,
    read it from configuration rather than a literal, and record the reasoning in the Design
    region.
- **Template.**
  - Add a generated port symbol in `.template.config/template.json` that replaces `36371` **only**
    after `"ingress-port": "` (an `onlyIf` scope, the same technique as `appNameKebab`).
  - Template-smoke asserts that a generated app's `ingress-port` is an integer in range and is not
    `36371`, and that a second generation gets its own value. If the generator is deterministic per
    run, assert only the range and "replaced".
- **dev CLI.** Remove `DefaultForwardPort` / `DefaultIngressPort` 8080.
  - `dev open` (kubernetes forward port and compose fallback) resolves `ingress-port` through the
    existing parameter resolution: env, then user secret, then `appsettings.Production.json`, then
    `appsettings.json`.
  - `--port` still overrides.
  - The help text and route examples stop saying 8080; say "the ingress-port parameter (36371 in
    this repo)" or similar.
  - The kubernetes port-forward uses the same port. One number per app, everywhere.
- **Leave alone.** The AppHost **run-mode** `Ingress:Port` / `Ingress:HttpPort` pins (local
  `dev run`) are a separate concern. Note them in the PR if they also sit on a common port, but do
  not change them here.
- **Tests:** dev CLI resolution of `ingress-port` from `appsettings.json`, `--port` override, and the
  agreement between the committed value and the AppHost declaration (replacing the old
  8080-agreement test).
- **Docs:**
  - `skills/tw-deploy` (and its `references/`): replace 8080 with "the app's `ingress-port`" and
    say how it is chosen;
  - Design regions in `program.cs`, `constants.cs`, `deploy-operate.cs`, `open-command.cs`.

## Checklist

- [x] `Parameters:ingress-port` = 36371 committed; AppHost reads it, no 8080 literal
- [x] Template port generator scoped to `"ingress-port": "`; smoke asserts replacement + range
- [x] dev CLI defaults removed; `dev open` resolves `ingress-port`; help text updated
- [x] Tests + agreement test
- [x] `tw-deploy` and Design regions reconciled
- [x] Gates (`.env` note under Results): `dev build` 0/0, dev-cli-tests, `dev publish compose` (published `.env` shows 36371),
      `dev publish kubernetes`, `dev template-smoke`, `ganda repo audit`

## Notes

- Workers must not touch the maintainer's kind cluster `kind-timewarp-architecture` (live
  deployment), containers or user secrets. The maintainer runs `dev open --target kubernetes` after
  merge.
- The rule from 070-006 / 284 applies: wrap tools; never reconstruct ganda's hash in code.

## Session

- Created: 2026-10-12 (cockpit, per Steve)
- 2026-10-12 implement oracle (ganda task work, headless Claude): implemented and gated; see Results.

## Results

- **AppHost:** `appsettings.json` commits `Parameters:ingress-port` = `36371`. `program.cs` reads
  `builder.Configuration["Parameters:ingress-port"]` and passes it to `AddParameter(...,
  publishValueAsDefault: true)`, so there is no literal. That overload needs the value when the
  parameter is declared, and the Design region records why. A missing value fails publish instead
  of falling back to a shared port. `constants.cs` has a note next to `IngressPortParameterName`.
- **Template:** `.template.config/template.json` adds an `ingressPort` symbol (`generated` / `port`,
  low 20000, high 60000). It replaces `36371` only after `"ingress-port": "` (onlyIf).
- **Template-smoke:** `TemplateSmokeHarness.ReadGeneratedIngressPort` checks every generated app:
  the port is an integer inside the symbol's range, and it is not the `replaces` value. It reads
  both from `template.json`, so they are not copied into the harness. The generator is random, so
  the command collects every generation's port and fails only when all of them are the same.
- **dev CLI:** removed `DefaultForwardPort` and `DefaultIngressPort`.
  - `dev open --target kubernetes` uses `--port`, else `ingress-port` resolved like `dev deploy`'s
    parameters (env, user secret, `appsettings.Production.json`, `appsettings.json`), through
    `DeployOperate.ChooseForwardPort`. The port-forward uses that same number.
  - Compose uses `--port`, else `INGRESS_PORT` in the .env, else the parameter.
  - If nothing resolves, both targets refuse with `NoIngressPortMessage`, which names the
    appsettings key, the user-secrets command and `--port`.
  - `ParsePort` now names where the value came from (`--port` or the parameter) in its error.
  - Help text and route examples say "the AppHost ingress-port parameter, 36371 in this repo".
- **Tests:** dev-cli-tests: 211/211.
  - New: forward-port precedence (option, then parameter); refusal with no port; a bad parameter
    value; resolving `ingress-port` from `appsettings.json`, with env winning over it.
  - The old 8080 agreement test is replaced: the committed value must be a valid port, `program.cs`
    must read it from configuration, and `AddParameter(IngressPortParameterName, "…"` must not
    appear.
  - aspire-tests: new `PublishedIngressPort_Should_DefaultToTheCommittedParameter`. The publish
    model's `ingress-port` default must equal the committed `Parameters:ingress-port`.
- **Docs:** `skills/tw-deploy/SKILL.md` and `references/local-kubernetes-with-kind.md` now say
  `<ingress-port>` instead of 8080, explain how the port is chosen, and show it in the committed
  Parameters example.
- **`.env` checklist note:** `aspire publish` leaves every `.env` value blank, `INGRESS_PORT=`
  included, and it did with 8080 too (the `Env` fixture in deploy-operate-tests assumes it).
  `aspire deploy` and `aspire do prepare-compose` fill it from the parameter default. So the gate
  checks that default in the model (the aspire-test above) rather than in the published file.
- **Run-mode pins (left alone):** `Ingress:Port` / `Ingress:HttpPort` are 63610 / 63620 in
  `appsettings.Development.json`. Those are not common ports, so no follow-up.
- **Gates run:**
  - `dev build`: 0/0.
  - dev-cli-tests: 211/211.
  - aspire-tests `ComposePublish_Given_`: 8/8.
  - `dev publish compose`: production-safe (7/7).
  - `dev publish kubernetes`: production-safe, helm lint 0 failed.
  - `dev template-smoke`: SUCCEEDED. Generated ports were 40515, 45409, 35380 and 26324.
  - `dev check-version`: new version, OK.
  - `ganda repo audit`: passes.
- The kind cluster, containers and user secrets were not touched.

### How to validate

**Smoke**

```bash
grep -n '"ingress-port"' source/container-apps/aspire/projects/aspire-app-host/appsettings.json
(cd tests/tools/dev-cli-tests && dotnet run)
dev self-install && dev open --help
dev template-smoke
dev open --target kubernetes   # maintainer, after merge, against kind-timewarp-architecture
```

**Expect**

- `appsettings.json` shows `"ingress-port": "36371"`.
- dev-cli-tests pass, including `IngressPort_Should_BeTheCommittedParameterNotACodeLiteral` and
  the `ForwardPort_*` facts.
- `dev open --help` shows no 8080. The `--port` option and the kubernetes example name the
  ingress-port parameter (36371 in this repo).
- `dev template-smoke` prints `AppHost ingress-port is the generated <n> (20000-60000).` once per
  generated app, then `Generated ingress-ports differ per app: …`, then `Template smoke
  SUCCEEDED`.
- `dev open --target kubernetes` prints `Ingress port: 36371 (ingress-port from the AppHost
  appsettings.json)` and forwards `localhost:36371` to port 80 of the controller.
