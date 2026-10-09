# Audit and trim AGENTS.md to a lean core

## Description

Steven (2026-10-10): `AGENTS.md` is 33 KB (33,098 bytes), 408 lines and about 4,000 words, which
is absurd. Agents are smarter now and need less, not more. He suspects much of it is
contradictory.

Every agent run loads this file, so its size and any contradictions cost on every task. Cut it
down to a short core and move anything situational into the skill it belongs to.

## Requirements

1. **Audit every section** of `AGENTS.md`. Find:
   - contradictions, both within the file and against the skills under `skills/` and the shared
     `tw-*` skills;
   - stale or obsolete rules;
   - duplicates of what a skill, analyzer, `ganda repo audit` check or the dev CLI already
     enforces;
   - generic advice a capable agent doesn't need.

   Write the audit as review notes in this task folder (for example `audit.md`), with a table of
   section, verdict (keep, trim, move to skill, delete) and reason, and list each contradiction
   found (quote both sides and say which one wins).
2. **Rewrite `AGENTS.md` as a short core:** what the repo is, hard rules that nothing else
   enforces, and pointers to the skills, dev CLI and docs. Target well under 8 KB. Move anything
   still valuable but situational into the matching skill rather than deleting it.
3. **Keep generated copies consistent:** if the template ships its own `AGENTS.md`, or
   `ganda agents` sync targets this repo, make sure the generated/shipped version matches the new
   lean core (or is deliberately different, with the reason recorded).
4. **Gates:** dev build passes, `ganda repo audit` is clean, and one PR whose body has a
   before/after size summary (bytes, lines, words) and the contradiction list.

Merge with `ganda pr merge` only when Steven approves. Do not merge as part of the walk.

## Checklist

- [ ] Audit every section of AGENTS.md; write review notes with the verdict table in the task folder
- [ ] List every contradiction found (within the file and against skills/ and shared tw-* skills)
- [ ] Move situational but valuable content into the matching skills
- [ ] Rewrite AGENTS.md as a lean core, well under 8 KB
- [ ] Check the template's shipped AGENTS.md and ganda agents sync targets stay consistent
- [ ] dev build passes
- [ ] ganda repo audit clean
- [ ] PR open with before/after size summary and contradiction list in the body
- [ ] Merge via ganda pr merge only after Steven approves

## Notes

- Before: 33,098 bytes, 408 lines, about 4,000 words (measured on master, 2026-10-10).
- For comparison, Crunchit's AGENTS.md is 13 KB and most other TimeWarp repos are 1 to 6 KB.
