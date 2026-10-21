# Persist Data Protection keys in Postgres and make the AI-key hint environment-aware

## Description

Two problems found on 2026-10-21 while testing the `aca-neon` deploy.

1. **No Data Protection key storage.** The source has no `AddDataProtection` / `PersistKeysTo…`,
   so web-server keeps the default key ring on the container's file system. On Azure Container
   Apps with min replicas 0, every cold start gets a fresh key ring. Expected consequences (verify
   on a deployment): auth cookies and anything else protected with Data Protection stop validating
   after each idle period, so users are signed out. More than one replica would not share keys
   either. Task 398 (Admin-managed xAI key, encrypted at rest) depends on a durable key ring.
2. **The AI-not-configured hint is wrong off a developer machine.** Ask shows
   `dotnet user-secrets set "XAI:ApiKey" "<your-xai-key>" --id <user-secrets-id>`
   (`XaiChatContracts.SetupCommand`, `source/container-apps/web/features/agent-chat/xai-chat-contracts.cs`).
   A deployed app runs as Production, which never reads user secrets, so that command does nothing
   there.

## Goal (implementer restatement)

Stop web-server from minting a new ASP.NET Data Protection key ring on every container start, and stop the Ask "not configured" hint from telling a deployed user to run a user-secrets command.

Acceptance:

1. With postgres configured, the key ring is stored on the app's `PostgresDbContext` (migration included) under a stable application name, so a restarted web-server can unprotect a payload the previous process protected.
2. Without postgres, or with no connection string, the ASP.NET file-system ring stays. `tw-deploy` says cookies then do not survive a new container or a second replica.
3. No certificate or Key Vault wraps the key XML. `tw-deploy` says the database's own encryption is the at-rest protection, and names a Key Vault protector as a possible follow-up.
4. Development keeps the user-secrets command. Every other environment names `XAI__ApiKey` as an environment variable or a container-app secret and never includes the user-secrets id. Admin → AI waits for task 398.
5. Tests cover a restart against one database and the hint text per environment. `#if postgres` stays valid and `dev template-smoke --matrix` passes.

The cold-start failure is recorded with two isolated key directories that cannot unprotect each other's payload. The fix is recorded with two sequential web-server hosts against one migrated database. A live aca-neon scale-to-zero is not part of this walk.

## Checklist

- [x] Reproduce 1 on a deployed target (sign in, let the app scale to zero, come back) or with two web-server instances, and record the result
- [x] When `postgres` is on: persist the key ring in Postgres (`PersistKeysToDbContext` on the app's DbContext, with a migration); set a stable application name
- [x] When `postgres` is off: leave the ASP.NET default key ring and say so in `tw-deploy` (Decided: Steve 2026-10-22)
- [x] Keys protected at rest: out of scope for this task (Decided: Steve 2026-10-22); note it in `tw-deploy` as a possible follow-up
- [x] The not-configured hint depends on the environment: Development keeps the user-secrets command; elsewhere it names `XAI__ApiKey` (environment variable or container-app secret) and, once task 398 lands, Admin → AI
- [x] The hint never shows the user-secrets id outside Development
- [x] Tests: key ring survives a web-server restart (integration); hint text per environment
- [x] `tw-deploy` skill: where the key ring lives per target
- [x] `#if postgres` regions intact; `dev template-smoke --matrix` passes

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-21)
- Implementation: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-22)
- Audit: `ganda repo audit --fix` set the executable bit on `get-agent-chat-configuration-tests.cs` after the first commit's hook reported `runfile-executable`.

## Notes

- Origin: Steve 2026-10-21, screenshot of "AI not configured" on the aca-neon deployment; asked whether the key should be Admin-configurable (tasks 398, 399).

## Results

When a Postgres connection string is set, web-server calls `AddDataProtection`, `SetApplicationName(DataProtectionDefaults.ApplicationName)`, and `PersistKeysToDbContext<PostgresDbContext>`. The application name is `TimeWarp.Architecture`, the template source name, so `dotnet new` rewrites it with the generated app. Migration `20261021184129_AddDataProtectionKeys` creates `data_protection.keys` (`integer` id, nullable `text` FriendlyName, required `text` Xml). It does not run `CREATE EXTENSION`. With postgres off, or with the flag on and no connection string, `PostgresDbModule` returns before that registration and the ASP.NET file-system ring stays. The key XML is stored as text. `tw-deploy` says the database's own encryption is the at-rest protection and names a certificate or Key Vault protector as a possible follow-up. `aca-neon` min replicas 0 is called out there and in the Neon scale-to-zero note.

`XaiChatDefaults.SetupHint` keeps the user-secrets command in Development, including the user-secrets id. Every other environment uses `Set XAI__ApiKey as an environment variable or a container-app secret.` Client fallbacks use that deployed sentence when the server omits the text. Task 398 still appends "Admin → AI" when that page exists. `dev run` still prints the Development command.

The cold-start failure and the restart fix both ran against Docker (Testcontainers `postgres:16`). Neither test skipped. A live aca-neon scale-to-zero was not run.

- Two file-system rings with the same application name cannot unprotect each other's payload (`CryptographicException`). `KeyRing_Given_.Separate_Key_Directories_Should_Fail_Unprotect` passed.
- Two sequential web-server hosts against one migrated database: the second host unprotects `auth-cookie-stand-in`, the application discriminator is `TimeWarp.Architecture`, and `data_protection.keys` has rows. `KeyRing_Given_.Second_Web_Server_Should_Unprotect_What_The_First_Protected` passed (about 6.3s).
- `SiteSettingsBootPendingMigrations_` passed. The log names pending migration `20261021184129_AddDataProtectionKeys` and records no Error. XmlKeyManager warning 35 (no XML encryptor) is the warning accepted while at-rest wrapping stays out of scope.

Hint tests passed: `GetAgentChatConfigurationHint` 6, `CompleteAgentChatHandler` 5, `AgentAskReadiness` 11, `AskStarterPrompts` 7.

`./bin/dev template-smoke --matrix` exited 0 (`Template smoke SUCCEEDED`). `ganda repo audit` passed 32 checks and failed 0. The commit hook then reported `runfile-executable` on the new hint runfile, which was still untracked when that audit ran. `ganda repo audit --fix` set its executable bit, and the follow-up audit passed 32 and failed 0.

### How to validate

**Smoke:**

```bash
cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Debug --nologo -- --filter-class KeyRing_Given_
cd tests/container-apps/web/web-jaribu-tests && dotnet test -c Debug --nologo -- --filter-class GetAgentChatConfigurationHint
cd tests/container-apps/web/web-jaribu-tests && dotnet test -c Debug --nologo -- --filter-class CompleteAgentChatHandler
cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Debug --nologo -- --filter-class AgentAskReadiness
./bin/dev template-smoke --matrix
ganda repo audit
```

**Expect:** the key-ring filter reports 2 passed. Separate directories throw `CryptographicException`. The second web-server returns `auth-cookie-stand-in`. Development hint text is `XaiChatDefaults.SetupCommand` and contains the user-secrets id. Production, Testing, Staging, and a blank server string are `Set XAI__ApiKey as an environment variable or a container-app secret.` and do not contain `0e53fdd3-6f93-4d5a-9c86-040621f7929e` or `user-secrets`. Template smoke prints `Template smoke SUCCEEDED`. Audit prints `Repository passes all audit checks.`
