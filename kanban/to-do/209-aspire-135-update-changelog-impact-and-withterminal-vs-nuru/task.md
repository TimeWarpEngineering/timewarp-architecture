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

- [ ] `aspire update` in this worktree; SDK + all `Aspire.Hosting.*` (incl. EF preview) on one 13.5 train
- [ ] Breaking-change grep + Notes table (file / hit / action)
- [ ] Confirm `dev db *` / `aspire describe --include-hidden` still match 13.5 CLI
- [ ] WithTerminal vs Nuru verdict in Notes (and Nuru task only if product work is warranted)
- [ ] Cross-repo scan table filled
- [ ] `dev build` 0/0; aspire-tests still boot
- [ ] Do not add Bootstrap, do not change pinned ingress/dashboard ports

## Session

- Created: ganda session 626426 (2026-09-02)
- Cockpit: grok `01a03d38-9611-7620-aae5-848e15dafa94` (timewarp-flow)
- Operator: CLI already 13.5.3; asked for `aspire update` + 13.5 review + WithTerminal/Nuru

## Notes

Cockpit research (do not treat as Results):

- Architecture already on 13.5.2 (not 13.4). This is a **patch train bump** (13.5.2 → whatever `aspire update` selects, likely 13.5.3) plus a **changelog impact review**, not a 13.4→13.5 migration.
- `WithTerminal` is for TUI/shell resources. Architecture’s web/api/grpc/postgres/ingress are not candidates. Nuru REPL **is**.
- Keep `AspireUseCliBundle=false` unless review finds `dotnet run` on AppHost is now broken without the bundle.
