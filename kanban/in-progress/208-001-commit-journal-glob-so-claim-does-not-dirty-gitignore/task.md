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

- [x] Root `.gitignore` has `*.journal.json` glob and `.memsearch/memory/`
- [x] Six 262 basenames and `.memsearch/` stay
- [x] `git ls-files '*.journal.json'` empty; no journal blobs in the PR
- [x] Results + How to validate

## Notes

- Parent **208**. Ganda SSOT: check `routine-journals-gitignore` (268 glob) and
  `memsearch-memory-gitignore` (263). Pickup: `source/timewarp-ganda/.../task-work-journal-ignore.cs`.
- Cockpit: timewarp-flow Grok `01a03d38-9611-7620-aae5-848e15dafa94` (2026-09-07). Do not
  implement in cockpit.

## Session

- Created: 192270 (2026-09-07)
- Cockpit: Grok — `/tw-merge` 205-004 GC refuse; commit the claim pickup
- Implementer: Grok session `01a07cd1-1c85-7613-9424-eb32c533b285` (2026-09-08)

## Results

Committed the Ganda 268 claim-pickup blocks that were already appended on this worktree. After they land on `origin/master`, claim is a no-op and `WorktreeGcService` no longer sees `M .gitignore`.

### What was implemented

- Root `.gitignore` now has `*.journal.json` and `.memsearch/memory/` (claim already wrote them; they were committed, not restored).
- Six 262 basenames (`task-work`, `stacked-task-set`, `planning`, `rfc`, `debate`, `advisor`) and `.memsearch/` kept.
- No journal blobs and no `.memsearch/memory/` daily notes in the commit.

### Files changed

- `.gitignore` — 7 lines appended (`b48a3d64`)
- `kanban/in-progress/208-001-…/task.md` — column move + Results

### Key decisions / deviations

- `.memsearch/memory/` is redundant with `.memsearch/` (209). Committed anyway: `EnsureMemsearchMemoryGitignoreContent` only accepts the four `memory` spellings, not `.memsearch/`.
- Did not `git rm --cached` any `.memsearch/memory/*.md` (none tracked). Promote stays `git add -f`.
- `ganda repo audit --fix --checks …` still runs the full audit after the named fixes. The two named checks PASS; other FAILs (`bin-dev`, `kebab-path-names`, …) are out of scope.

### Test outcomes

- `git ls-files '*.journal.json'` — empty
- `git ls-files '.memsearch/'` — empty
- `git check-ignore -v` on this kitchen’s `task-work.journal.json` — matched `.gitignore:477:*.journal.json`
- `ganda repo audit --fix --checks routine-journals-gitignore,memsearch-memory-gitignore`
  - `routine-journals-gitignore` PASS
  - `memsearch-memory-gitignore` PASS
  - `--fix` reported FIXED / “already ignores” (no further file change)

### How to validate

**Smoke**

```bash
git show HEAD:.gitignore | tail -20
git ls-files '*.journal.json'
git ls-files '.memsearch/'
ganda repo audit --fix --checks routine-journals-gitignore,memsearch-memory-gitignore
```

**Expect**

- Tail includes the six 262 basenames, then `*.journal.json`, then `.memsearch/memory/`. `.memsearch/` remains above the journal block (line 459).
- Both `git ls-files` commands print nothing.
- Named checks `routine-journals-gitignore` and `memsearch-memory-gitignore` PASS. Other full-audit FAILs are out of scope.
- After merge to `origin/master`, a fresh `ganda kanban claim 208-001` (or any later claim) must not dirty `.gitignore`.
