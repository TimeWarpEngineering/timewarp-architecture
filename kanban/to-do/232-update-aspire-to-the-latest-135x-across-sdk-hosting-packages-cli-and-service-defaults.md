# Update Aspire to the latest 13.5.x across SDK, hosting packages, CLI, and service defaults

## Description

Requested by Steve 2026-09-16, per https://aspire.dev/whats-new/aspire-13-5/#-upgrade-to-aspire-135.
The repo is already on the 13.5 series (SDK, hosting packages, and CLI at **13.5.3**); the
current stable is **13.5.4**. Bring everything to 13.5.4 in one move — the release notes warn
that mixed 13.4/13.5 (and by extension mixed patch) packages can fail at runtime — and apply the
13.5 breaking changes that touch this repo.

Current state (2026-09-16):

| Item | Now | Latest |
|------|-----|--------|
| `Aspire.AppHost.Sdk` (aspire-app-host.csproj) | 13.5.3 | 13.5.4 |
| `Aspire.Hosting.Yarp`, `.PostgreSQL`, `.Testing` (Directory.Packages.props) | 13.5.3 | 13.5.4 |
| `Aspire.Hosting.EntityFrameworkCore` | 13.5.3-preview.1.26425.3 | 13.5.4-preview.1.26464.4 (no stable yet) |
| `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL` | (check pin) | 13.5.4 |
| `Microsoft.Extensions.ServiceDiscovery` / `.Http.Resilience` (service defaults) | 10.9.0 | 10.10.0 (verify Resilience) |
| Aspire CLI (`aspire --version`) | 13.5.3 | 13.5.4 |
| `global.json` SDK | 10.0.400 rollForward latestFeature | unchanged |

## Requirements

- Prefer the documented path: `aspire update --self` then `aspire update` from the repo root
  (`aspire.config.json` points at the AppHost). Review the diff it produces; if it edits files
  outside `Directory.Packages.props` / the AppHost csproj, keep only what is needed and explain.
  If `aspire update` cannot run headless, bump the pins by hand to the table above.
- Keep every `Aspire.*` package on the same 13.5.4 series (EF hosting on its matching
  `13.5.4-preview.1.*` since no stable exists; note that in Results). Bump ServiceDiscovery /
  Resilience to the 10.10.0 line only if the service-defaults project builds warning-free with it.
- Apply 13.5 breaking changes that apply here (grep, do not assume): `ServiceProvider` → `Services`
  on hosting context types; `PublishAsConnectionString` → `AddConnectionString`; proxyless endpoint
  port allocation now happens during preparation (check the YARP ingress pins `Ingress:Port` /
  `Ingress:HttpPort` still bind 63610/63620 in Development and the 118/155 wait-edge notes in
  `aspire-app-host/program.cs` still hold); `DotnetProjectResource` moved to
  `Aspire.Hosting.Dotnet` (experimental) if referenced; dev-cli `aspire ps` usage (`--resources`
  / `--include-hidden` removed → `aspire describe`) in `tools/dev-cli/endpoints/db-app-host.cs`
  / `run-command.cs` if used. New AppHosts set `AspireUseCliBundle=true`; decide whether to add
  it to this AppHost (document either way).
- CI: check `.github/workflows` for a pinned Aspire CLI install; align it.
- Docs: `documentation/developer/guides/*` that state the Aspire version.

## Checklist

- [ ] SDK, all `Aspire.*` pins, and CLI at 13.5.4 (EF hosting at its 13.5.4 preview)
- [ ] Breaking-change grep applied; `Ingress` ports and wait-edge notes verified
- [ ] `dev build` 0/0; `dev run` boots (web-server, api-server, yarp, postgres, migrations);
      `cd tests/container-apps/aspire/aspire-tests && dotnet test -c Release` green
- [ ] `dev template-smoke` passes (generated AppHost uses the new SDK)
- [ ] `ganda repo audit` clean; CI workflow aligned; docs updated
- [ ] Results and How to validate (include `aspire --version` and the pin table after)

## Session

- Created: cockpit (2026-09-16)
- Claude Code cockpit session: https://claude.ai/code/session_01KPZXyAmA6Vk99W1yUQUn1N

## Notes

- Release notes summary (fetched 2026-09-16): upgrade = `aspire update --self` + `aspire update`;
  breaking: `ServiceProvider`→`Services`, `PublishAsConnectionString` obsolete, `aspire ps` flags
  removed, GitHub Models package obsolete, proxyless port allocation timing
  (`ASPIRE_PROXYLESS_ENDPOINT_PORT_RANGE`), `TerminalOptions.Shell` removed, dashboard AI assistant
  removed, VS Code auto-launch off, Orleans internals, `DotnetProjectResource` relocated.
  Features of interest: `WithHttpsDeveloperCertificate()` (experimental) for project resources,
  Blazor gateway Docker Compose publishing, persistent volumes for Kubernetes environments.
- Files: `Directory.Packages.props` (L74-86, L148-182), `source/container-apps/aspire/projects/aspire-app-host/aspire-app-host.csproj`,
  `aspire-app-host/program.cs`, `aspire-service-defaults/*`, `tools/dev-cli/endpoints/{run-command,db-app-host}.cs`,
  `aspire.config.json`, `.github/workflows/*.yml`.
- Prior: 155 (wait edges removed), 118 (plane split), 104-031 (original Host forwarding), 107 (YARP route generation).

## Results

_Pending._

### How to validate

_Pending._
