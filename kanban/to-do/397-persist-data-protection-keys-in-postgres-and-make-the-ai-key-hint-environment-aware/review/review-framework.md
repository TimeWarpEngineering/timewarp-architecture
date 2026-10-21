# Review framework

## Budget (by-diff)

- Lines changed: 1184
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Scope — task 397

**Date:** 2026-10-22
**Host task:** kanban/to-do/397-persist-data-protection-keys-in-postgres-and-make-the-ai-key-hint-environment-aware/
**Diff scope:** branch `task/397-persist-data-protection-keys-in-postgres-and-make` vs `master` (commits 3bccacdc3, 03fe93732)
**Plan / brief:** `## Goal (implementer restatement)` and `## Description` in task.md. Persist the ASP.NET Data Protection key ring on `PostgresDbContext` when postgres is configured, and make the Ask "AI not configured" hint depend on the environment.
**Effort:** 3 (by-diff budget), roster axis general
**Reviewer roster:** general
**Session IDs:** review oracle (claude, claude-opus-5-5) daca0722-ee55-4126-90d2-5dbd3aed5e27

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
