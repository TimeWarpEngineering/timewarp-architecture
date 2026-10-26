# Round 2 — general
**Date:** 2026-10-26
**Scope reviewed:** 53e4f4f24 vs c1a0d6eaa (`skills/tw-deploy/SKILL.md`, AppHost `program.cs` Design region, four publish test fixtures)

## Summary

The fix pass addresses all three round-1 findings and adds no new behavior. The `tw-deploy` rules list is whole again, and the SigNoz subsection follows it. The Compose, Kubernetes, ACA, and ACA + Neon in-proc publish fixtures now pass `--Observability:SigNoz:Url=https://observability.example`. That URL passes the AppHost validation, so the `signoz` absence checks now depend on the `IsRunMode` gate. The Design paragraph order is fixed. Re-run in this worktree: `Publish_Given_` 47/47 passed, `SigNozLinkModel` 6/6 passed.

## Prior findings

- M1 — fixed. `skills/tw-deploy/SKILL.md:287` now holds the yarp rule at the end of the Rules list, and `### SigNoz link on the local dashboard` follows it.
- M2 — fixed. Each `PublishInProcAsync` passes `SigNozUrlArgument`. The URL is a valid absolute https URL, so publish reaches the run-mode gate (`program.cs:929`) and does not throw. When `TIMEWARP_*_OUTPUT` names external output (`dev publish`), the URL comes from the operator's configuration. The in-proc lane is the one that guards the gate.
- M3 — fixed. `program.cs:107-118`: the PublicOrigin sentences follow PublicUrl, then SigNoz, then public-host forwarding.

## Issues

None.
