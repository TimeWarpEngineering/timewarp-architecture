# Commit journal glob so claim does not dirty gitignore

## Description

`/tw-merge` of architecture **205-004** (and 205-001 / 205-002 / 132 / 111) merged the PR then
**refused worktree gc** because the claim tree had ` M .gitignore`.

That is not leftover product work. **Ganda 268** `TaskWorkJournalIgnore.EnsurePorcelainGitignores`
runs on `ganda kanban claim` / `ganda task work` and **appends** two blocks when those exact
lines are missing. Architecture **208** committed the six 262 **basenames** (legacy PASS).
Claim still appends the 268 **glob** so origins converge. The append is an uncommitted
edit of a **tracked** file. `WorktreeGcService` treats that as dirty and refuses prune.

Maintainer: **commit the damn `.gitignore` changes.** After they are on `origin/master`,
claim pickup is a no-op and gc stops choking.

## Requirements

Commit the two blocks claim already writes (keep the existing six basenames and the
existing `.memsearch/` line — do not rewrite the rest of `.gitignore`):

```gitignore
# Routine journals beside kitchens (local; not product)
*.journal.json

# Memsearch memory auto-writes (plugin daily notes; local until promoted).
# Deliberate promote of distilled memory: git add -f .memsearch/memory/<file>.md
.memsearch/memory/
```

- `.memsearch/memory/` is **redundant** with `.memsearch/` (209 said so). Commit it anyway:
  `EnsureMemsearchMemoryGitignoreContent` only accepts the four `memory` spellings, not
  `.memsearch/`. Skip it and claim will keep dirtying the file.
- Do **not** `git rm --cached` tracked `.memsearch/memory/*.md` if any exist. Promote stays
  `git add -f`.
- Do **not** commit journal contents or `.memsearch/memory/` daily notes.
- `ganda repo audit --fix --checks routine-journals-gitignore,memsearch-memory-gitignore`
  should PASS after the commit. Full-audit other FAILs are out of scope.
- Claim this worktree **already** appended those lines. **Commit them.** Do not `git restore
  -- .gitignore`.

## Checklist

- [ ] Root `.gitignore` has `*.journal.json` glob and `.memsearch/memory/`
- [ ] Six 262 basenames and `.memsearch/` stay
- [ ] `git ls-files '*.journal.json'` empty; no journal blobs in the PR
- [ ] Results + How to validate

## Notes

- Parent **208**. Ganda SSOT: check `routine-journals-gitignore` (268 glob) and
  `memsearch-memory-gitignore` (263). Pickup: `source/timewarp-ganda/.../task-work-journal-ignore.cs`.
- Cockpit: timewarp-flow Grok `01a03d38-9611-7620-aae5-848e15dafa94` (2026-09-07). Do not
  implement in cockpit.

## Session

- Created: 192270 (2026-09-07)
- Cockpit: Grok — `/tw-merge` 205-004 GC refuse; commit the claim pickup
