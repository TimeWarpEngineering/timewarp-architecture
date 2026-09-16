# Review framework — task 232

**Date:** 2026-09-16
**Host task:** kanban/in-progress/232-update-aspire-to-the-latest-135x-across-sdk-hosting-packages-cli-and-service-defaults/
**Diff scope:** branch `task/232-update-aspire-to-the-latest-135x-across-sdk-hostin` vs `origin/master` (commits `97868694` chore(deps) bump Aspire train to 13.5.4; `7e9d67e7` docs(kanban) implementer results). Product files: `Directory.Packages.props`, `source/container-apps/aspire/projects/aspire-app-host/aspire-app-host.csproj`.
**Plan / brief:** One-train bump 13.5.3 → 13.5.4 (SDK, hosting, testing, CLI) plus matching EF preview `13.5.4-preview.1.26464.4`. ServiceDiscovery / Yarp / Http.Resilience to 10.10.0. Apply 13.5 breaking changes that actually touch this repo; keep `AspireUseCliBundle=false`.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** Grok review oracle 2026-09-16 (ganda task-work review node)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## What to verify

- Every `Aspire.*` pin is on the 13.5.4 series (EF hosting on matching preview); no leftover 13.5.3
- ServiceDiscovery / Resilience 10.10.0 is coherent (service-defaults + yarp)
- Breaking-change grep claims in `task.md` Results hold (ServiceProvider, PublishAsConnectionString, `aspire ps` flags, DotnetProjectResource, wait-edges, ingress ports)
- `AspireUseCliBundle=false` is an explicit, documented choice
- CI / docs claims: no pinned Aspire CLI in workflows; no `documentation/developer/guides/*`
- Implementer Results match the actual diff
