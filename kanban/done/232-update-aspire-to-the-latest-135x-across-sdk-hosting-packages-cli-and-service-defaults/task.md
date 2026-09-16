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

- [x] SDK, all `Aspire.*` pins, and CLI at 13.5.4 (EF hosting at its 13.5.4 preview)
- [x] Breaking-change grep applied; `Ingress` ports and wait-edge notes verified
- [x] `dev build` 0/0; isolated AppHost boots (web-server, api-server, ingress, postgres, migrations);
      `cd tests/container-apps/aspire/aspire-tests && dotnet test -c Release` green
- [x] `dev template-smoke` passes (generated AppHost uses the new SDK)
- [x] `ganda repo audit` clean (2 pre-existing advisories); CI workflow aligned; docs updated
- [x] Results and How to validate (include `aspire --version` and the pin table after)
- [x] Implementation review: round 1 general, disposition clean

## Session

- Created: cockpit (2026-09-16)
- Claude Code cockpit session: https://claude.ai/code/session_01KPZXyAmA6Vk99W1yUQUn1N
- Implementation: Grok (2026-09-16) on claimed worktree `task-232-update-aspire-to-the-latest-135x-across-sdk-hostin`
- Review oracle: Grok (2026-09-16) effort 1, roster general (Grok 4.5 sub-agent `01a0ab06-f5ee-7c31-9b39-4cd892222458`)

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
- Review kitchen: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.

## Results

One-train bump 13.5.3 → 13.5.4 (SDK, hosting packages, testing, CLI) plus matching EF preview. ServiceDiscovery / Resilience / ServiceDiscovery.Yarp took 10.10.0 because service-defaults and yarp restore/build 0/0.

### Upgrade path

- `aspire update --self` is a no-op on a `dotnet tool` install; it prints `dotnet tool update -g Aspire.Cli`. That command moved the CLI **13.5.3 → 13.5.4**.
- `aspire update --yes --non-interactive --nologo` from the repo root (`aspire.config.json` AppHost) bumped `Aspire.AppHost.Sdk`, `Aspire.Hosting.Yarp`, `Aspire.Hosting.PostgreSQL`, and `Aspire.Hosting.EntityFrameworkCore`.
- The updater also collapsed several `ProjectReference` elements in `aspire-app-host.csproj` onto one line. That formatting was reverted; only the SDK version change was kept.
- `Aspire.Hosting.Testing` is not in the AppHost graph, so `aspire update` does not touch it. Bumped by hand to 13.5.4 (same as task 209).

### Pin table after

| Item | After |
|------|--------|
| `Aspire.AppHost.Sdk` | **13.5.4** |
| `Aspire.Hosting.Yarp` / `.PostgreSQL` / `.Testing` | **13.5.4** |
| `Aspire.Hosting.EntityFrameworkCore` | **13.5.4-preview.1.26464.4** (no stable package) |
| `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL` | **not referenced** — repo uses `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3 |
| `Microsoft.Extensions.ServiceDiscovery` / `.Yarp` / `Http.Resilience` | **10.10.0** |
| Aspire CLI (`aspire --version`) | **13.5.4+9c1b401dd67746739044f68959cbf4d3d7af93a6** |
| `global.json` SDK | 10.0.400 `rollForward` latestFeature (unchanged) |
| `AspireUseCliBundle` | **false** (kept) |

### Breaking-change grep (13.5 list vs this repo)

| Change | This repo |
|--------|-----------|
| Hosting context `ServiceProvider` → `Services` | No hits. Scalar `WithCommand` in `resource-builder-extensions.cs` discards the execute context (`_ =>`). |
| `PublishAsConnectionString` → `AddConnectionString` | No hits. |
| `aspire ps --resources` / `--include-hidden` removed | `dev run` uses `aspire run`; `dev db *` uses `aspire resource web-migrations … --non-interactive`. No `ps` flags (task 209). |
| `DotnetProjectResource` / `AddDotnetProject` | Not referenced. |
| Proxyless port allocation timing | Development pins still `Ingress:Port=63610` / `Ingress:HttpPort=63620` in `appsettings.Development.json`. Isolated `--isolated` start randomizes (observed https 39635 / http 44427). |
| 118/155 wait-edge notes | Still hold: no `WaitFor` / `WaitForCompletion` on `web-migrations`; `web-server` waits on `postgresDb` only. |
| `AspireUseCliBundle=true` on new templates | **Keep false.** Template hosts launch via `dotnet run` / `Aspire.Hosting.Testing` with NuGet-restored DCP/dashboard; `NoWarn` includes `ASPIRE010`. SDK default remains false for existing AppHosts. |

### CI / docs

- `.github/workflows/workflow.yml` has no pinned Aspire CLI install. CI runs `dotnet run tools/dev-cli/dev.cs -- workflow` / `template-smoke` (AppHost SDK comes from the csproj). No workflow edit.
- `documentation/developer/guides/*` does not exist in this repo. No developer-guide version strings to update.

### Files changed

- `Directory.Packages.props` — hosting/testing 13.5.4, EF preview 13.5.4-preview.1.26464.4, ServiceDiscovery family + Resilience 10.10.0
- `source/container-apps/aspire/projects/aspire-app-host/aspire-app-host.csproj` — SDK 13.5.4 only

### Gates

- `dotnet run tools/dev-cli/dev.cs -- build` → **0 Warning(s) 0 Error(s)**
- `cd tests/container-apps/aspire/aspire-tests && dotnet test -c Release` → **Passed! total: 7 failed: 0**
- Isolated AppHost (`aspire start --isolated`, SDK **13.5.4**): `api-server` Running, `web-server` Running, `ingress` Running, `postgres`/`postgres-db` Running, `web-migrations` Finished. Ingress `curl` http **200**, https **200**. Stopped with `aspire stop --apphost …` (left the unrelated master 13.5.3 AppHost running).
- `dotnet run tools/dev-cli/dev.cs -- template-smoke` → **Template smoke SUCCEEDED**. Generated `SmokeDefault` AppHost is `Aspire.AppHost.Sdk/13.5.4`.
- `ganda repo audit` → **passes** (2 pre-existing advisory warnings: `memsearch-scaffold`, `vscode-window-icon`).

### How to validate

**Depends on:** Docker (postgres), Aspire CLI 13.5.4 on PATH (`dotnet tool update -g Aspire.Cli` if `aspire --version` is older). Stop any AppHost in this worktree before rebuild (`aspire stop --apphost source/container-apps/aspire/projects/aspire-app-host/aspire-app-host.csproj`). In a ganda worktree use `--isolated` so a master AppHost is not reused.

**Smoke**

```bash
aspire --version
# Expect: 13.5.4+…

rg 'Aspire.AppHost.Sdk/' source/container-apps/aspire/projects/aspire-app-host/aspire-app-host.csproj
# Expect: Aspire.AppHost.Sdk/13.5.4

rg 'Aspire.Hosting\.(Yarp|PostgreSQL|Testing|EntityFrameworkCore)|ServiceDiscovery|Http.Resilience' Directory.Packages.props
# Expect: hosting/testing 13.5.4; EF 13.5.4-preview.1.26464.4; ServiceDiscovery* and Http.Resilience 10.10.0

dotnet run tools/dev-cli/dev.cs -- build
# Expect: 0 Warning(s) 0 Error(s)

cd tests/container-apps/aspire/aspire-tests && dotnet test -c Release
# Expect: Passed! total: 7 failed: 0

aspire start --isolated --non-interactive --nologo \
  --apphost source/container-apps/aspire/projects/aspire-app-host/aspire-app-host.csproj
aspire describe --include-hidden --non-interactive --nologo --format Json
# Expect: web-server / api-server / ingress / postgres Running; web-migrations Finished
# Then: aspire stop --apphost source/container-apps/aspire/projects/aspire-app-host/aspire-app-host.csproj
```

**Expect**

- CLI and every `Aspire.*` pin on 13.5.4 (EF hosting on `13.5.4-preview.1.26464.4`).
- Generated template AppHost (`artifacts/template-smoke/work/SmokeDefault/…/aspire-app-host.csproj` after `dev template-smoke`) uses `Aspire.AppHost.Sdk/13.5.4`.
- Development ingress pins remain 63610 / 63620 in `appsettings.Development.json`; isolated runs randomize ports.
- `AspireUseCliBundle` stays `false`.

**Automated gate**

```bash
dotnet run tools/dev-cli/dev.cs -- build
cd tests/container-apps/aspire/aspire-tests && dotnet test -c Release
dotnet run tools/dev-cli/dev.cs -- template-smoke
ganda repo audit
```

**Not in scope:** adding `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL` (unused); opting the AppHost into the CLI bundle; bumping `Microsoft.Extensions.Diagnostics.Testing` (still 10.9.0).

### Review disposition

- **Rounds:** 1
- **Effort / roster:** 1, `general` only (Grok 4.5 sub-agent)
- **Disposition:** `clean` (no issues raised; 0 open)
- **Final counts:** bug 0 / suggestion 0 / nit 0 (all statuses 0)
- **Paths:** `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`
- Orchestrator re-verified leftover 13.5.3 (none in product files), breaking-change greps, ingress pins 63610/63620, wait-edges, CI (no pinned Aspire CLI), and `AspireUseCliBundle=false`. No fix loop; no wontfix; no escalation.
