# Review framework — task 127

**Date:** 2026-07-27
**Host task:** kanban/in-progress/127-group-container-app-artifact-folders-under-projects-web-first-then-apigrpcaspire/
**Diff scope:** commits `267b4523` + `ad19d511` (stage 1 web → `web/projects/`); base = parent of `267b4523`
**Plan / brief:** Group container-app artifact folders under `projects/`. Stage 1 only: six web project folders under `web/projects/`; path reference sweep; docs/skills; gates green. Stage 2 (api/grpc/aspire) deferred pending maintainer review.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** orchestrator session (2026-07-27); implementer subagent 019fa480-51e3-7e03-ba05-e4a06821691c

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
- Focus: missed path references, broken relative depths, template.json excludes, accidental renames of ServiceNames/project names, docs drift, residual Class A hits
