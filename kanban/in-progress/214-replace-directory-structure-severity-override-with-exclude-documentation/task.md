# Replace directory-structure severity override with exclude = documentation

## Description

Task 210-005 retired `documentation/` and, because `ganda repo audit` had no per-directory
waiver, downgraded the whole `directory-structure` check in `.editorconfig`
(`directory-structure.severity = warning`). That also stops `tests/`, `skills/` and `kanban/`
from blocking. Ganda task 278 (timewarp-ganda PR #157, merged 2026-09-13) added
`directory-structure.exclude`. Swap the override for the exclude so the check returns to error
for everything except the one deliberately retired directory.

## Requirements

- In root `.editorconfig`, under `[ganda.audit]`, replace
  `directory-structure.severity = warning` with `directory-structure.exclude = documentation`
  and update the comment (documentation/ retired; regions + skills are the record; see AGENTS.md
  §Documentation).
- Do not add any other `[ganda.audit]` change.
- Gate: `ganda repo audit` must report `directory-structure` as **passed** with message
  `Expected directory structure is present (excluded: documentation/)`, and the run must have no
  blocking failures. This requires an installed `ganda` built at or after timewarp-ganda
  commit for PR #157 (`ganda --version` shows the commit hash). If the installed binary still
  prints `Unknown` / ignores the key, reinstall ganda from its master first; do not merge this
  task with the check silently downgraded.
- No product code changes.

## Checklist

- [x] Installed `ganda` is at/after timewarp-ganda PR #157 (`ganda repo audit --list-checks` footer mentions `directory-structure.exclude`)
- [x] `.editorconfig` swapped; comment updated
- [x] `ganda repo audit` passes with the exclusion note on `directory-structure`
- [x] Mention in 210-005's disposition exception log that the wontfix is now resolved (one line under its `review/disposition.md` Escalations, or in this task's Results if that file is frozen)

## Depends on

- (cross-repo) timewarp-ganda 278 — merged 2026-09-13; install gate above

## Notes

- Origin: 210-005 review wontfix M5; ganda 278 brief follow-up item.
- Decision (Steve, 2026-09-13): `documentation/` stays required by default across TimeWarp
  repos; exemption is per-repo and per-directory via `.editorconfig`.

## Session

- Created: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED (2026-09-13)
- Implementer: grok-4.6 session 01a099d7-02c0-78d2-8d6c-c0d4971bdc91 (2026-09-13)

## Results

Swapped the repo-wide `directory-structure` warning for a per-directory exclude so `tests/`, `skills/`, and `kanban/` block again, while the retired `documentation/` tree stays waived.

**Files changed**

- `.editorconfig` `[ganda.audit]`: `directory-structure.severity = warning` → `directory-structure.exclude = documentation`; comment now points at AGENTS.md §Documentation. No other `[ganda.audit]` keys.
- `kanban/done/210-005-…/review/disposition.md` Escalations: one line that task 214 resolved M5.
- Kitchen moved to `kanban/in-progress/` and renamed `…-exclude--documentation` → `…-exclude-documentation` so `kebab-path-names` is not a blocking failure (the `=` in the original title became an empty slug segment).

**Install / audit**

- Installed `ganda --version`: `1.0.0-beta.24+a20e5705f8eedac0f71a50da063e1223cb2461f6` (at/after timewarp-ganda PR #157).
- `ganda repo audit --list-checks` footer includes `directory-structure.exclude = documentation`.
- `ganda repo audit`: `directory-structure` **PASS** with `Expected directory structure is present (excluded: documentation/)`. No blocking failures. Advisory warnings only: `memsearch-scaffold` (`.githooks/pre-commit`, `.githooks/pre-push`) and `vscode-window-icon` (`peacock.color`) — pre-existing, not this change.

**Decisions / deviations**

- No product code changes.
- Fresh worktrees do not carry `bin/dev` (gitignored `[Bb]in/`). Restore it (copy from the origin-home worktree or `self-install`) before treating `bin-dev` / `dev-cli-capabilities` as green.

### How to validate

**Smoke**

```bash
ganda --version
ganda repo audit --list-checks
# restore bin/dev if this worktree has none, e.g.:
#   mkdir -p bin && cp /home/steve/worktrees/github.com/TimeWarpEngineering/timewarp-architecture/master/bin/dev bin/dev
ganda repo audit
rg -A2 '^\[ganda.audit\]' .editorconfig
```

**Expect**

- `ganda --version` prints a commit hash (not `Unknown`); `--list-checks` footer mentions `directory-structure.exclude`.
- `directory-structure` is **PASS** with message `Expected directory structure is present (excluded: documentation/)`.
- Audit summary: no blocking failures (warnings for memsearch-scaffold / vscode-window-icon may remain).
- `[ganda.audit]` contains only `directory-structure.exclude = documentation` (no `directory-structure.severity`).
