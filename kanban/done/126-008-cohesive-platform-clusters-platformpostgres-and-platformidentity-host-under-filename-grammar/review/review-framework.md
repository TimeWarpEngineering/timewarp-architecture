# Review framework — task 126-008

**Date:** 2026-07-27
**Host task:** kanban/in-progress/126-008-cohesive-platform-clusters-platformpostgres-and-platformidentity-host-under-filename-grammar/
**Diff scope:** commit `49a2d1c3` — platform/postgres + platform/identity-host clusters
**Plan / brief:** Second grammar tree root WebPlatformTreeRoot; 12 file moves with -layer suffixes; template !postgres excludes; AGENTS + feature-placement skill
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** orchestrator grok-build 2026-07-27

## Ground rules

- Read-only on product code; write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts open
- Do not invent findings; zero issues is valid

## Claims to verify

- Generator emits platform globs; g.props not hand-edited beyond generator
- Membership guard scans platform/
- All 12 files moved with correct suffixes; namespaces unchanged
- template.json !postgres excludes updated; SmokeNoPostgres strips postgres/
- AGENTS.md + skill document features vs platform vs host
- Seam interfaces stayed in web-application/abstractions
