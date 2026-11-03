# Disposition — task 469-003

**Date:** 2026-11-04
**Outcome:** accepted-exceptions
**Rounds:** 2
**Final open count:** 0

## Summary

Round 1 (effort 3, two general passes) found no behaviour change in the split of the AppHost and
Web.Server composition roots. It raised five findings: three stale-pointer suggestions, a
blank-line nit, and an RCS1163 nit for the all-services-off flag combination. The fix pass fixed
M1, M2, M3 and M5, and marked M4 wontfix. Round 2 (effort 2, general) re-verified the fixes against
commit 406fad697, found no new issues, and accepted the M4 rationale. `./bin/dev build` passes with
0 warnings and 0 errors.

## Exception log (if accepted-exceptions)

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M4 | nit | The flag combination with api, grpc and web all off is not in the smoke matrix. Its yarp-on form already failed on master (IDE0005). Supporting it needs a flag-set decision and a new matrix variant, which is outside a behaviour-preserving refactor. | implementer (fix pass), accepted by review oracle round 2 |

## Escalations

- None. Open before done (recorded in task.md Learnings): the Requirements item "`dev run` still
  starts" was not performed in the task worktree, because worktree AppHosts share the master
  UserSecretsId. An operator run in the master checkout closes it.
