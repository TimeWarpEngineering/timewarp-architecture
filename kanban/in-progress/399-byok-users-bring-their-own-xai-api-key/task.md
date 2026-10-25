# BYOK: users bring their own xAI API key

## Description

Let each signed-in user supply their own xAI API key for Ask (bring your own key), so usage
bills to their xAI account. Builds on the encrypted-secret storage from task 398 and the durable
key ring from task 397.

## Goal (implementer restatement)

Each signed-in user can save, replace, and clear their own xAI API key on Profile. The key is
write-only: the page and the API show "ends in …xxxx" and never return the secret. Ciphertext is
stored as `user.{userId}.xai.api-key` and protected with a Data Protection purpose that includes
that user id, so one user's protector cannot open another's row.

Ask resolves a key on every completion, with no process restart. The host `XAI:ApiKey` wins in
every mode. Under that, SiteSettings `XaiByokMode` (default Allow) chooses the rest: Required uses
only the user key, Allow uses the user key then the site key, and Off uses only the site key.
Off hides the Profile section and refuses a save. When nothing resolves, Ask names the missing
key: the user's Profile key, or an admin for the site key, according to the mode.

A save calls xAI before it writes. A rejected key or an unreachable call does not save, and the
error text does not contain the key. Retiring a principal (merge) deletes that user's row. The
same delete is what a user-deletion path calls.

## Decisions (recorded: Steve 2026-10-25)

- Precedence: **host configuration always wins** (does not follow the earlier proposal of
  user → site → host). Task 398 is shipped and tested with host-wins
  (`SiteXaiApiKey_Given_` asserts a host `XAI:ApiKey` resolves and admin writes return 409),
  and a host key is deployment-level intent (corporate key, org billing, centralized
  controls) that a self-registered user must not override. The ladder below the host is
  driven by the policy switch:
  - **Required:** user key only (no fallback)
  - **Allow:** user key, then site key (398)
  - **Off:** site key only
  - The host key wins in all three.
- Policy: **SiteSettings switch with three values — Required / Allow / Off — default
  "Allow"** (accepts the proposal). Default "Allow" fits the template: no site key exists
  out of the box, so BYOK makes Ask usable with zero admin action. "Off" gives back the
  pre-BYOK behavior and must hide the Profile → AI key section entirely (users cannot save
  a key the app will not use). Changing the policy takes effect without a restart,
  mirroring task 398's read-key-once-per-completion behavior.
- Provider scope: **xAI only now, storage shaped as provider + key** (accepts the proposal).
  Name rows `user.{userId}.xai.api-key` beside 398's `site.xai.api-key`, and scope the
  Data Protection purpose per user (for example `TimeWarp.Architecture.XaiApiKey.User:{id}`)
  so user A's ciphertext cannot be unprotected in user B's context. Contracts, UI, and hints
  stay xAI-only; no provider-selection UI until a second provider exists.

## Checklist

- [x] Profile → AI key: set, replace, clear; write-only, shows "ends in …xxxx"
- [x] Encrypted at rest per user (Data Protection purpose includes the user id)
- [x] Ask uses the resolved key per the precedence decision; the key never reaches the browser or logs
- [x] When no key resolves, Ask says which one to set (user key or ask an admin), depending on the policy
- [x] A policy change (Required / Allow / Off) takes effect on the next completion without a restart
- [x] Validate a key on save with a cheap xAI call; show the provider's error without echoing the key
- [x] Deleting a user deletes their key (`user.{userId}.xai.api-key` rows and the per-user purpose scope)
- [x] Tests: precedence ladder per policy (host first; Required = no fallback; Allow = user then site; Off = site only), policy modes, per-user isolation (user A never uses B's key, per-user purpose scope), validation failure
- [x] With policy **Off**, Profile hides the AI key section and Ask ignores/resolves without user keys
- [x] Contracts per `tw-web-api-contracts`

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-21)
- Decisions recorded: ses_e8cbae678ffe6TePjMOOX5xy0N (2026-10-25) — precedence (host always wins, ladder per policy), policy switch (Required / Allow / Off, default Allow), provider scope (xAI only, provider + key storage shape)
- Implemented: grok 01a17414-3cc0-7172-bbd7-08dcb08980b1 (2026-10-26) — Profile key, policy ladder, probe-before-save, merge delete
- Audit: added `stacked-task-set*.resume.json` to `.gitignore`. `kanban-landed` still warns that eight other claims have no worktree; that warning is advisory and is not this task.

## Notes

- Origin: Steve 2026-10-21, "I also want to implement BYOK".
- Depends on tasks 397 and 398.
- Save-time validation (implementer, 2026-10-26): a failed or unreachable cheap xAI call does
  not save. The response shows the scrubbed provider error, or "xAI could not be reached.
  Nothing was saved." There is no unvalidated flag. Steve can override this if a stored-but-marked
  key is preferred.
- There is no delete-user endpoint. `DeleteForPrincipalAsync` removes the row. Merge of a
  principal into another account calls it for the retired id, and the EF merge deletes the same
  row in that transaction.

## Results

Signed-in users store an xAI API key at `user.{guid:D}.xai.api-key`, protected with purpose
`TimeWarp.Architecture.XaiApiKey.User:{guid:D}`. Profile reads and writes `api/Users/Current/xai-api-key`
(GET ProfileRead, PUT and DELETE ProfileWrite). The body never returns the key. Status carries
`suffix` so the page can say "ends in …xxxx".

`IXaiApiKeyResolver` reads the host `XAI:ApiKey` first. Below that, `SiteSettings.XaiByokMode`
(default Allow, stored as 0) picks the rest: Required is the user key only, Allow is the user key
then the site key, and Off is the site key only. Off sets status `visible` false and PUT/DELETE
return 409 "This site does not accept a personal xAI API key." A mode change is read on the next
completion. The chat client is cached per key, so a mode change that resolves the same key reuses
that client.

Save calls GET `{endpoint}/models` with the candidate key. A 401 or 403 does not save and returns
400 with the provider text scrubbed (`[redacted]`, newlines removed, 200 characters). Any other
failure, including a network error, does not save and returns 503 "xAI could not be reached.
Nothing was saved."

When no key resolves, Ask's `keyHint` is "Set your xAI API key on Profile." (Required), "Set your
xAI API key on Profile, or ask an admin to set the site key." (Allow), or the admin sentence plus
the existing host setup text (Off). `adminHint` stays off while the mode is Required.

Retiring a principal deletes that user's row from `DeleteForPrincipalAsync` after the merge and
again in the same EF `SaveChanges`. The Data Protection purpose is a derived protector, not its
own key-ring row.

Admin → Authentication has an Ask API key select (Allow, Required, Off). Profile's section uses
`UserXaiApiKeySettings`, `UserXaiApiKeyInput`, `UserXaiApiKeySave`, `UserXaiApiKeyClear`,
`UserXaiApiKeyStatus`, and `UserXaiApiKeyHost`. The Profile, Admin, and Ask pages were not opened
in a browser.

Column `identity.site_settings."XaiByokMode"` is migration `20261025175631_AddSiteSettingsXaiByokMode`,
non-nullable integer, default 0.

### How to validate

Smoke:

From `tests/container-apps/web/web-jaribu-tests`:

```bash
dotnet test -- --filter-class UserXaiApiKey
dotnet test -- --filter-class XaiApiKeyStatus
dotnet test -- --filter-class SiteXaiApiKey
dotnet test -- --filter-class SiteSettings
dotnet test -- --filter-class CompleteAgentChat
dotnet test -- --filter-class GetAgentChatConfigurationHint
```

From `tests/container-apps/web/web-server-integration-tests`:

```bash
dotnet test -- --filter-class Xai
```

Expect:

- `UserXaiApiKey` passes 11. The host key wins while Required. Required ignores the site key. Allow uses the user key, then the site key. Off ignores the user key. The next resolve sees Off, then Required. User A's purpose does not open B's row. A rejected probe is 400, does not write, and the detail does not contain the key. An unreachable probe is 503 and does not write. `DeleteForPrincipalAsync` removes A's row and leaves B and `site.xai.api-key`. Off status has `visible` false and PUT is 409. The audit line contains "set" and not the key. A write while the host key is set stores the user key with `supersededByHost` and suffix `aaaa`.
- `XaiApiKeyStatus` passes 3. The user status JSON is camelCase, round-trips `mode` Allow, and has no `apiKey` property.
- `SiteXaiApiKey` passes 6 (task 398 host-wins and storage failures still hold).
- `SiteSettings` passes 66. Those tests still pass with the new mode property on the settings contract.
- `CompleteAgentChat` passes 21. A missing key uses the upstream detail when the stub sets one, and the existing host setup text when it does not.
- `GetAgentChatConfigurationHint` passes 7. Required omits `adminHint` and sets `keyHint` to the Profile sentence.
- `Xai` integration passes 15. A 401 body that contains the key and a newline becomes 400 with `[redacted]` and no newline. A connection failure is 503 "xAI could not be reached. Nothing was saved." Allow sends the signed-in user's key, Off sends the site key, Required sends the user key again, and Allow for a user with no key sends the site key.
- Profile and Admin were not clicked in a browser. The markup is `data-qa="UserXaiApiKeySettings"` on Profile (hidden until status `visible` is true) and `data-qa="XaiByokMode"` on Admin → Authentication.
