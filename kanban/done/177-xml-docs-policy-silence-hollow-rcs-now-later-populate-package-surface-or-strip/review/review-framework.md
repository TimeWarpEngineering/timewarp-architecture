# Review framework — task 177

**Date:** 2026-09-21
**Host task:** kanban/in-progress/177-xml-docs-policy-silence-hollow-rcs-now-later-populate-package-surface-or-strip/
**Diff scope:** branch `task/177-xml-docs-policy-silence-hollow-rcs-now-later-popul` vs `origin/master` (`7a972942` rescope + `c322181a` implementation). Working tree clean.
**Plan / brief:** Path A (real `///` on packable public surface) + Path B (strip hollow shells from template/app/tests) + CS1591 warning gated on `IsPackable=true`. RCS1141/1228 stay off.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** grok review oracle (2026-09-21); round 1 general subagent `01a0c348-db20-7e90-a531-34ef9ab0b210`

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## Requirements to check

1. **Gate:** CS1591 is a warning (TreatWarningsAsErrors → fail) only when `IsPackable=true`; non-packable stays in NoWarn. Switch may live in `Directory.Build.targets` if props evaluation order requires it — confirm the claimed reason is true.
2. **A:** every public type/member in packable projects has a real `<summary>` (substance, not empty shells). Foundation hollow shells filled or removed. Generated TypedId BCL XML in the generator.
3. **B:** hollow `<param>`/`<returns>`/`<typeparam>` shells gone from container-apps / tools / tests; no new XML added there.
4. **Policy docs:** AGENTS.md Documentation one-liner; `.editorconfig` XML-analyzer comment block matches the shipped policy; RCS1141/1228 remain `none`.
5. **Proof:** `dev build` 0/0 and `dev template-smoke` were claimed; re-verify what can be re-verified (hollow-shell rg, IsPackable/CS1591 wiring, sample XML quality, missed packable public members).
