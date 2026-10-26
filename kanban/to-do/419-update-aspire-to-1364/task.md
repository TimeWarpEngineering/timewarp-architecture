# Update Aspire to 13.6.4

## Description

Steve, 2026-10-26: "create a task to upate to Aspire 13.6.4", with
<https://aspire.dev/whats-new/aspire-13-6/#-upgrade-to-aspire-136>.

The template pins `<AspireVersion>13.6.2</AspireVersion>` in `Directory.Packages.props`. Aspire
13.6.4 is on nuget.org. The 13.6 minor upgrade already landed, so this is a patch bump. The 13.6
breaking changes on the linked page (MongoDB TLS, the Cosmos DB vNext emulator, Front Door origin
names, portable connection-string names, the terminal API namespace move) came with 13.6.x and
apply to us already.

The patches fix problems we can hit:

- **13.6.3:** resource operations no longer stall when a watch stops being read. The bundled DCP
  moves to 0.26.6.
- **13.6.4:** a DCP create that times out (or returns HTTP 504) no longer leaves a resource in
  `FailedToStart` (microsoft/aspire#20691, the startup timeouts reported in 13.6). CLI diagnostic
  logs become owner-only (`0600`/`0700`) on Unix.

## Requirements

- **One property drives the bump.** Set `<AspireVersion>` to `13.6.4`. The AppHost SDK, every
  `Aspire.*` pin, `Aspire.Hosting.Testing`, and the CI `Aspire.Cli` install read it.
- **Preview suffixes.** `Aspire.Hosting.Kubernetes` and `Aspire.Hosting.EntityFrameworkCore` carry
  their own preview build after the property. Move both from `-preview.1.26519.11` to
  `-preview.1.26523.11` (the 13.6.4 preview on nuget.org).
- **Bump the pins by hand, not with `aspire update`.** `aspire update` reflows csproj attributes,
  skips `Aspire.Hosting.Testing`, and does not know about the single-property layout. Run
  `aspire update --self` (or `dotnet tool update -g Aspire.Cli --version 13.6.4`) locally so
  `dev run`'s CLI version check passes. Check for any other `13.6.2` pin (for example a
  `dotnet-tools.json` entry or a CI step) and move it too.
- **tw-deploy reference.** `skills/tw-deploy/references/azure-container-apps-neon.md` cites 13.6.2
  for ACR Basic provisioning and the per-project API. Confirm those facts still hold on 13.6.4
  (publish output: `dev publish all` or the aca/aca-neon publish tests) and update the version in
  the text. If the behavior changed, say what changed.
- **Leave test fixtures alone.** `tests/tools/dev-cli-tests/aspire-run-tests.cs` uses 13.6.x
  strings as fixed inputs to `AssessCliVersion` and `ParseRepoAspireVersion`. They don't read the
  repo pin, so they stay as they are.
- **No release from this alone.** Only template content and pins change. Run `dev check-version`.
  Bump `<Version>` only if it requires it.

## Checklist

- [ ] `<AspireVersion>` 13.6.4 and both preview suffixes moved in `Directory.Packages.props`
- [ ] No other `13.6.2` Aspire pin left (`rg '13\.6\.2'` outside kanban and test fixtures)
- [ ] tw-deploy aca-neon reference checked against 13.6.4 publish output and updated
- [ ] `dev build` 0/0; `dev check-version`; `dev regenerate-generated` golden diff empty
- [ ] `tests/container-apps/aspire/aspire-tests` green
- [ ] `tests/tools/dev-cli-tests` green
- [ ] `dev template-smoke` green
- [ ] `ganda repo audit` clean

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-26)

## Notes

- Release notes: <https://github.com/microsoft/aspire/releases/tag/v13.6.3>,
  <https://github.com/microsoft/aspire/releases/tag/v13.6.4>.
- 13.6.4 adds `DcpPublisher:KubernetesApiTimeout`, `DcpPublisher:KubernetesInitializationAdditionalTimeout`,
  and `DcpPublisher:KubernetesCreateRecoveryTimeout`. The defaults are fine; don't set them.
