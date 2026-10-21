# Round 1 — general
**Date:** 2026-10-22
**Scope reviewed:** `master...task/397-persist-data-protection-keys-in-postgres-and-make`. That is all product files plus the call sites around them: PostgresDbModule, AggregateDbContext, the pending-migrations probe and the boot wait, XaiChatUpstream, dev-cli XaiPreflight, the template.json `(!postgres)` exclusions, and the tw-deploy Key Vault claims.

## Summary

When a Postgres connection string is set, `PostgresDbModule` now registers `AddDataProtection().SetApplicationName(DataProtectionDefaults.ApplicationName).PersistKeysToDbContext<PostgresDbContext>()`. The new migration creates `data_protection.keys`. With no connection string the module returns before that, and the ASP.NET default file-system ring stays. The Ask hint is now `XaiChatDefaults.SetupHint(IHostEnvironment.IsDevelopment())`. Development keeps the user-secrets command. Every other environment gets `Set XAI__ApiKey as an environment variable or a container-app secret.`, and the client falls back to that sentence. The risk is low. I found no defect.

## Verification (re-run by the reviewer; implementer claims were not taken on trust)

- `web-server-integration-tests -- --filter-class KeyRing_Given_`: 2/2 passed against Testcontainers Postgres. Neither test skipped.
- Full `web-server-integration-tests`: 352 total, 351 passed, 1 skipped, 0 failed.
- `Boot_Given_`, with the database one migration behind (`AddDataProtectionKeys` pending): passed. The `XmlKeyManager[35]` warning is logged only after the seed wait clears. The key ring therefore loads after migration, and no EF Error is logged for the missing table. A web-server that boots before `dev deploy migrate` waits; it does not fail on the key ring.
- Hint classes: `GetAgentChatConfigurationHint_Given_` 6/6, `CompleteAgentChatHandler` 5/5, `AgentAskReadiness_Should_` 11/11, `AskStarterPrompts_Should_` 7/7.
- `./bin/dev template-smoke --matrix`: SmokeDefault, SmokeNoPostgres, SmokeNoApi, SmokeNoWeb, SmokeNoGrpc, and SmokeNoYarp all built (`Template smoke SUCCEEDED`, exit 0). The `<!--#if (postgres) -->` package references and the new `(!postgres)` exclusion entry for the test file hold.
- `ganda repo audit`: passes all checks.

## Checks with no finding

- Only web-server issues and reads the auth cookie (`cookie-browser-session-service-server.cs`, `web-server/program.cs`). No other host needs the shared ring.
- `DataProtectionDefaults.ApplicationName` equals template `sourceName` (`TimeWarp.Architecture`), so `dotnet new` rewrites it per generated app, as the Design region says.
- A save through `AggregateDbContext` works for the non-aggregate `DataProtectionKey`. The restart test shows `data_protection.keys` has rows, and the second host unprotects. The `ApplicationDiscriminator` assertion and the row count also rule out a false pass from the shared in-process `~/.aspnet` file ring.
- The tw-deploy statements were checked against `skills/tw-deploy` and its references. `aca` keeps the Postgres connection string in a Key Vault that Aspire provisions, and `aca-neon` provisions no Key Vault.
- No `XaiChatDefaults.SetupCommand` call site outside Development remains. The mock factory still uses the Development command, as its Design region documents, and `dev run` (XaiPreflight) still prints that command.
- No suppression, stub, or always-true guard was added to make a gate pass. The only `#pragma` is `CA2100` on `CREATE DATABASE` with a Guid-minted name. It matches the existing pattern in `site-settings-boot-pending-migrations-tests.cs` and the web-infrastructure-tests.

## Issues

None.
