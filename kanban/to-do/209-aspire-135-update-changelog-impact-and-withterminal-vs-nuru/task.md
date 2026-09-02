# Aspire 13.5 update, changelog impact, and WithTerminal vs Nuru

## Description

CLI is already **13.5.3** (`aspire --version`). Architecture AppHost SDK + hosting packages are still **13.5.2**. Run `aspire update` from this repo, then review every 13.5 change for impact here and in sibling TimeWarp repos.

Source of truth: https://aspire.dev/whats-new/aspire-13-5/ (section [Upgrade to Aspire 13.5](https://aspire.dev/whats-new/aspire-13-5/#-upgrade-to-aspire-135)). Breaking-change list is on the same page.

Do **not** mix 13.4 and 13.5 packages (Aspire known issue: `MissingMethodException` / `TypeLoadException`). Bump SDK and every `Aspire.Hosting.*` pin together, including the EF preview train.

`WithTerminal()` is **not** Nuru API and **not** Amuru `WithTerminalLogger`. It is Aspire’s experimental PTY/TUI attach (`ASPIRETERMINAL001`, dashboard terminal view, `aspire terminal` behind `features.terminalCommandsEnabled`). Investigate whether it unblocks Nuru REPL under Aspire (today the Nuru sample explicitly cannot run REPL because Aspire had no interactive console).

## Current pins (architecture origin-home, 2026-09-02)

| Surface | Version |
|---|---|
| Aspire CLI (machine) | 13.5.3 — already updated; do not `aspire update --self` unless the worker’s CLI is older |
| `Aspire.AppHost.Sdk` | 13.5.2 (`aspire-app-host.csproj`) |
| `Aspire.Hosting.Yarp` / PostgreSQL / Testing | 13.5.2 (CPM) |
| `Aspire.Hosting.EntityFrameworkCore` | 13.5.2-preview.1.26421.6 |
| `AspireUseCliBundle` | `false` + `NoWarn` ASPIRE010 (keep unless this task decides to opt in) |

`dev db *` talks to the live graph via `aspire resource web-migrations … --apphost … --non-interactive`. 13.5 **removed** `aspire ps --include-hidden`; `aspire describe --include-hidden` and `aspire resource` still have it.

## Requirements

### A. `aspire update` in this repo

- Run from architecture **claimed worktree** (not cockpit master): `aspire update` (project packages). CLI is already 13.5.3.
- Align AppHost SDK, CPM `Aspire.Hosting.*`, and the EF **preview** package to the same 13.5 train (no 13.4 leftovers).
- Keep `AspireUseCliBundle=false` unless a documented reason to opt in (`dotnet run` / `Aspire.Hosting.Testing` still launch without the CLI bundle).
- `dev build` 0/0; `aspire-tests` still boot; `dev db status` against a running AppHost if one is up.
- Do not change `launchSettings.json` pinned ports (17304 / 63610 / 63620).

### B. Changelog review — architecture

Walk https://aspire.dev/whats-new/aspire-13-5/ **Breaking changes** against this tree. Record hits in Notes (file + whether a code change is required):

1. Hosting context `ServiceProvider` → `Services` — check `WithCommand` / `ExecuteCommandContext` (`resource-builder-extensions.cs` Scalar command uses `_ =>`, likely OK).
2. `PublishAsConnectionString` obsolete — grep.
3. `aspire ps --resources` / `--include-hidden` removed — grep `dev` CLI, skills, docs. `aspire describe` / `aspire resource` still support `--include-hidden`.
4. GitHub Models hosting deprecated — we should not have it.
5. Proxyless endpoint port allocation timing — YARP/ingress/postgres ports; confirm pinned `launchSettings` still win.
6. `TerminalOptions.Shell` removed — only if we already called `WithTerminal`.
7. Dashboard AI Assistant removed — no product code.
8. VS Code dashboard auto-launch removed — docs/skills only if we documented auto-open.
9. `DotnetProjectResource` / `AddDotnetProject` experimental (`ASPIREDOTNETPROJECT001`).
10. Do **not** add `WithTerminal()` to web-server / api-server / postgres by default (background services, not TUIs). Experimental; debugger does not auto-attach.

Also note 13.5 features we **might** want later (not this PR unless trivial): resource command `CommandOptions.Arguments`, Interaction Service prompts, `aspire stop --force`.

### C. WithTerminal vs Nuru (investigation; product Nuru work is a **separate** repo task)

Docs: https://aspire.dev/app-host/with-terminal/

Nuru today (`timewarp-nuru` `samples/aspire-otel/apphost.cs`):

```csharp
#:sdk Aspire.AppHost.Sdk@13.1.0
// Pass arguments to run a command instead of entering REPL mode
// (REPL requires interactive console which Aspire doesn't provide)
builder.AddCSharpApp("nuruclient", "./nuru-client.cs")
  .WithArgs("status");
```

That comment is the hook: 13.5 `WithTerminal()` is the interactive console Aspire did not have. Investigate and write a short verdict in Notes:

- Does `AddCSharpApp(...).WithTerminal()` let the Nuru REPL work in the dashboard / `aspire terminal attach`?
- stdin/TUI: Nuru `ITerminal`, Spectre, REPL samples (`samples/endpoints/10-repl`). PTY vs redirected stdout.
- Experimental: `ASPIRETERMINAL001`; CLI `aspire terminal` needs `aspire config set features.terminalCommandsEnabled true`.
- **Not** Amuru `WithTerminalLogger` (`--tl`) — different API.
- Nuru backlog **083** (Blazor WASM terminal REPL) is a **browser** terminal over SignalR, not Aspire `WithTerminal`. Do not conflate.
- Nuru sample SDK is **13.1.0** — mixed-version risk if only WithTerminal is added.

If the verdict is “Nuru should adopt WithTerminal in aspire-otel / a new sample”, **create a timewarp-nuru task** and link it here. Do not implement Nuru in this architecture PR.

### D. Other TimeWarp repos (scan + Notes table; no drive-by PRs)

| Repo | Aspire today (spot check 2026-09-02) | This task |
|---|---|---|
| **timewarp-architecture** | SDK + hosting **13.5.2**; CLI 13.5.3 | **Implement** `aspire update` + review |
| **timewarp-nuru** | `samples/aspire-otel` Sdk **13.1.0**; empty Aspire CPM group | Investigate WithTerminal; spawn Nuru task if needed |
| **netclaw** | Sdk **13.3.5**, `AspireHostingVersion` **13.4.6** | Note only — do not mix 13.4/13.5; separate bump |
| **copic** | no AppHost Sdk in tree (spot check) | Note |
| **timewarp-flow** | no AppHost | Note (skills mention Aspire 13.4) |
| **timewarp-amuru** | `WithTerminalLogger` only | Not Aspire WithTerminal |

Grep org worktrees for `Aspire.AppHost.Sdk` / `Aspire.Hosting` and list any other hits in Notes.

## Checklist

- [x] `aspire update` in this worktree; SDK + all `Aspire.Hosting.*` (incl. EF preview) on one 13.5 train
- [x] Breaking-change grep + Notes table (file / hit / action)
- [x] Confirm `dev db *` / `aspire describe --include-hidden` still match 13.5 CLI
- [x] WithTerminal vs Nuru verdict in Notes (and Nuru task only if product work is warranted)
- [x] Cross-repo scan table filled
- [x] `dev build` 0/0; aspire-tests still boot
- [x] Do not add Bootstrap, do not change pinned ingress/dashboard ports

## Session

- Created: ganda session 626426 (2026-09-02)
- Cockpit: grok `01a03d38-9611-7620-aae5-848e15dafa94` (timewarp-flow)
- Operator: CLI already 13.5.3; asked for `aspire update` + 13.5 review + WithTerminal/Nuru
- Implementer: claude (Fable 5.1) under `ganda task work 209`, worktree
  `task-209-aspire-135-update-changelog-impact-and-withtermina` (2026-09-02)
- Machine side effects (outside this repo, recorded for reversibility): removed the dead
  user-level NuGet source `smoke` (pointed at the deleted `dev` worktree's
  `artifacts/template-smoke/packages`; it failed every restore with NU1301 — re-add with
  `dotnet nuget add source <path> -n smoke` if a smoke run needs it); `aspire agent init`
  (run to refresh vendored skills) also wrote a PostToolUse telemetry hook into
  `~/.claude/settings.json`, which was removed again, and refreshed `~/.agents/skills`.
- Nuru follow-up: timewarp-nuru task **467** (Spike Aspire 13.5 WithTerminal for the Nuru REPL), registered via kanban-only PR timewarp-nuru#230 (`ganda kanban publish` blocked by that repo's PR-required ruleset).

## Notes

Cockpit research (do not treat as Results):

- Architecture already on 13.5.2 (not 13.4). This is a **patch train bump** (13.5.2 → whatever `aspire update` selects, likely 13.5.3) plus a **changelog impact review**, not a 13.4→13.5 migration.
- `WithTerminal` is for TUI/shell resources. Architecture’s web/api/grpc/postgres/ingress are not candidates. Nuru REPL **is**.
- Keep `AspireUseCliBundle=false` unless review finds `dotnet run` on AppHost is now broken without the bundle.

## Results

### A. `aspire update` (13.5.2 → 13.5.3, one train)

| Surface | Before | After |
|---|---|---|
| `Aspire.AppHost.Sdk` (`aspire-app-host.csproj`) | 13.5.2 | 13.5.3 |
| `Aspire.Hosting.Yarp` / `Aspire.Hosting.PostgreSQL` (CPM) | 13.5.2 | 13.5.3 |
| `Aspire.Hosting.EntityFrameworkCore` (CPM, preview) | 13.5.2-preview.1.26421.6 | 13.5.3-preview.1.26425.3 |
| `Aspire.Hosting.Testing` (CPM) | 13.5.2 | 13.5.3 (aligned by hand; `aspire update` only walks the AppHost graph) |
| `AspireUseCliBundle` / `NoWarn ASPIRE010` | `false` | unchanged — `dotnet run` and `Aspire.Hosting.Testing` boot without the CLI bundle |
| `launchSettings.json` ports | 17304/15217 (AppHost), 63610/63620 (yarp) | unchanged |

`aspire update --yes` also reflowed every multi-attribute `ProjectReference` in the AppHost
csproj onto one line; that reformat was reverted and only the SDK version line kept.

### B. 13.5 breaking-change review (this tree)

| # | Change | Hit | Action |
|---|---|---|---|
| 1 | Command context `ServiceProvider` → `Services` | `resource-builder-extensions.cs` Scalar `WithCommand` discards the execute context (`_ =>`) and `UpdateState` reads only `ResourceSnapshot` | none |
| 2 | `PublishAsConnectionString` obsolete | no hits | none |
| 3 | `aspire ps --resources` / `--include-hidden` removed | `dev db *` uses `aspire resource web-migrations <cmd> --apphost … --non-interactive --nologo` (no `ps`) — OK. Vendored skills `.claude/skills/aspire*` + mirrored `.agents/skills/aspire*` documented `aspire ps --include-hidden` in 6 places and described `aspire ps` as a resource list | skills patched: `aspire describe --include-hidden` / `aspire resource <name> --include-hidden`; `ps` described as AppHost list; router prose 13.4 → 13.5 |
| 4 | GitHub Models hosting deprecated | no hits | none |
| 5 | Proxyless endpoint port timing | no `isProxied: false`; ingress ports set via `WithEndpoint("https"/"http", e => e.Port = …)` from `Ingress:*` config; per-project `launchSettings` pinned | none — verified live: ingress 63610/63620, dashboard 17304 |
| 6 | `TerminalOptions.Shell` removed | no `WithTerminal` in tree | none |
| 7 | Dashboard AI assistant removed | no hits | none |
| 8 | VS Code dashboard auto-launch removed | `aspire-monitoring/references/diagnostics-bridge.md` said "auto-launched" | prose fixed (both skill trees) |
| 9 | `AddDotnetProject` experimental | no hits | none |
| 10 | `WithTerminal()` on web/api/grpc/postgres | not added (background services) | none |

Vendored skill refresh: `aspire agent init` on 13.5.3 only ships `aspireify` + `dotnet-inspect`
bundle skills; the `aspire*` router skills come from elsewhere and were hand-patched.
`dotnet-inspect/SKILL.md` was kept at the 13.5.3 bundle version; the new `aspireify` skill was
**not** added (separate decision). Historical `13.4.6` comments in `program.cs` (lines 27, 199)
are dated incident notes and were left alone.

Verified with the 13.5.3 CLI against the running master AppHost: `aspire describe --include-hidden
--apphost …` lists hidden resources; `aspire resource web-migrations ef-database-status --apphost
… --non-interactive --nologo` (the `dev db status` shape) executes; `aspire ps` has no
`--include-hidden` / `--resources` flag and lists AppHosts only.

13.5 features noted for later (not this PR): `CommandOptions.Arguments` for resource commands,
Interaction Service prompts, `aspire stop --force`, and a `references/aspire-13-5-breaking-changes.md`
for the vendored `aspire` skill (it currently ships a 13.3 list only).

### C. WithTerminal vs Nuru verdict

**Plausible, unproven — spike warranted; created timewarp-nuru task 467.**

- `WithTerminal()` (13.5, `ASPIRETERMINAL001`) runs the resource under a real PTY with attached
  stdin, gives the dashboard a live Terminal view, and `aspire terminal attach` behind
  `features.terminalCommandsEnabled` (unset on this machine). Debugger does not auto-attach.
  `TerminalOptions` now exposes `Columns`/`Rows`/`ShowTerminalHost`; `Shell` was removed because it
  never did anything.
- Nuru's REPL has **no** `Console.IsInputRedirected` guard: it enters REPL only on `--interactive`
  / `-i` or `ReplOptions.AutoStartWhenEmpty` (generated by `interceptor-emitter.cs`
  `EmitInteractiveFlag`), then uses raw-mode key reading. So under a PTY the sample's
  "REPL requires interactive console which Aspire doesn't provide" comment is now stale in
  principle — that comment is the whole reason `WithArgs("status")` exists.
- Unknowns the docs do not settle: whether `WithTerminal()` is accepted on the `AddCSharpApp`
  builder (docs show only `AddExecutable` / `AddContainer`), whether Aspire's PTY satisfies
  `Console.ReadKey(intercept: true)` and resize the way Nuru's reader expects, and whether the
  dashboard xterm forwards the ANSI sequences Nuru's REPL emits.
- Mixing risk: the sample pins `#:sdk Aspire.AppHost.Sdk@13.1.0`; adopting `WithTerminal()` means
  moving the whole sample to 13.5 (no 13.4/13.5 mix). Nuru CPM has an empty Aspire group.
- Not Amuru `WithTerminalLogger` (that wraps `dotnet … --tl`). Not Nuru backlog 083 (browser
  terminal over SignalR); 083 stays as is.

### D. Cross-repo scan (org worktrees, master checkouts, 2026-09-02)

| Repo | Aspire | Note |
|---|---|---|
| timewarp-architecture | SDK + hosting 13.5.3 (this PR) | done here |
| timewarp-nuru | `samples/aspire-otel` SDK 13.1.0; empty Aspire CPM group | task 467 (spike) |
| netclaw | SDK 13.3.5 + `AspireHostingVersion` 13.4.6 (already mixed trains) | separate bump task; do not mix 13.4/13.5 |
| netclaw-skill-server | `dev/src/SkillServer.AppHost` SDK 13.1.2, `AspireVersion` 13.1.2 | note only (new hit vs the task table) |
| copic | no AppHost | none |
| timewarp-flow | no AppHost; no 13.4 skill mentions on master | none |
| timewarp-amuru | `WithTerminalLogger` only (dotnet `--tl`) | not Aspire |
| fluentui-blazor (dev-v5) | template placeholder `!!REPLACE_WITH_LATEST_ASPIRE_VERSION!!` | upstream template, none |

### How to validate

**Smoke**

```bash
grep -n "Aspire" Directory.Packages.props source/container-apps/aspire/projects/aspire-app-host/aspire-app-host.csproj | grep -i version
dotnet run tools/dev-cli/dev.cs -- build
cd tests/container-apps/aspire/aspire-tests && dotnet test -c Release
grep -rn "aspire ps --include-hidden" .claude/skills .agents/skills
diff -r .claude/skills .agents/skills && echo identical
```

**Expect**

- Every `Aspire.*` pin reads 13.5.3 (EF preview `13.5.3-preview.1.26425.3`); no 13.5.2 / 13.4 left.
- `dev build`: `0 Warning(s)  0 Error(s)`.
- aspire-tests: `Passed! total: 7 failed: 0` (closed-box AppHost boots on 13.5.3).
- The `aspire ps --include-hidden` grep returns nothing; both skill trees are identical.
- Optional with an AppHost running from this worktree (`dev run`): `dev db status` succeeds
  (`aspire resource web-migrations ef-database-status …`).
