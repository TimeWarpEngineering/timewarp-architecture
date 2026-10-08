# Disposition — task 287

**Date:** 2026-10-09
**Outcome:** accepted-exceptions
**Rounds:** 2
**Final open count:** 0

## Summary

Effort 3, roster general (by-diff budget, 1947 lines). Round 1 raised 7 findings (3 bug, 2 suggestion, 2 nit): `dev deploy --help` hidden by the new route group, a broken pwsh escape in the Key Vault grant, a lost firewall-cleanup failure when Ctrl+C propagates, a bash `<` in the printed migrate command, and a silent port-forward timeout were fixed. Round 2 verified all fixes and raised 2 doc nits, both fixed. Final: 7 fixed (3 bug, 1 suggestion, 3 nit), 2 wontfix, 0 open. Gates after fixes: `dev build` 0/0, dev.cs runfile build clean, dev-cli-tests 194/194, `ganda repo audit` pass.

## Exception log

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M5 | suggestion | The connection string in bundle argv is visible in the operator's local process list while it runs. `--connection` is the EF bundle's documented interface and the task requirement; the process is short-lived and the CLI never prints the value. The trade-off is recorded in the `deploy-operate.cs` Design region | review oracle (round 2 concurred) |
| M6 | nit | Migrate on kubernetes resolves all four deploy parameters, not just `k8s-namespace`. The template commits all four, so the cost is zero, and it keeps "resolved exactly as `dev deploy`" literal | review oracle (round 2 concurred) |

## Escalations

- None. Not verified: the Key Vault `--query` quoting through `az.cmd` on Windows (verified in pwsh 7 on Linux).
