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

## Goal (implementer restatement)

Move this template from Aspire 13.6.2 to 13.6.4 as a patch bump. One `<AspireVersion>` property
becomes `13.6.4`, and the two preview packages (`Aspire.Hosting.Kubernetes` and
`Aspire.Hosting.EntityFrameworkCore`) keep that prefix with the 13.6.4 preview suffix
`-preview.1.26523.11`. Do not run `aspire update` against the projects. Update the local Aspire
CLI to 13.6.4 so `dev run` accepts it. Any other real 13.6.2 pin (tools manifest, CI literal)
moves with the property. Test fixtures that feed fixed strings into `AssessCliVersion` and
`ParseRepoAspireVersion` stay on 13.6.2.

Confirm the aca-neon write-up still matches 13.6.4 publish output: ACR Basic is still provisioned,
the per-project registry API is unchanged, and container apps still pull with `Server` and
`Identity` only. Update the cited version (and the source commit if it moved). If publish output
changed, record what changed. Comments in product source that name 13.6.2 as the current pin move
to 13.6.4 when the fact still holds, so a search outside kanban and the test fixtures finds no
13.6.2. Do not add the new DCP timeout settings. Do not cut a release. Bump `<Version>` only when
`dev check-version` says the current version is not ahead of the last release.

Done when `dev build` is 0/0, `dev check-version` passes, `dev regenerate-generated` has an empty
golden diff, the aspire container-app tests, dev-cli tests, and `dev template-smoke` pass, and
`ganda repo audit` is clean.

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

- [x] `<AspireVersion>` 13.6.4 and both preview suffixes moved in `Directory.Packages.props`
- [x] No other `13.6.2` Aspire pin left (`rg '13\.6\.2'` outside kanban and test fixtures)
- [x] tw-deploy aca-neon reference checked against 13.6.4 publish output and updated
- [x] `dev build` 0/0; `dev check-version`; `dev regenerate-generated` golden diff empty
- [x] `tests/container-apps/aspire/aspire-tests` green
- [x] `tests/tools/dev-cli-tests` green
- [x] `dev template-smoke` green
- [x] `ganda repo audit` clean

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-26)
- Implementation: task-work implementer (2026-10-26). Pins, the aca-neon reference, and the two AppHost version comments. `dotnet tool update -g Aspire.Cli --version 13.6.4` was already on 13.6.4. `<Version>` stayed at 2.0.0-beta.27.
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session not reported, max-turns 500, Done — 2026-10-26T06:55:23Z
- Review oracle: tw-implementation-review, effort 2, roster general (claude-opus-5-5, session not reported). 1 round, disposition clean (2026-10-26).
- Review oracle: review by reviewer-claude (claude, model claude-opus-5-5), session not reported, max-turns 120 — 2026-10-26T07:00:51Z

## Results

`<AspireVersion>` is `13.6.4`. `Aspire.Hosting.Kubernetes` and `Aspire.Hosting.EntityFrameworkCore` use `$(AspireVersion)-preview.1.26523.11`. The AppHost SDK, the stable `Aspire.*` pins, `Aspire.Hosting.Testing`, and the workflow `Aspire.Cli` install still read that one property. There is no `dotnet-tools.json` Aspire pin and no literal `13.6.2` left in CI. Outside `kanban/` and `tests/tools/dev-cli-tests/aspire-run-tests.cs`, `rg '13\.6\.2'` is empty. Those test strings stay fixed inputs.

`dev check-version` reports source `2.0.0-beta.27` one prerelease increment ahead of NuGet `2.0.0-beta.26`. `<Version>` was not bumped. The new DCP timeout settings were not set.

aca-neon publish behavior is the same on 13.6.4. `dev publish aca-neon` with CLI `13.6.4+c89352703f78ae46de90fbf751ed8b52b77b89bf` wrote `aca-env-acr.bicep` as `Microsoft.ContainerRegistry/registries` with `sku.name` `Basic`. `aca-env.bicep` references that registry as existing, assigns `AcrPull`, and outputs `AZURE_CONTAINER_REGISTRY_ENDPOINT` and `AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID`. api, grpc, and web `registries` entries set `server` and `identity` only. Ingress stays on `mcr.microsoft.com/dotnet/nightly/yarp` and has no `registries` entry. The four cited Aspire source files are byte-identical between commit `af07e33` (13.6.2) and `c893527` (13.6.4). `Azure.Provisioning.AppContainers` is still 1.2.0. The reference now cites 13.6.4 and `c89352703f78ae46de90fbf751ed8b52b77b89bf`.

`dev build`: 0 warnings, 0 errors. `dev regenerate-generated`: golden diff empty. aspire-tests: 84 passed, 0 failed. dev-cli-tests: 342 passed, 0 failed. `dev template-smoke`: `Template smoke SUCCEEDED` (SmokeDefault build 0 warnings; web aggregator 427/427, api 9/9, timewarp-testing 14/14). `dev lint-skills`: 9/9. `dev publish aca-neon` safety suite: 7 passed. `ganda repo audit`: Passed 33, Failed 0 errors, one pre-existing advisory warning, exit 0. The warning is `kanban-landed` for eight other claims that have no worktree (070-001, 104-024, 104-025, 104-026, 123, 141, 198, 244). That warning is not from this bump.

### Review

The review ran 1 round at effort 2 with a general reviewer. Final counts are 0 bug, 0 suggestion, and 0 nit, all statuses 0. The disposition is **clean**, with no wontfix and no escalation. The reviewer re-ran `dev build` (0/0) and `dev check-version` (pass). It confirmed that both `13.6.4-preview.1.26523.11` packages are on nuget.org and that tag `v13.6.4` is commit `c893527`. The four cited Aspire source files have the same blobs at 13.6.2 and 13.6.4. Artifacts are in `review/review-framework.md`, `review/round-1/merged.md`, and `review/disposition.md`.

### How to validate

Smoke:

```bash
rg '13\.6\.2' --glob '!kanban/**' --glob '!tests/tools/dev-cli-tests/aspire-run-tests.cs'
./bin/dev publish aca-neon
rg -n "name: 'Basic'" artifacts/aspire-output/aca-neon/aca-env-acr/aca-env-acr.bicep
rg -n "registries:|server:|identity:|username:|passwordSecretRef" artifacts/aspire-output/aca-neon --glob '*.bicep'
```

Expect: the first `rg` prints nothing. Publish prints `Azure Container Apps + Neon publish output is production-safe`. `aca-env-acr.bicep` contains `name: 'Basic'`. api-server, grpc-server, and web-server set `server` and `identity` under `registries`. No module sets `username` or `passwordSecretRef`. Ingress has no `registries` entry.

## Review

- Round 1: Done

## Notes

- Release notes: <https://github.com/microsoft/aspire/releases/tag/v13.6.3>,
  <https://github.com/microsoft/aspire/releases/tag/v13.6.4>.
- 13.6.4 adds `DcpPublisher:KubernetesApiTimeout`, `DcpPublisher:KubernetesInitializationAdditionalTimeout`,
  and `DcpPublisher:KubernetesCreateRecoveryTimeout`. The defaults are fine; don't set them.
