# BYOK: users bring their own xAI API key

## Description

Let each signed-in user supply their own xAI API key for Ask (bring your own key), so usage
bills to their xAI account. Builds on the encrypted-secret storage from task 398 and the durable
key ring from task 397.

## Decisions for Steve (before implementation)

- [ ] Precedence: proposal is user key, then site key (398), then host configuration; or should host configuration always win?
- [ ] Policy: can Admin require BYOK (no site key fallback), allow it, or turn it off? Proposal: a SiteSettings switch with those three values, default "allow"
- [ ] Provider scope: xAI only now, or a provider + key pair so other providers can follow? Proposal: xAI only, storage shaped as provider + key

## Checklist

- [ ] Profile → AI key: set, replace, clear; write-only, shows "ends in …xxxx"
- [ ] Encrypted at rest per user (Data Protection purpose includes the user id)
- [ ] Ask uses the resolved key per the precedence decision; the key never reaches the browser or logs
- [ ] When no key resolves, Ask says which one to set (user key or ask an admin), depending on the policy
- [ ] Validate a key on save with a cheap xAI call; show the provider's error without echoing the key
- [ ] Deleting a user deletes their key
- [ ] Tests: precedence, policy modes, per-user isolation (user A never uses B's key), validation failure
- [ ] Contracts per `tw-web-api-contracts`

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-21)

## Notes

- Origin: Steve 2026-10-21, "I also want to implement BYOK".
- Depends on tasks 397 and 398.
