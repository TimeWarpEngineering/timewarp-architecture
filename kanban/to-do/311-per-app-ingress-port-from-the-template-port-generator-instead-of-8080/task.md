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

- [ ] `Parameters:ingress-port` = 36371 committed; AppHost reads it, no 8080 literal
- [ ] Template port generator scoped to `"ingress-port": "`; smoke asserts replacement + range
- [ ] dev CLI defaults removed; `dev open` resolves `ingress-port`; help text updated
- [ ] Tests + agreement test
- [ ] `tw-deploy` and Design regions reconciled
- [ ] Gates: `dev build` 0/0, dev-cli-tests, `dev publish compose` (published `.env` shows 36371),
      `dev publish kubernetes`, `dev template-smoke`, `ganda repo audit`

## Notes

- Workers must not touch the maintainer's kind cluster `kind-timewarp-architecture` (live
  deployment), containers or user secrets. The maintainer runs `dev open --target kubernetes` after
  merge.
- The rule from 070-006 / 284 applies: wrap tools; never reconstruct ganda's hash in code.

## Session

- Created: 2026-10-12 (cockpit, per Steve)
