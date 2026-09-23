# Round 1 — merged findings
**Date:** 2026-09-23
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 1 |

## Issues

### M1 — Severity: nit — Status: wontfix
- File: tools/dev-cli/services/template-smoke-harness.cs:716
- Description: `TIMEWARP_TEST_PORT_BASE` is a second string literal in dev-cli, duplicating
  `InProcTestPorts.EnvironmentVariableName`.
- Suggestion: harness assertion that the generated app's `in-proc-test-ports.cs` contains the
  literal (dev-cli cannot reference the testing library).
- Source: general
- Disposition notes: wontfix on this task (review oracle). The cross-project reference is
  structurally impossible, both sites name the other in their Design comments, and divergence
  is self-diagnosing: the generated hosts would fall back to 7000/7255, and the concurrent
  `dev test` + `dev template-smoke` gate recorded in task.md "How to validate" fails with a
  teaching error that names the variable. A textual assertion would need a full template-smoke
  re-run to prove and adds a third copy of the name; not worth it for a nit.

## Duplicates / conflicts

- None (single reviewer).
