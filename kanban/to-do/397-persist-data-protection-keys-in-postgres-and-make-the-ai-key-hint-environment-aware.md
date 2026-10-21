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

## Checklist

- [ ] Reproduce 1 on a deployed target (sign in, let the app scale to zero, come back) or with two web-server instances, and record the result
- [ ] When `postgres` is on: persist the key ring in Postgres (`PersistKeysToDbContext` on the app's DbContext, with a migration); set a stable application name
- [ ] When `postgres` is off: decide with Steve (file system with a documented volume, or leave the default and say so in `tw-deploy`)
- [ ] Keys protected at rest where the target supports it (decide with Steve; e.g. Key Vault on aca, none on compose), not required for the first cut
- [ ] The not-configured hint depends on the environment: Development keeps the user-secrets command; elsewhere it names `XAI__ApiKey` (environment variable or container-app secret) and, once task 398 lands, Admin → AI
- [ ] The hint never shows the user-secrets id outside Development
- [ ] Tests: key ring survives a web-server restart (integration); hint text per environment
- [ ] `tw-deploy` skill: where the key ring lives per target
- [ ] `#if postgres` regions intact; `dev template-smoke --matrix` passes

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-21)

## Notes

- Origin: Steve 2026-10-21, screenshot of "AI not configured" on the aca-neon deployment; asked whether the key should be Admin-configurable (tasks 398, 399).
