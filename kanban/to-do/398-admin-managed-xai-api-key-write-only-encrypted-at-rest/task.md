# Admin-managed xAI API key (write-only, encrypted at rest)

## Description

Make the site's xAI API key settable from Admin, next to Assistant name (task 361) and the
accepted Entra tenants (task 306), so a deployed app can turn Ask on without the Azure portal or
a redeploy. The key is a secret, so it is handled unlike the other SiteSettings values.

Depends on task 397 (durable Data Protection key ring).

## Goal (implementer restatement)

An administrator can turn Ask on from Admin → AI: set, replace, or clear the site xAI API key. The browser only ever sees whether Ask has a key, the last four characters of an admin key, and who set it and when. If a stored row cannot be unprotected, Ask is not configured and Admin says the stored key has to be replaced. The plaintext is protected with ASP.NET Data Protection under the purpose `TimeWarp.Architecture.XaiApiKey` and stored in Postgres (`secrets.protected_secrets`), on the same durable key ring as task 397. That store is a named protected secret, so a later per-user key can reuse it.

When `XAI:ApiKey` is set on the host, Ask uses that value and Admin → AI is read-only ("managed by the host"). A completion reads the effective key once, so an admin change applies without a restart, and a database failure becomes "not configured" for Ask instead of a 500. Status, set, and clear answer 503 when the store throws. `settings.write` is required to read the status or change the key. Set, replace, and clear write an audit line whose action comes from the store (inserted or replaced, or clear) and names the principal, never the key or its suffix.

The copy-paste setup text stays the exact user-secrets command in Development, and the host-variable sentence everywhere else. "Or open Admin → AI." is a separate sentence and appears only when this host can store an admin key. It is not on Admin → AI and not in a 503 detail. Without postgres, or with no connection string, Admin → AI only shows how to set the host value. Host configuration still wins in every variant. An open tab reloads Ask readiness after a successful set or clear.

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
- Review oracle: review by reviewer-claude (claude, model claude-opus-5-5), session not reported, max-turns 200 — 2026-10-21T22:14:04Z
- Fix pass (implement-fix-2): addressed review round 1 M1–M12. Store failures on resolve return null and status/set/clear return 503. The Admin sentence is a separate field and only when the host can store a key. Configured follows a trial unprotect (`needsReplacement`). A successful set or clear reloads Ask. The store write result is the audit action, and a unique violation retries as replace. One completion reads the key once. The admin integration test captures the key the chat client receives. Migration class is `AddProtectedSecrets`. The password field uses `new-password`. 2026-10-22

## Results

Administrators set, replace, and clear the site xAI API key on Admin → AI (`/Admin/AI`). The browser receives status only: configured, last four characters, who set it, and when. A stored row that cannot be unprotected is `needsReplacement`: Ask is not configured, and Admin says the stored key has to be replaced. The plaintext is protected with purpose `TimeWarp.Architecture.XaiApiKey` and stored in `secrets.protected_secrets`. `XAI:ApiKey` on the host wins, Admin → AI is then read-only ("Managed by the host"), and writes return 409. A completion reads the effective key once (`BeginKeyResolution` before the configured check), so a replacement is used without a restart. A store failure (missing table, database down) makes Ask not configured and makes status, set, and clear return 503. Without postgres, or with no connection string, set and clear return 503 and Admin → AI only shows how to set the host value.

Set, replace, and clear write `Site xAI API key {Action} by {PrincipalId}`. The action comes from the store write (`set` when the row was inserted, `replace` when it already existed or a concurrent insert lost the race, `clear` on delete). The line does not include the key or its suffix. `SiteXaiApiKey_Given_` asserts those three lines. Anonymous callers get 401. A member gets 403. `settings.read` does not see this.

The copy-paste block is the exact user-secrets command in Development, and the `XAI__ApiKey` sentence everywhere else. `Or open Admin → AI.` is `AdminHint`, rendered outside that block, and only when this host can store an admin key. It is absent from Admin → AI and from 503 details. A successful set or clear reloads Ask readiness in an open tab. A failed save does not.

The Admin → AI page was not clicked in a browser. The HTTP round trip (including two concurrent first writes and a completion that captures the key), the encrypted row, the status JSON, and the Ask HtmlRenderer were. The Ask Playwright classes expect the exact Development command and compiled under `./bin/dev build`; they were not executed.

### How to validate

**Smoke:**

```bash
cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Debug --nologo -- --filter-class XaiApiKeyAdmin_
cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Debug --nologo -- --filter-class XaiChatUpstream_Given_
cd tests/container-apps/web/web-jaribu-tests && dotnet test -c Debug --nologo -- --filter-class SiteXaiApiKey_Given_
cd tests/container-apps/web/web-jaribu-tests && dotnet test -c Debug --nologo -- --filter-class XaiApiKeyStatus_Given_
cd tests/container-apps/web/web-jaribu-tests && dotnet test -c Debug --nologo -- --filter-class GetAgentChatConfigurationHint_Given_
cd tests/container-apps/web/web-jaribu-tests && dotnet test -c Debug --nologo -- --filter-class CompleteAgentChatHandler_Given_
cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Debug --nologo -- --filter-class AgentAskReadiness_Should_
./bin/dev build
./bin/dev template-smoke
ganda repo audit
```

**Expect:** `XaiApiKeyAdmin_` passes 1. Anonymous is 401, a member is 403, the admin response suffix is `aaaa` then `bbbb`, the JSON has no `apiKey` property, the ciphertext does not contain the plaintext, a second host unprotects the replacement, a host `XAI:ApiKey` is what Ask resolves and PUT returns 409, and DELETE leaves Ask with no key. Two concurrent first PUTs both return 200, and the completion client receives the key from set and again from replace. `XaiChatUpstream_Given_` passes 11, and one turn unprotects the stored key once. `SiteXaiApiKey_Given_` passes 6: an unreadable row needs replacement, a store failure is 503 for status/set/clear and null for Ask, and the audit lines are `set`, `replace`, and `clear` plus the principal id. `XaiApiKeyStatus_Given_` passes 2 and the JSON name list includes `needsReplacement`. Hint filters pass 6 and 5: `SetupCommand` is the exact command or the host-variable sentence, and `AdminHint` is `Or open Admin → AI.` only when storage is available. `CompleteAgentChatHandler_Given_` passes 5, and a 503 detail has no Admin sentence. `AgentAskReadiness_Should_` passes 14: the Admin sentence is outside the copy block, a successful save reloads Ask, and a failed save does not. `./bin/dev build` is 0 warnings and 0 errors. Template smoke prints `Template smoke SUCCEEDED` for SmokeDefault. Audit prints `Repository passes all audit checks.`

### Review

- Round 1 (effort 3, general): M1–M12 implemented on this id in the fix pass. Host review round 2 follows.
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`.

## Learnings

- Misread (review round 1): the Goal says the without-postgres behavior is "the decision already written on this task". The checklist item reads "Without `postgres`: decide with Steve (host configuration only, Admin shows how to set it)". That parenthetical is a proposal awaiting Steve, not a recorded decision, and no Note records Steve deciding it. The implementer built the proposal, which is reasonable, but it should have been surfaced as an open operator confirmation instead of described as decided. Separately, the restatement says the no-storage Admin page "only shows how to set the host value". The implementation also tells the admin to "open Admin → AI" there (review M2). why: review oracle round 1, 2026-10-22
- An `AsyncLocal` value assigned inside `IsConfiguredAsync` is not visible to the caller after the await, including when the assignment happens before that method's first await. The completion handler opens the slot (`BeginKeyResolution`) on its own flow, and the check fills the object the caller already holds. why: fix pass, one unprotect per turn, 2026-10-22

## Review

- Round 1: Blocked — open M1, M2, M3, M4, M5, M6, M7, M8, M9, M10, M11, M12

## Notes

- Origin: Steve 2026-10-21, "should this be configurable from Admin like Assistant name? And Tenant Ids".
- Task 399 (BYOK, per-user keys) builds on the same encrypted-secret storage; keep the storage reusable for it.
