[![Dotnet](https://img.shields.io/badge/dotnet-10.0-blue)](https://dotnet.microsoft.com)
[![Stars](https://img.shields.io/github/stars/TimeWarpEngineering/timewarp-architecture?logo=github)](https://github.com/TimeWarpEngineering/timewarp-architecture)
[![Discord](https://img.shields.io/discord/715274085940199487?logo=discord)](https://discord.gg/7F4bS2T)
[![workflow](https://github.com/TimeWarpEngineering/timewarp-architecture/actions/workflows/workflow.yml/badge.svg)](https://github.com/TimeWarpEngineering/timewarp-architecture/actions/workflows/workflow.yml)
[![NuGet](https://img.shields.io/nuget/v/TimeWarp.Architecture.svg)](https://www.nuget.org/packages/TimeWarp.Architecture/)
[![NuGet](https://img.shields.io/nuget/dt/TimeWarp.Architecture.svg)](https://www.nuget.org/packages/TimeWarp.Architecture/)

[![Twitter](https://img.shields.io/twitter/follow/StevenTCramer.svg)](https://twitter.com/intent/follow?screen_name=StevenTCramer)
[![Twitter](https://img.shields.io/twitter/follow/TheFreezeTeam1.svg)](https://twitter.com/intent/follow?screen_name=TheFreezeTeam1)

# TimeWarp Architecture

## timewarp-architecture



### Documentation

Purpose/Design regions plus `skills/` are the documentation of record. `AGENTS.md` is the short
core for agents in this repo and is not packed into the template. Generated apps include
`skills/` in their template output.

### Installation

```console
dotnet new --install TimeWarp.Architecture
```

### Usage

```console
dotnet new timewarp-architecture -n MyTimeWarpApp
```

### What a generated app contains

- `source/` and `tests/`: the container apps for the flags you chose, and their test projects.
- `msbuild/` and the root build files (`*.slnx`, `Directory.Build.props`, `Directory.Packages.props`,
  `global.json`, `.editorconfig`).
- `skills/`: the agent skills the app's own code follows.
- `kanban/`: an empty board.
- `tools/dev-cli/`: the dev CLI, with `.timewarp/dev.jsonc` (its settings) and `.envrc` (puts
  `bin/` on `PATH`). Bootstrap it once with `dotnet run tools/dev-cli/dev.cs -- self-install`, then
  `dev --help`. It has the app commands: `build`, `test`, `run`, `db` (with postgres and web),
  `publish`, `deploy` (and `deploy migrate`), `open`, `deprovision`, `entra`, `workflow`, `clean`,
  `audit`. This repository's own template, release and skill-lint commands are not in it. In an app
  generated without a feature, a command that needs it (`entra` without web; `deploy` without web;
  `open` and `deploy` to compose or kubernetes without yarp, while aca and aca-neon serve web-server;
  `deploy migrate` without postgres or web) stops and names the flag that is off.
- `.github/`: CI. `workflows/workflow.yml` runs on every pull request and on pushes to `master`,
  `main` and `feature/**`. Its jobs are classify, then `ci (fast)`, `ci (integration)` and
  `ci (publish)` (each one `dev workflow` phase; publish runs `dev publish all` and its
  production-safety suites and deploys nothing), then `ci`, the one check over them.
  `actions/setup-ci/action.yml` installs .NET from `global.json`, the NuGet cache and the pinned
  Aspire CLI. A push or pull request whose tree already passed (a merge of a green pull request, a
  kanban-only change) skips the CI groups through a green-key marker; the trailer
  `Green-Key: full-run` on the head commit, or a manual run of the workflow, runs them anyway.

  To configure the repository on GitHub:
  - Make `ci` a required status check on the default branch (a ruleset or branch protection).
    `ganda pr merge` waits for it.
  - Runners are `ubuntu-latest`. To run classify, `ci (fast)` and `ci (publish)` on self-hosted
    runners, set the repository variable `CI_RUNNER_LABELS` to a JSON array of labels, such as
    `["self-hosted", "linux"]`. `ci (integration)` stays hosted: it needs Docker. Fork pull
    requests always run hosted.

### In-app Ask (xAI Grok)

Ctrl-K **Ask** calls xAI through web-server. The key stays in user-secrets and is never sent to
the browser. Set it with:

```pwsh
dotnet user-secrets set "XAI:ApiKey" "<your-xai-key>" --id 0e53fdd3-6f93-4d5a-9c86-040621f7929e
```

The `--id` is web-server's `UserSecretsId`, so the command works from any directory.
`dev run` still starts when the key is missing and prints that command. In Development, Ask
stays visible and shows the same command until the key is set. Any other environment names
`XAI__ApiKey` (an environment variable or a container-app secret) and does not show the
user-secrets id. `XAI:Model` overrides the default `grok-4.7`.

## Content

The template creates the distributed app projects and their corresponding test projects.
