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

## Checklist

- [ ] `web-server-resource.cs`: external endpoints in publish mode when the target is Container Apps
      and the yarp flag is off. The comment at "External in run mode only" states the new rule.
- [ ] Passkey allowed RP ID for the web-server FQDN on the Azure targets with yarp off.
- [ ] `publish-target.cs` Design text: the "without the yarp flag publishes no host port at all"
      statement is still true for compose and kubernetes; say what the Azure targets do.
- [ ] `aca-neon-publish-tests.cs` `Publish_Should_OnlyExposeTheIngressAndNoDashboard`: without the
      ingress, expect exactly one `external: true` and expect it on web-server (today it expects 0).
- [ ] `aca-publish-tests.cs`: same expectation, and its header line "none at all without the yarp flag".
- [ ] `dev deploy` smoke and `dev open` resolve the public URL with yarp off.
- [ ] `skills/tw-deploy`: the ingress topology section says which app is public per target with yarp off.
- [ ] `dev template-smoke --matrix` NoYarp variant and the publish suites pass.

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

### How to validate (required before done)
