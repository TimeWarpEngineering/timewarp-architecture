# Link the Aspire dashboard to a configured SigNoz instance

## Goal (implementer restatement)

`dev run` should show a SigNoz link on the Aspire dashboard when an operator configures one, and add nothing when they do not.

- The AppHost reads `Observability:SigNoz:Url`. A missing or blank value adds no resource. An absolute `https` URL adds a run-mode external service named `signoz` whose dashboard link text is "SigNoz". Any other value stops startup with a message that names the key.
- Publish and deploy (Compose, Kubernetes, Azure Container Apps) do not gain that resource. Their output stays the same.
- The URL is not written into template files, including `appsettings*.json`. The AppHost Design region and the skill that covers the local dashboard document the key. The skill uses a neutral example URL. `tw-dev-cli` is not in this repo; `tw-deploy` is the skill that already describes what the `dev run` dashboard links.
- `AddExternalService` is already on the pinned Aspire.Hosting 13.6.2, confirmed with `dotnet-inspect` against that assembly. No new package.
- An AppHost model test covers the link when the URL is set, no resource when it is unset, startup failure when it is invalid, and no resource in publish mode even when it is set. `dev build` is 0/0.

Review round 1 fix pass:

- The SigNoz subsection in `tw-deploy` follows the complete "Rules for new code" list, before Secrets and parameters. The yarp rule stays in that list.
- The Compose, Kubernetes, Azure Container Apps, and Azure Container Apps + Neon in-proc publish fixtures pass `Observability:SigNoz:Url` as an absolute `https` URL. Their output still does not name `signoz`.
- The AppHost Design paragraph for SigNoz follows the `Authentication:Entra:PublicOrigin` sentences and precedes public-host forwarding.

## Description

Steve, 2026-10-26: add a link from the Aspire dashboard to our local development SigNoz at
https://signoz.timewarp.work/, so developers can open it from the resource list next to the app.

The template ships to every generated app, so the URL is TimeWarp infrastructure and must not be
hard-coded in template files. The AppHost reads it from configuration. When it is set, the
dashboard shows a SigNoz resource with a clickable URL. When it is unset, nothing is added.

Aspire (pinned at `<AspireVersion>` 13.6.2) supports both pieces: an external service resource
(`builder.AddExternalService(name, url)`) and custom resource URLs (`WithUrl`, with a display text).
See aspire.dev, "Define custom resource URLs".

## Requirements

- **AppHost** (`source/container-apps/aspire/projects/aspire-app-host/program.cs`): read one
  configuration key, for example `Observability:SigNoz:Url`. When it is a valid absolute
  `https` URL, add a `signoz` resource whose dashboard URL reads "SigNoz". An invalid value fails
  AppHost startup with a clear message, not a silent skip. When the key is unset, nothing is
  added.
- **Run mode only.** Do not add the resource to publish or deploy output (Compose, Kubernetes,
  Azure Container Apps). Check that `aspire publish` output is unchanged.
- **No URL in template files.** Do not add the value to `appsettings*.json`. TimeWarp developers
  set it in AppHost user secrets or an environment variable
  (`Observability__SigNoz__Url=https://signoz.timewarp.work/`). Document the key in the AppHost
  Design region and in the `tw-dev-cli` or `tw-deploy` skill section that covers `dev run`,
  whichever owns the dashboard. Use a neutral example URL in the skill.
- No new package if `AddExternalService` is in the pinned Aspire.Hosting. Confirm with
  `dotnet-inspect`.

## Out of scope

- Sending telemetry to SigNoz (an OTLP exporter or collector fan-out next to the Aspire dashboard).
  File a follow-up if wanted.

## Checklist

- [x] Config key read, validated, and the resource added only when set
- [x] Run mode only; publish output unchanged
- [x] Design region and skill note
- [x] Test: an AppHost model test (existing `aspire-*` test pattern in `tests/tools/dev-cli-tests`
      or the AppHost tests) shows the resource and URL when set and no resource when unset
- [x] `dev build` 0/0
- [x] SigNoz skill subsection follows the complete rules list (review M1)
- [x] Publish fixtures pass a configured SigNoz URL and still omit `signoz` (review M2)
- [x] SigNoz Design paragraph follows the PublicOrigin sentences (review M3)

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-26)
- Implementation: task-work implementer (2026-10-26). `dotnet-inspect` found `AddExternalService` on Aspire.Hosting 13.6.2, so no package was added. `tw-dev-cli` is not in this repo; the dashboard note is in `tw-deploy`. `ganda repo audit` passed with a pre-existing `kanban-landed` warning for eight other claims that have no worktree.
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session not reported, max-turns 500, Done — 2026-10-25T19:24:14Z
- Review round 1 (2026-10-26): review oracle (claude-opus-5-5, headless), effort 2, roster general. 0 bug, 2 suggestion, 1 nit, all open (`review/round-1/merged.md`). A fix pass is needed for M1–M3, followed by round 2. The Goal restatement matches the task (no misread).
- Review oracle: review by reviewer-claude (claude, model claude-opus-5-5), session not reported, max-turns 120 — 2026-10-25T19:28:04Z
- Fix pass (2026-10-26): review M1–M3. The SigNoz subsection in `tw-deploy` now follows the complete rules list. The four in-proc publish fixtures pass `--Observability:SigNoz:Url=https://observability.example`. The AppHost Design paragraph for SigNoz follows the PublicOrigin sentences. `SigNozLinkModel` passed 6/6 and `Publish_Given_` passed 47/47. `./bin/dev build` reported 0 warnings and 0 errors.
- Audit: `kanban-landed` is an error while this claim's branch-tip kitchen differs from origin-home. `ganda repo audit --fix` lands that kitchen. It does not merge the task branch. The eight other claims with no worktree stay a warning.

## Results

The AppHost reads `Observability:SigNoz:Url`. An absolute `https` URL adds a run-mode external service named `signoz` whose dashboard link text is "SigNoz". A missing or blank value adds nothing. Any other value throws `InvalidOperationException` and names the key. Publish mode does not add the resource, including when the URL is set. Compose, Kubernetes, Azure Container Apps, and Azure Container Apps + Neon in-proc publish fixtures pass `--Observability:SigNoz:Url=https://observability.example` and their output still does not name `signoz`. No URL was added to `appsettings*.json`. `tw-deploy` documents the key with the example `https://observability.example`, in a subsection that follows the complete rules list.

### How to validate

Smoke:

```bash
cd tests/container-apps/aspire/aspire-tests
dotnet test -- --filter-class SigNozLinkModel
dotnet test -- --filter-class Publish_Given_
```

```bash
./bin/dev build
```

Expect:

- `SigNozLinkModel`: 6 passed. The set case has one `signoz` URL annotation whose target is `https://observability.example/traces` and whose display text is `SigNoz`. The unset case has no `signoz` resource. `http://observability.example`, `observability.example`, and `https://` each fail startup with a message that names `Observability:SigNoz:Url`. Publish mode with the valid URL set has no `signoz` resource.
- `Publish_Given_`: 47 passed. Compose, Kubernetes, Azure Container Apps, and Azure Container Apps + Neon publish with `Observability:SigNoz:Url` set to `https://observability.example` and still do not name `signoz`. The Neon `MissingParameterValueException` lines in that run are the existing missing-secret log, not a failure.
- `./bin/dev build`: `0 Warning(s)`, `0 Error(s)`.

To see the link on a running dashboard, set `Observability:SigNoz:Url` in the AppHost user secrets (or `Observability__SigNoz__Url`) to an absolute `https` URL and run `dev run`. The resource list shows `signoz` with a link labeled SigNoz. This walk did not start the dashboard.

## Review

- Round 1: Blocked — open M1, M2, M3

## Notes

Requested by Steve 2026-10-26.
