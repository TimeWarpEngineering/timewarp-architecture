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

- [ ] Installed `ganda` is at/after timewarp-ganda PR #157 (`ganda repo audit --list-checks` footer mentions `directory-structure.exclude`)
- [ ] `.editorconfig` swapped; comment updated
- [ ] `ganda repo audit` passes with the exclusion note on `directory-structure`
- [ ] Mention in 210-005's disposition exception log that the wontfix is now resolved (one line under its `review/disposition.md` Escalations, or in this task's Results if that file is frozen)

## Depends on

- (cross-repo) timewarp-ganda 278 — merged 2026-09-13; install gate above

## Notes

- Origin: 210-005 review wontfix M5; ganda 278 brief follow-up item.
- Decision (Steve, 2026-09-13): `documentation/` stays required by default across TimeWarp
  repos; exemption is per-repo and per-directory via `.editorconfig`.

## Session

- Created: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED (2026-09-13)
