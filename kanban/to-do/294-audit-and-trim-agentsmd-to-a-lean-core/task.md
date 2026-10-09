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

- [x] Audit every section of AGENTS.md; write review notes with the verdict table in the task folder
- [x] List every contradiction found (within the file and against skills/ and shared tw-* skills)
- [x] Move situational but valuable content into the matching skills
- [x] Rewrite AGENTS.md as a lean core, well under 8 KB
- [x] Check the template's shipped AGENTS.md and ganda agents sync targets stay consistent
- [x] dev build passes
- [x] ganda repo audit clean
- [ ] PR open with before/after size summary and contradiction list in the body
- [ ] Merge via ganda pr merge only after Steven approves

## Notes

- Before: 33,098 bytes, 408 lines, about 4,000 words (measured on master, 2026-10-10).
- For comparison, Crunchit's AGENTS.md is 13 KB and most other TimeWarp repos are 1 to 6 KB.
- Verdict table, quoted contradictions, and the pack/`ganda agents` decision: `audit.md` in this folder.
- PR body for the host open-pr node is under `## Results` / `### Pull request body`. This walk does not open the PR or merge.

## Session

- Implementer: Grok session 01a121e2-20df-7741-9b78-feb8a06de58a (2026-10-10)
- Review oracle: Claude Opus 5.5 with a general reviewer subagent (2026-10-10), effort 3

## Results

`AGENTS.md` is a short core every agent run can load. Situational rules that were only in that file now live in the skill or catalog that owns them. The audit (verdict table and nine contradictions, both sides quoted) is `audit.md` in this task folder.

After: 3,566 bytes, 54 lines, 481 words. Before: 33,098 bytes, 408 lines, 4,000 words.

### Files

- `AGENTS.md` rewritten.
- `audit.md` added in this folder.
- Moved: browser-protocol endpoints and seam `JsonSerializerOptions` into `skills/tw-web-api-contracts/SKILL.md`; in-proc vs closed-box host lanes into `skills/tw-feature-placement/references/co-located-jaribu-runfiles.md`; retired diagnostic ids as `;` comments on both `AnalyzerReleases.Unshipped.md` files.
- Skill sentences that had drifted: `tw-feature-placement` (endpoint function is used; misplacement is TWA0015/TWA0016; pointers no longer cite an AGENTS.md catalog), `tw-slice-isolation` (same pointer), `kanban/overview.md` (reserve/claim, and a banner that the contracts skill wins over the old Mapper/Endpoint/validator checklist).
- Citations only: `purpose-region-analyzer.cs`, `fast-endpoint-source-generator.md`, four test Design comments, `readme.md` (this file is not packed).

### Decisions

- No tenth skill. `AssertSkillsShipped` expects the existing nine `skills/*/SKILL.md` files. Platform package facts stay in the MSBuild comments.
- The template pack does not include `AGENTS.md` or `CLAUDE.md`. `ganda agents targets` does not list this repo. Both are deliberate; `audit.md` records why. `CLAUDE.md` stays `@AGENTS.md`.
- Flow skills `tw-agent-context-regions` (says TWPA0004), `tw-csharp` ("non-trivial" Purpose), and `tw-jaribu` (dotted test names) were not edited. They live in timewarp-flow. The lean core states the ids this repo actually enforces.
- `readme.md` still badges `dotnet-10.0` while the repo is `net11.0`. Left as-is; outside this audit.
- Column stays `to-do` so the host's task-folder path stays valid. PR and merge stay for later nodes.

### Tests

- `./bin/dev build`: succeeded, 0 Warning(s), 0 Error(s), 00:00:07.68.
- `ganda repo audit`: Passed 31, Failed 0, Skipped 0.

### How to validate

**Smoke**

```bash
python3 -c "import pathlib; p=pathlib.Path('AGENTS.md'); b=p.read_bytes(); t=b.decode(); print(len(b), t.count(chr(10))+(0 if t.endswith(chr(10)) else 1), len(t.split()))"
test -f kanban/to-do/294-audit-and-trim-agentsmd-to-a-lean-core/audit.md
rg -n '^\| Section \|' kanban/to-do/294-audit-and-trim-agentsmd-to-a-lean-core/audit.md
rg -n 'AGENTS.md' timewarp-templates/source/timewarp-architecture-template/timewarp-architecture-template.csproj || echo 'pack allow-list has no AGENTS.md'
```

**Expect**

- First command prints `3566 54 481` (bytes, lines, words). 3566 is under 8192.
- `audit.md` exists and its verdict table header is `| Section | Verdict | Reason |`.
- The template csproj allow-list does not name `AGENTS.md` (the echo line, or no match).

**Automated gate**

```bash
./bin/dev build
ganda repo audit
```

Expect build 0 warnings and 0 errors, and audit `Failed: 0`.

**Not in scope:** opening the PR, merging, editing timewarp-flow skills, or the readme `dotnet-10.0` badge.

### Review

- Effort 3 (by-diff, 836 lines), roster: general. Rounds: 2 (round 2 re-verified the fix delta).
- Final counts: bug 0; suggestion 1 fixed; nit 2 fixed and 1 wontfix; 0 open.
- Disposition: **accepted-exceptions**. M2 is wontfix because `TIMEWARP_TEST_PORT_BASE` is already in the same reference, and `InProcTestPorts` owns the defaults.
- Fixes: `AGENTS.md` PR-gates row restores the same-commit `<Version>` and pin bump rule (task 124). The task.md wording and the `tw-feature-placement` rewrap are fixed.
- Artifacts: `review/review-framework.md`, `review/round-1/general.md`, `review/round-2/merged.md`, `review/disposition.md`.

### Pull request body

```markdown
## Summary

Every agent run loads `AGENTS.md`. It had grown into a second copy of the skills, the analyzer catalog, and `dev` help, and some of those copies had drifted. It is now the short core: what this repo is, hard rules nothing else enforces, and pointers. Situational rules moved into the skill or `AnalyzerReleases.Unshipped.md` comment that owns them. Full verdict table: `kanban/to-do/294-audit-and-trim-agentsmd-to-a-lean-core/audit.md`.

## Size

| | Bytes | Lines | Words |
|--|------:|------:|------:|
| Before (master, 2026-10-10) | 33,098 | 408 | 4,000 |
| After | 3,566 | 54 | 481 |

Target was well under 8 KB (8,192 bytes).

## Contradictions

1. **Opening a task.** Old file and `kanban/overview.md` said `ganda kanban create`. `tw-kanban` says create is a deprecated alias of reserve + first claim. **Winner: `tw-kanban`.** Overview updated.
2. **Purpose diagnostic id.** Analyzer is `TWA0004`. Flow skill `tw-agent-context-regions` says `TWPA0004`. **Winner: TWA0004.** Flow skill not edited (other repo). Lean core states the real id.
3. **Purpose scope.** Analyzer: every source file, generated code exempt. Flow `tw-csharp`: non-trivial files only. **Winner: the analyzer.** Flow skill not edited.
4. **Test file names.** Flow `tw-jaribu`: dotted `{sut}.{action}.cs`. This repo: kebab `*-tests.cs` (TWA0015/TWA0016). **Winner: this repo.**
5. **`Lazy` hosts.** Flow `tw-jaribu`: static/`Lazy` is fine when nothing needs dispose. Local co-located reference: never process-static `Lazy` for ASP.NET hosts (Testcontainers postgres is the documented exception). **Winner: the local reference for hosts.**
6. **`endpoint` function.** Skill said "currently unused." Three `EndpointWithoutRequest` files use it. **Winner: the code.** Rule moved to `tw-web-api-contracts`.
7. **Misplaced files.** Skill named TWA0004. TWA0004 is missing Purpose. Misplacement is the membership guard plus TWA0015/TWA0016. **Winner: those analyzers.** Skill fixed.
8. **HTTP verb.** Skill said the verb must match the server endpoint. TWA0005 is retired; generated FastEndpoints take the verb from `[ApiRoute]`. **Winner: the analyzer comment.** Skill and Unshipped comment updated.
9. **Board Definition of Done.** Overview still lists a Mapper, a hand-written Endpoint, and RequestValidator unit tests. `tw-web-api-contracts` says generated FastEndpoints, no mapper, no isolated validator unit tests. **Winner: the contracts skill.** Overview banner added; old checklist left in place.

Checked and not contradictions: fail-closed auth wording, `IAuthApiRequest` vs TWA0014, TW0001 warning in `.editorconfig`, no Estimate field, SDK pin matched `global.json` on 2026-10-10 (lean file now points at `global.json`).

## Pack and sync

Deliberately different. The template pack allow-list does not include `AGENTS.md` or `CLAUDE.md`. Generated apps get `skills/`. `ganda agents targets` syncs only timewarp-flow global prefs, not this repo.

## Test plan

- [x] `./bin/dev build` — 0 warnings, 0 errors
- [x] `ganda repo audit` — Passed 31, Failed 0
- [ ] Merge only via `ganda pr merge` after Steven approves
```
