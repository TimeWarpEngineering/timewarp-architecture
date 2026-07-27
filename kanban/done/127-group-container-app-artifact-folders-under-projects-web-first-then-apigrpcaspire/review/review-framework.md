# Review framework — task 127

**Date:** 2026-07-28
**Host task:** kanban/in-progress/127-group-container-app-artifact-folders-under-projects-web-first-then-apigrpcaspire/
**Diff scope:**
- Stage 1 (already dispositioned clean in round 1): `267b4523` + `ad19d511`
- Stage 2 (this round): `156ccb72` (api) + `f62064da` (grpc) + `6e049ff1` (aspire) + `e5f5b4a1` (docs/kanban)
**Plan / brief:** Group container-app artifact folders under `projects/`. Stage 1 web done; Stage 2 api → grpc → aspire; yarp left flat.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** orchestrator (2026-07-28); stage 2 implementer 019fa490-ab21-7310-9b6f-74c01a452e2f

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
- Focus for round 2: residual old api/grpc/aspire paths, relative depth errors, Dockerfile paths, aspire.config + dev-cli, ServiceNames untouched, yarp still flat
