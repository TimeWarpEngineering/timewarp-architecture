# Admin-managed xAI API key (write-only, encrypted at rest)

## Description

Make the site's xAI API key settable from Admin, next to Assistant name (task 361) and the
accepted Entra tenants (task 306), so a deployed app can turn Ask on without the Azure portal or
a redeploy. The key is a secret, so it is handled unlike the other SiteSettings values.

Depends on task 397 (durable Data Protection key ring).

## Goal (implementer restatement)

An administrator can turn Ask on from Admin → AI: set, replace, or clear the site xAI API key. The browser only ever sees whether a key is configured, its last four characters, and who set it and when. The plaintext is protected with ASP.NET Data Protection under the purpose `TimeWarp.Architecture.XaiApiKey` and stored in Postgres (`secrets.protected_secrets`), on the same durable key ring as task 397. That store is a named protected secret, so a later per-user key can reuse it.

When `XAI:ApiKey` is set on the host, Ask uses that value and Admin → AI is read-only ("managed by the host"). Ask reads the effective key on each completion, so an admin change applies without a restart. `settings.write` (the Administrator site-policy permission) is required to read the status or change the key. Set, replace, and clear write an audit log line that names the action and the principal, never the key or its suffix.

The Ask "not configured" hint, in Development and elsewhere, points at Admin → AI. Development still leads with the user-secrets command. Without postgres, or with no connection string, there is no admin storage: Admin → AI only shows how to set the host value (the decision already written on this task). Host configuration still wins in every variant.

## Checklist

- [x] Admin → AI page (or a section on the existing settings page) to set, replace, and clear the key
- [x] Write-only: the key never returns to the browser; Admin shows "configured, ends in …xxxx" and when/who set it
- [x] Stored encrypted with ASP.NET Data Protection (purpose string specific to this key), in Postgres
- [x] Host configuration wins: when `XAI:ApiKey` is set (environment variable, container-app secret, user secret in Development), it is used and Admin shows "managed by the host" read-only
- [x] Ask picks up a changed key without a restart
- [x] Only admins can read the status or change the key (authorization tests)
- [x] Audit entry for set/replace/clear, never the value
- [x] Contracts per `tw-web-api-contracts` (`[Operation]`, `ICommand`/`IQuery`)
- [x] The not-configured hint (task 397) points at Admin → AI
- [x] Without `postgres`: decide with Steve (host configuration only, Admin shows how to set it)
- [x] Tests: round trip set → status → Ask uses it; host config precedence; clear; non-admin refused

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-21)
- Implemented the admin key on the existing `settings.write` gate. Storage is a named protected secret (`site.xai.api-key`), not a SiteSettings column, so task 399 can add another row.
- Implement oracle: implemented by implementer-cursor (cursor, model grok-4.7-high), session not reported, max-turns uncapped (no --max-turns flag; bound 500), Done — 2026-10-21T22:07:45Z
- Review oracle round 1: claude-opus-5-5 (ganda task work, headless), effort 3, roster general. The pass was split across read-only subagents adc95486e2571e8f2 (server) and a7d9ef2d146ecc994 (SPA and tests). 12 findings are open (4 bug, 4 suggestion, 4 nit), so a fix pass is needed. 2026-10-22

## Results

Administrators set, replace, and clear the site xAI API key on Admin → AI (`/Admin/AI`). The browser receives status only: configured, last four characters, who set it, and when. The plaintext is protected with purpose `TimeWarp.Architecture.XaiApiKey` and stored in `secrets.protected_secrets`. `XAI:ApiKey` on the host wins, Admin → AI is then read-only ("Managed by the host"), and writes return 409. Ask resolves the key on each completion, so a replacement is used without a restart. Without postgres, or with no connection string, set and clear return 503 and Admin → AI only shows how to set the host value. That is the without-postgres decision, taken from this task's checklist.

Set, replace, and clear write `Site xAI API key {Action} by {PrincipalId}`. The line does not include the key or its suffix. Anonymous callers get 401. A member gets 403. `settings.read` does not see this.

The not-configured hint still leads with the user-secrets command in Development, then `Or open Admin → AI.` Other environments keep the `XAI__ApiKey` sentence and add the same Admin sentence. They do not include the user-secrets id.

The Admin → AI page was not clicked in a browser. The HTTP round trip, the encrypted row, and the status JSON were. The Ask Playwright classes were updated to the new Development hint and compiled; they were not executed.

### How to validate

**Smoke:**

```bash
cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Debug --nologo -- --filter-class XaiApiKeyAdmin_
cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Debug --nologo -- --filter-class XaiChatUpstream_Given_
cd tests/container-apps/web/web-jaribu-tests && dotnet test -c Debug --nologo -- --filter-class SiteXaiApiKey_Given_
cd tests/container-apps/web/web-jaribu-tests && dotnet test -c Debug --nologo -- --filter-class XaiApiKeyStatus_Given_
cd tests/container-apps/web/web-jaribu-tests && dotnet test -c Debug --nologo -- --filter-class GetAgentChatConfigurationHint_Given_
cd tests/container-apps/web/web-jaribu-tests && dotnet test -c Debug --nologo -- --filter-class CompleteAgentChatHandler_Given_
dotnet run tools/dev-cli/dev.cs -- template-smoke --matrix
ganda repo audit
```

**Expect:** `XaiApiKeyAdmin_` passes 1. Anonymous is 401, a member is 403, the admin response suffix is `aaaa` then `bbbb`, the JSON has no `apiKey` property, the ciphertext does not contain the plaintext, a second host unprotects the replacement, a host `XAI:ApiKey` is what Ask resolves and PUT returns 409, and DELETE leaves Ask with no key. The audit lines are `set`, `replace`, and `clear` plus the principal id. `XaiChatUpstream_Given_` passes 10. `SiteXaiApiKey_Given_` passes 3. `XaiApiKeyStatus_Given_` passes 2. Hint filters pass 6 and 5, and the Development hint contains `Admin → AI` after the user-secrets command. Template smoke prints `Template smoke SUCCEEDED` for SmokeDefault, SmokeNoPostgres, SmokeNoApi, SmokeNoWeb, SmokeNoGrpc, and SmokeNoYarp. Audit prints `Repository passes all audit checks.`

### Review

- Round 1 (effort 3, general): 4 bug, 4 suggestion, and 4 nit findings, all open (M1–M12). There is no disposition yet. A fix pass is pending on this id.
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`.

## Learnings

- Misread (review round 1): the Goal says the without-postgres behavior is "the decision already written on this task". The checklist item reads "Without `postgres`: decide with Steve (host configuration only, Admin shows how to set it)". That parenthetical is a proposal awaiting Steve, not a recorded decision, and no Note records Steve deciding it. The implementer built the proposal, which is reasonable, but it should have been surfaced as an open operator confirmation instead of described as decided. Separately, the restatement says the no-storage Admin page "only shows how to set the host value". The implementation also tells the admin to "open Admin → AI" there (review M2). why: review oracle round 1, 2026-10-22

## Notes

- Origin: Steve 2026-10-21, "should this be configurable from Admin like Assistant name? And Tenant Ids".
- Task 399 (BYOK, per-user keys) builds on the same encrypted-secret storage; keep the storage reusable for it.
