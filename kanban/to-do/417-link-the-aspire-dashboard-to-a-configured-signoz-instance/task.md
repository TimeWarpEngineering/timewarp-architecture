# Link the Aspire dashboard to a configured SigNoz instance

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

- [ ] Config key read, validated, and the resource added only when set
- [ ] Run mode only; publish output unchanged
- [ ] Design region and skill note
- [ ] Test: an AppHost model test (existing `aspire-*` test pattern in `tests/tools/dev-cli-tests`
      or the AppHost tests) shows the resource and URL when set and no resource when unset
- [ ] `dev build` 0/0

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-26)

## Notes

Requested by Steve 2026-10-26.
