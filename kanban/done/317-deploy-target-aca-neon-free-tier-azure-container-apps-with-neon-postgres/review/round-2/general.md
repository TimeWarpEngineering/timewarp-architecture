# Round 2 — general
**Date:** 2026-10-21
**Scope reviewed:** fix delta `0bf297e17..c640fb928` (one commit, kanban paths excluded), re-verified against round-1 M1–M8 and scanned for new defects. Call sites of the changed APIs were read in `deploy-command.cs`, `deploy-migrate-command.cs`, and `aspire-deploy-preflight.cs`.

## Summary

The fix commit addresses all eight round-1 findings. The culture swap is gone. Decompiling Azure.Provisioning 1.6.0 shows that `BicepSyntax.Value(double)` formats with `CultureInfo.InvariantCulture`, so the new Design claim is true, and the CPU regex now rejects `0,25`. Both retry paths go through `PostgresRetryPolicy.Configure(builder, count, maxDelay)`, and `Resolve` has tests. aca-neon migrate resolves only the direct secret and skips `az login`. Its preflight detail line names the direct connection, and `MigrateNeonAsync` still reads the direct string from `preflight.Secrets`. CI uploads the aca-neon Bicep and SQL. `Server` is accepted as the host, and `Disabled` and URIs are refused without echoing the secret. The secret tests now pass real values and assert that only `Parameters__*` environment entries are written. The Entra placeholder is documented for every target. I found no new defects in the delta.

Re-run by this reviewer: `tests/tools/dev-cli-tests` `dotnet test -c Release` passed 273 of 273. `tests/container-apps/web/web-infrastructure-tests` `dotnet test -c Release --filter-class Resolve_Given_` passed 4 of 4. I did not re-run the Aspire publish suites. That change is a one-character regex tightening, and the decompile shows the literal is `0.25`.

## Issues

<!-- None. -->
