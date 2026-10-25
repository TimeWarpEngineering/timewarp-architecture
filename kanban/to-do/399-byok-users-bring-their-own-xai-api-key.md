# BYOK: users bring their own xAI API key

## Description

Let each signed-in user supply their own xAI API key for Ask (bring your own key), so usage
bills to their xAI account. Builds on the encrypted-secret storage from task 398 and the durable
key ring from task 397.

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

- [ ] Profile → AI key: set, replace, clear; write-only, shows "ends in …xxxx"
- [ ] Encrypted at rest per user (Data Protection purpose includes the user id)
- [ ] Ask uses the resolved key per the precedence decision; the key never reaches the browser or logs
- [ ] When no key resolves, Ask says which one to set (user key or ask an admin), depending on the policy
- [ ] A policy change (Required / Allow / Off) takes effect on the next completion without a restart
- [ ] Validate a key on save with a cheap xAI call; show the provider's error without echoing the key
- [ ] Deleting a user deletes their key (`user.{userId}.xai.api-key` rows and the per-user purpose scope)
- [ ] Tests: precedence ladder per policy (host first; Required = no fallback; Allow = user then site; Off = site only), policy modes, per-user isolation (user A never uses B's key, per-user purpose scope), validation failure
- [ ] With policy **Off**, Profile hides the AI key section and Ask ignores/resolves without user keys
- [ ] Contracts per `tw-web-api-contracts`

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-21)
- Decisions recorded: ses_e8cbae678ffe6TePjMOOX5xy0N (2026-10-25) — precedence (host always wins, ladder per policy), policy switch (Required / Allow / Off, default Allow), provider scope (xAI only, provider + key storage shape)

## Notes

- Origin: Steve 2026-10-21, "I also want to implement BYOK".
- Depends on tasks 397 and 398.
- Open item flagged with the decisions (2026-10-25), needs Steve before implementation: the
  save-time validation error policy — whether a failed or unreachable cheap xAI validation
  call means "warn and do not save" or "save but mark unvalidated". Same risk shape as the
  task 398 learnings: a parenthetical that read decided but was not.
