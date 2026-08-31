# Review framework — task 200

**Date:** 2026-08-31
**Host task:** kanban/in-progress/200-web-server-js-initializer-manifest-must-include-webspa-so-windowspa-cannot-go-missing/
**Diff scope:** branch `task/200-web-server-js-initializer-manifest-must-include-we` vs `origin/master` (commits `49a517c6`, `aadcc568`)
**Plan / brief:** `task.md` — host JS initializer list must include Web.Spa after incremental/Rebuild/Aspire start; automated gate; passkey ceremony must not depend on `window.Spa`.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** Grok review oracle (2026-08-31)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-1/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## What was implemented (from Results)

- Passkey C# (`PasskeyCeremonyClient`, Settings add-passkey) calls `WebAuthnJsModule`, which `import()`s `./js/features/web-authn.js` named exports instead of `Spa.WebAuthn.*`.
- web-spa re-globs `wwwroot/js/**` into Content after tsc so SWA discovery tags `web.spa.lib.module.js`.
- web-server fails build/publish if `_ExistingBuildJSModules` / `_ExistingPublishJSModules` omit `web.spa*.lib.module.js`.
- Fast Up-to-date Check watches the unfingerprinted emit path.
- Jaribu tests: host manifest JSON gate; import-path / no-`Spa.WebAuthn` call-site gate.

## Round 2

Re-review after M1 Design-region fix on `web-authn-js-module.cs`. Carry stable ID `M1`. Scan the fix delta for new defects; do not clobber round-1 files.
