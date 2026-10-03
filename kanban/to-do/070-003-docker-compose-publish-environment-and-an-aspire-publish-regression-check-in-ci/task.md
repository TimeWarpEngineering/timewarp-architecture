# Docker Compose publish environment and an aspire publish regression check in CI

## Description

Child of 070. Add the Docker Compose publish target so `aspire publish` emits a runnable
`compose.yaml` for standalone hardware, and guard it with a CI check that runs `aspire publish`
and inspects the output.

## Requirements

1. **Package and environment.** Add `Aspire.Hosting.Docker` (13.6.x, the CPM pin moved forward
   with the other Aspire pins) and `builder.AddDockerComposeEnvironment(...)` in the AppHost.
   Confirm the API via the Aspire docs MCP (`deploy-to-docker-compose`, `docker-integration`);
   don't guess.
   - It must be **publish-only**: run mode (`dev run`) is unchanged.
   - Every template flag combination stays valid.
2. **Production-safe output** (the parent's cross-cutting list), verified by a test that runs the
   publish pipeline (or the model) and inspects `compose.yaml`:
   - no mock auth;
   - no browser-log forwarding;
   - no dashboard REPL;
   - secrets (postgres password, Entra) are `.env` parameters, not literals;
   - only the ingress is published to the host.
3. **Postgres.** Use a named volume for data and a secret password parameter, and define how
   migrations run in the Compose target. The options are the published migration bundle or
   script (task 155), run as a one-shot service or by hand; pick one and record it.
4. **Artifacts.** Decide whether generated output is committed or CI-only. The default is
   CI-only: upload it as a workflow artifact. Record the decision in the AppHost Design region. Do
   not create a `devops/` docs tree.
5. **CI regression check** in `.github/workflows/workflow.yml`, following the single-workflow,
   mode-aware `dev workflow` pattern; mirror sibling repos and don't invent a second workflow.
   - Run `aspire publish` for the Compose target.
   - Fail on errors and on any production-safety violation.
   - Upload the artifact.
   - Expose it through `dev`, for example `dev publish compose`, so it runs locally too.
6. **Runtime neutrality.** Nothing hard-codes the `docker` CLI (see task 277); go through the
   Aspire runtime setting.

## Checklist

- [ ] `Aspire.Hosting.Docker` + `AddDockerComposeEnvironment` (publish-only; flags valid)
- [ ] Production-safety test over the generated compose output
- [ ] Postgres volume, secret parameter and migration path decided and implemented
- [ ] Artifact policy decided (Design region)
- [ ] CI publish check + `dev` entry point
- [ ] Gates: `dev build` 0/0, `dev test`, `dev template-smoke`, `ganda repo audit`
- [ ] Do **not** start an AppHost or run containers; `aspire publish` (artifact generation) is fine
- [ ] Implementation review; host `open-pr`

## Notes

- After 070-002 (Dockerfiles gone), images come from the .NET SDK container build.
- Maintainer check after merge: on a Linux host or WSL, run `docker compose up` from the
  generated output and confirm the app comes up behind the ingress.

## Session

- Created: 2026-10-03 (rewrite of 070)
