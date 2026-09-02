# Round 1 — merged findings
**Date:** 2026-09-02
**Sources:** general, orchestrator (review oracle verification pass)

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: `.claude/skills/aspire-orchestration/references/safety-guardrails.md:233-235` (+ `.agents/` mirror)
- Description: the `ps` → `describe` edit left a literal duplicate `aspire describe --include-hidden --format Json` line in the code block, and the "Normal flow — filtered" line above it still reads `aspire ps --format Json` as a filtered resource list.
- Suggestion: drop the duplicate; point the normal-flow line at `aspire describe --format Json`.
- Source: general, orchestrator
- Disposition notes: duplicate line removed; normal-flow line now `aspire describe --format Json` (both trees).

### M2 — Severity: suggestion — Status: fixed
- File: `.claude/skills/aspire-orchestration/references/safety-guardrails.md:198,213`; `.claude/skills/aspire-orchestration/references/resource-management.md:15`; `.claude/skills/aspire-orchestration/SKILL.md:108,112` (+ `.agents/` mirrors)
- Description: adjacent sections in the same edited files still describe `aspire ps --format Json` as a resource list returning `name` / `displayName` (Rule 5 JSON block, Known JSON Output Issues row, `aspire wait` displayName tip, "Resource not found" troubleshooting row). Contradicts the 13.5 statement this commit adds two sections later. Confirmed against the 13.5.3 CLI: `aspire ps` "List running AppHosts"; `aspire describe` "Describe resources in a running AppHost" with `--include-hidden`.
- Suggestion: point resource-level `name` / `displayName` guidance at `aspire describe --format Json`; keep `aspire ps` only where the AppHost list is meant.
- Source: general (orchestrator added SKILL.md:108,112)
- Disposition notes: all five lines repointed at `aspire describe --format Json` / `aspire ps` kept only for the AppHost-running check (both trees; `diff -r` identical).

### M3 — Severity: nit — Status: fixed
- File: `kanban/to-do/209-…/task.md` checklist "`dev build` 0/0; aspire-tests still boot"
- Description: ticked but Results record no actual run evidence (only prescriptive "Expect").
- Suggestion: record the run output in Results (orchestrator re-ran both gates this round: `dev build` 0 Warning(s) 0 Error(s); aspire-tests total 7 / failed 0).
- Source: general
- Disposition notes: gate evidence recorded in task.md Results (review oracle re-ran `dev build` and aspire-tests).

### M4 — Severity: suggestion — Status: fixed
- File: `.gitignore:473-476`
- Description: the added `.memsearch/memory/` rule is redundant — master already ignores the whole `.memsearch/` directory (`.gitignore:459`, verified via `git check-ignore -v`). It is also out of scope for an Aspire bump task.
- Suggestion: remove the added block; the existing rule already covers it and `git add -f` still works for deliberate promotion.
- Source: orchestrator
- Disposition notes: `.gitignore` restored to master byte-for-byte (`git diff master -- .gitignore` empty); existing `.memsearch/` rule already covers it.

## Duplicates / conflicts

- general Issue 1 and the orchestrator's independent duplicate-line observation collapsed into M1 (orchestrator also flagged the stale normal-flow `ps` line in the same block).
- general Issue 2 extended with two more lines of the same class in `aspire-orchestration/SKILL.md` (M2).
- M4 is orchestrator-only; the general reviewer checked `.gitignore` for tracked-file impact but not for redundancy with the existing rule.
