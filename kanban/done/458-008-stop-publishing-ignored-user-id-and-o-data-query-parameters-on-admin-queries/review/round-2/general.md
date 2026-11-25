# Round 2 — general
**Date:** 2026-11-26
**Scope reviewed:** delta since round 1. Commit `22e842c7e` rewrites the canonical-examples parenthetical in `skills/tw-web-api-contracts/SKILL.md` and records that fix on the task. Kitchen-only follow-up is `e7a4563b5` (implementer session line). Compared with `## Goal (implementer restatement)` and the original requirements.

## Summary

Round 1's only finding, M1, asked the skill to stop calling GetRoles "attribute auth + open-data" and GetRole "manual auth". The replacement says `Search` is GetRoles' only query parameter, and that GetRole's route id is the whole request (`IAuthApiRequest`, no caller property, `I*Details` Response). That matches `get-roles-contracts.cs` (`Query` declares only `Search`) and `get-role-contracts.cs` (empty `Query` body, `IAuthApiRequest`, `Response : IRoleDetails`). The fix-pass record covers the skill-text change: `./bin/dev build`, `./bin/dev lint-skills`, and `ganda repo audit`, all passed. Those gates were not re-run. The restatement still matches the task. No new defects on this delta.

## Resolved prior

### M1 — Severity: suggestion — Status: fixed
- File: skills/tw-web-api-contracts/SKILL.md:469
- The removed phrases are gone. The new parenthetical matches the living contracts named above.
