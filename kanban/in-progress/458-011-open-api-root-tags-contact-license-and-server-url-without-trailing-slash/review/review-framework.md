# Review framework — task 458-011

**Date:** 2026-11-26
**Host task:** kanban/in-progress/458-011-open-api-root-tags-contact-license-and-server-url-without-trailing-slash/
**Diff scope:** branch `task/458-011-open-api-root-tags-contact-license-and-server-url` vs `master` (3 commits: claim move, OpenAPI product change, land-implement). Product paths: `msbuild/open-api-document-metadata.props`, `skills/tw-web-api-contracts/SKILL.md`, api-server and web-server OpenAPI wiring, `foundation-server` OpenAPI transformers, web-server OpenAPI integration tests.
**Plan / brief:** Publish root `tags` (with descriptions on the web-server document), `info.contact.url` and `info.license` from repository metadata, and a server URL with no trailing slash. Document check: every operation tag is declared at the root.
**Effort:** 3 (general only)
**Reviewer roster:** general
**Session IDs:** review oracle grok session `01a215c9-dbc2-75f2-91ca-3573a507a588` (2026-11-26). Implementer session `df953187-d6b4-4f20-8f86-e8851d5da501` (recorded on task.md).

## Budget (by-diff)

- Lines changed: 864
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`
