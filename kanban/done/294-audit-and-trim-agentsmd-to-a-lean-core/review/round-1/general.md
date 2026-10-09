# Round 1 — general
**Date:** 2026-10-10
**Scope reviewed:** branch task/294 vs master

## Summary

The diff holds up. Every pointer in the new AGENTS.md resolves: skills exist, TWA0004/0008/0010/0015/0016/0014 and TW0001 match the analyzer and editorconfig sources, and `./bin/dev --capabilities` lists `check-version` and `template-smoke`. The size numbers (3498 bytes, 54 lines, 470 words; before 33098/408/4000) reproduce exactly. The moved facts check out: TWA0005, TWE001/004/012-014 and the endpoint shims. The `;` comments sit above `### New Rules` and do not touch the release table. Only minor lost-knowledge points and typos remain.

## Issues

### Issue 1 — Severity: suggestion
- File: AGENTS.md:1
- Description: The old Platform packages section said CPM pins for platform packages equal the release `<Version>` and bump in the same commit (task 124 policy), and that you never leave both a ProjectReference and a PackageReference for the same consumer. The new file only says "`dev check-version` when shipping". The same-commit rule still lives in `Directory.Packages.props` comments, but nothing in the lean core tells an agent that a version bump needs the pins bumped in the same commit. That is a recurring footgun (see the "Run check-version before PR" memory).
- Suggestion: Add one clause to the PR-gates row, for example "`dev check-version` (bump `<Version>` and the platform pins in the same commit)".
- Status: open

### Issue 2 — Severity: nit
- File: skills/tw-feature-placement/references/co-located-jaribu-runfiles.md:70
- Description: The new "Host lanes" table drops the `TIMEWARP_TEST_PORT_BASE` override and the default ports (web=7000, web-http=7001, api=7255, yarp=8443) that the old Stack section gave. It points at `InProcTestPorts`, which is the real source, so this is minor. The override env var is no longer discoverable without reading that class.
- Suggestion: Mention `TIMEWARP_TEST_PORT_BASE` in the in-proc row, if `in-proc-test-ports.cs` still reads it.
- Status: open

### Issue 3 — Severity: nit
- File: kanban/to-do/294-audit-and-trim-agentsmd-to-a-lean-core/task.md:61
- Description: "beside this kitchen" (line 61) and "the host kitchen path" (line 79) look like a mistyped word, possibly "folder" or "task". Line 61 is outside the PR body, so it is not harmful.
- Suggestion: Fix the wording.
- Status: open

### Issue 4 — Severity: nit
- File: skills/tw-feature-placement/SKILL.md:257
- Description: The reflowed paragraph leaves a short orphan line ("aggregator wiring and its mandatory"). This is cosmetic only.
- Suggestion: Rewrap the paragraph.
- Status: open
