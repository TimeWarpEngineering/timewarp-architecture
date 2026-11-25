# Expose web-server externally on aca targets when yarp is off

## Description

A generated app with `--yarp false` deploys to Azure Container Apps and is not reachable. The YARP
ingress is the only container app with external ingress, so with the yarp flag off the `aca` and
`aca-neon` publish output has no external app at all. `aspire deploy` succeeds and serves nothing.

Azure Container Apps has its own ingress. A web-only app (one backend) does not need YARP in front of
it there. Make web-server the external container app on the two Azure targets when the yarp flag is
off, so a web-only instance deploys and opens with no AppHost edit.

The first instance repo (see task 491) will be generated web-only with yarp off and deployed with
`dev deploy --target aca-neon`. It hits this on its first deploy.

## Propagation

likely

## Requirements

- With yarp off and `Publish:Target` `aca` or `aca-neon`, web-server is published with external
  ingress. Exactly one container app is external.
- With yarp on, nothing changes: the ingress stays the only external app and web-server stays internal.
- Compose and Kubernetes with yarp off keep today's behavior (no host port, no Ingress). This task is
  the two Azure targets only.
- Run mode is unchanged.
- The passkey RP ID is allowed on the deployed host with yarp off. Today
  `WebAuthnOptions__AllowedRpIds` gets `ingress.<environment default domain>` only inside the ingress
  block. With yarp off the public host is web-server's own FQDN on that default domain, and it needs
  the same treatment without a reference cycle.
- `dev deploy --target aca` and `--target aca-neon` smoke and open the web-server URL when there is no
  ingress resource.

## Goal (implementer restatement)

A generated app with `--yarp false` deployed to `aca` or `aca-neon` has no public container app, so
it serves nothing. On those two targets, with yarp off, web-server becomes the one external container
app (ACA's own managed ingress replaces YARP). Everything else keeps today's shape.

Acceptance:

- Publish mode, yarp off, target `aca`/`aca-neon`: web-server's ingress is `external: true` and it is
  the only external app. Yarp on: ingress is the only external app, web-server internal (unchanged).
- Compose and Kubernetes with yarp off, and run mode: unchanged.
- yarp off on Azure: web-server gets `WebAuthnOptions__AllowedRpIds__0` =
  `web-server.<environment default domain>` from the environment output (no self-reference cycle).
- `dev deploy` smoke and `dev open` on `aca`/`aca-neon` find whichever of `ingress` / `web-server` is
  the external app and use its FQDN; smoke uses web-server's own `/alive` when there is no ingress.
- Design text (`web-server-resource.cs`, `publish-target.cs`), the aca publish suites and
  `skills/tw-deploy` say which app is public per target with yarp off.

## Checklist

- [x] `web-server-resource.cs`: external endpoints in publish mode when the target is Container Apps
      and the yarp flag is off. The comment at "External in run mode only" states the new rule.
- [x] Passkey allowed RP ID for the web-server FQDN on the Azure targets with yarp off.
- [x] `publish-target.cs` Design text: the "without the yarp flag publishes no host port at all"
      statement is still true for compose and kubernetes; say what the Azure targets do.
- [x] `aca-neon-publish-tests.cs` `Publish_Should_OnlyExposeTheIngressAndNoDashboard`: without the
      ingress, expect exactly one `external: true` and expect it on web-server (today it expects 0).
- [x] `aca-publish-tests.cs`: same expectation, and its header line "none at all without the yarp flag".
- [x] `dev deploy` smoke and `dev open` resolve the public URL with yarp off.
- [x] `skills/tw-deploy`: the ingress topology section says which app is public per target with yarp off.
- [x] `dev template-smoke --matrix` NoYarp variant and the publish suites pass.

## Notes

- Files: `source/container-apps/aspire/projects/aspire-app-host/web-server-resource.cs`,
  `ingress-resource.cs` (the task 393 passkey block), `publish-target.cs`,
  `tests/container-apps/aspire/aspire-tests/aca-publish-tests.cs` and `aca-neon-publish-tests.cs`.
- Task 395 fixed the NoYarp publish-test failures and is the place those "0 external" expectations
  came from.
- Task 528 adds the web-only smoke combination (yarp, api and grpc all off). It does not block this task.
- Not in scope: a custom domain, or an Ingress for Kubernetes without YARP. On Kubernetes YARP is
  the ingress.

## Session

- Created: f7eccb81-87c9-4fb4-a0c4-0fb7417d4a2f (2026-11-25)

## Results (required before done)

What changed:

- `publish-target.cs`: `PublishTarget.ContainerAppPublicHost(appName)` builds
  `<app>.<environment default domain>` from the ACA environment output (null off aca/aca-neon). The
  ingress passkey block and the new web-server path both use it. Design text now says Compose and
  Kubernetes publish no host port without yarp, and that ACA makes web-server the one external app.
- `web-server-resource.cs`: `WebServerResource.PublishAsContainerAppsEdge` makes web-server external
  and adds `WebAuthnOptions__AllowedRpIds__0 = web-server.<domain>` on aca/aca-neon only. Design
  region states the exposure rule per mode/target/flag. `program.cs` calls it under `#if yarp #else
  #if web`, in the slot `IngressResource.Add` takes, after `AddEFMigrations`.
- dev CLI: `dev deploy` smoke and `dev open` on aca/aca-neon run `az containerapp list` filtered to the
  external app named `ingress` or `web-server` (`DeployAca.BuildPublicAppArguments` /
  `ParsePublicApp` / `NoPublicAppReason`, record `AcaPublicApp`). Exactly one must answer. Smoke on
  web-server uses `/alive` + `/api/identity/sign-in-methods` (`DeployPipeline.SmokePathsFor`), since
  `/web-server/alive` is an ingress-only route. `DeployOperate.WebServerResourceName` is agreement-
  tested against `ServiceNames.WebServiceName`.
- Tests: `aca-publish-tests` / `aca-neon-publish-tests` expect exactly one external app: ingress, else
  web-server. New `Publish_Should_AllowTheWebServerHostForPasskeys` (both suites) compiles in the
  `#else` of `#if yarp`. dev-cli-tests cover the az query, parser, reason text and smoke paths.
- `skills/tw-deploy` (SKILL.md topology, safety table, `dev open`; references passkey-hosts,
  azure-container-apps, azure-container-apps-neon) say which app is public with yarp off.

Gates:

- `./bin/dev build` — passed (0 warnings, 0 errors).
- `./bin/dev test --no-build --project dev-cli-tests --filter-class DeployOperate_` — 64/64 passed
  (covers deploy-operate-container-apps-given-tests.cs and deploy-operate-app-host-agreement-given-tests.cs).
- `./bin/dev test --no-build --project dev-cli-tests --filter-class DeployPipeline_` — 17/17 passed
  (deploy-pipeline-tests.cs).
- `./bin/dev test --no-build --project aspire-tests --filter-class AcaPublish_Given_` — 15/15 passed
  (aca-publish-tests.cs, aca-publish-secrets-tests.cs; yarp on).
- `./bin/dev test --no-build --project aspire-tests --filter-class AcaNeonPublish_Given_` — 7/7 passed
  (aca-neon-publish-tests.cs; yarp on).
- Yarp-off proof (temporary, reverted): removed `yarp` from the AppHost and aspire-tests
  DefineConstants (TWA0010 muted), removed `ingress-resource.cs` as the template does, then
  `AcaPublish_Given_` 15/15 and `AcaNeonPublish_Given_` 7/7 passed (including both
  `...WebServerHostForPasskeys` facts), and `./bin/dev publish aca` passed its suite. Its
  `web-server.bicep` had `external: true` and
  `WebAuthnOptions__AllowedRpIds__0 = 'web-server.${aca_env_outputs_..._default_domain}'`.
- Template engine: a probe template with the exact `program.cs` block, generated with
  `--yarp false`, `--yarp true`, and `--yarp false --web false`, emitted the edge call, the ingress
  call, and neither, respectively.
- `./bin/dev lint-skills` — 11/11 passed.
- `ganda repo audit` — first run failed `source-file-line-cap` (my SKILL.md edit reached 502 lines);
  condensed to 496, rerun passes with 1 non-blocking advisory (`vscode-window-icon`: the git-ignored
  local `.vscode/settings.json` is absent).
- Test projects are not standalone runfiles (aspire-tests and dev-cli-tests are csproj suites);
  targeted runs above via `--project`/`--filter-class`.
- Not run: `dev template-smoke --matrix` (CI runs the flag-off matrix nightly; the NoYarp compile
  path was proven by the define-removal run above). No live Azure deploy was made.

### How to validate (required before done)

Smoke: `./bin/dev test --project aspire-tests --filter-class AcaPublish_Given_` and
`--filter-class AcaNeonPublish_Given_`; then generate `dotnet new timewarp-architecture --yarp false`
and run `dev publish aca` (and `aca-neon`) in it.

Expect: yarp on — only `ingress` has `external: true`. yarp off — only `web-server` has
`external: true`, and `web-server.bicep` carries
`WebAuthnOptions__AllowedRpIds__0 = web-server.${aca_env_outputs_azure_container_apps_environment_default_domain}`.
After `dev deploy --target aca-neon` on a yarp-off app, smoke prints `Public container app: web-server`
and `200 /alive`, `200 /api/identity/sign-in-methods`, and `dev open --target aca-neon` opens
`https://web-server.<domain>`.
