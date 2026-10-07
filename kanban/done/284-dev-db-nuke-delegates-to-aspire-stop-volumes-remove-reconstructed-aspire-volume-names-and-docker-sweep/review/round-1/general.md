# Round 1 — general
**Date:** 2026-10-07
**Scope reviewed:** branch vs master: `tools/dev-cli/services/db-nuke.cs`, `tools/dev-cli/endpoints/db-nuke-command.cs`, `tests/tools/dev-cli-tests/db-nuke-tests.cs`, `tests/container-apps/aspire/aspire-tests/{aspire-tests.csproj,postgres-volume-model-tests.cs}`, `AGENTS.md`; call sites in `db-group.cs`, `db-reset-command.cs`, `aspire-cli.cs`, `aspire-run.cs`, `run-command.cs`, `dev-cli-tests.csproj`.

## Summary

Nuke is now repo-root/AppHost resolution → shared Aspire CLI ≥13.6 guard → `--yes` gate (prints the
exact `aspire stop` line, exit 1) → `aspire stop --apphost <csproj> --force --volumes` with exit code
propagated → adopted-volume hint naming `ASPIRE_CONTAINER_RUNTIME` (default `docker`). No container
CLI is invoked from code. All hash/prefix/sweep/container-cleanup code, its tests, the aspire-tests
compile-include + `DEV_CLI_SOURCE` and the prefix-parity test are gone; the real AppHost-model volume
tests remain. Purpose/Design regions in every edited file match the new behaviour; neighbouring
regions (db-group, db-reset, aspire-cli, aspire-run, run-command) were already accurate. AGENTS.md
matches the requirement wording. Low risk.

Verified independently: dev-cli-tests 87/87; `ganda repo audit` passes;
`grep -rn "VolumeNameGenerator\|SHA256" tools/dev-cli` empty; no remaining references to
`VolumeNamePrefix`, `PlanContainerCleanup`, `VolumeContainer` or `DEV_CLI_SOURCE`.

## Issues

None.
