# Silence GlobalUsingsAnalyzer on shebang git hooks (post-merge/post-commit)

## Description

After task 172 restored **GlobalUsingsAnalyzer** repo-wide (TreatWarningsAsErrors), the
`.githooks/post-merge` and `post-commit` shebang runfiles print:

```text
error GlobalUsingsAnalyzer: Move using TimeWarp.Amuru to global-usings.cs
The build failed. Fix the build errors and run again.
```

on every merge/commit. Hooks still exit 0 (pull succeeds), but the noise looks like a failed
pull. Single-file hooks have no project `global-usings.cs`, so the analyzer is a false positive.

## Checklist

- [x] `.githooks/Directory.Build.props` disables analyzers / TreatWarningsAsErrors for this tree
- [x] `post-merge` / `post-commit` run clean (memsearch index starts, no GlobalUsingsAnalyzer)
- [x] PR green; merge — product PR **300** (`2457d800`); kitchen re-id'd to **203** and moved to `kanban/done/` by task 202

## Notes

Formerly duplicate id 179; product PR **300**. CAS-reserved as **203** because 180–201 (and 202) already existed.

## Results

Tree-local `.githooks/Directory.Build.props` disables analyzers / TreatWarningsAsErrors so shebang `post-merge` / `post-commit` hooks no longer print GlobalUsingsAnalyzer false positives. Shipped as `2457d800` / PR **300**. Kitchen re-id'd from duplicate **179** to **203** and lives in `kanban/done/`.

### How to validate

**Smoke**

```bash
test -f kanban/done/203-silence-globalusingsanalyzer-on-shebang-git-hooks-post-mergepost-commit.md && echo ok
# Expect: ok

test ! -f kanban/in-progress/179-silence-globalusingsanalyzer-on-shebang-git-hooks-post-mergepost-commit.md && echo gone
# Expect: gone

ganda kanban path 203
# Expect: …/kanban/done/203-silence-globalusingsanalyzer-on-shebang-git-hooks-post-mergepost-commit.md

git log -1 --oneline 2457d800
# Expect: 2457d800 fix(githooks): disable analyzers for shebang post-merge/post-commit hooks
```

**Expect**

- Githooks-silence kitchen is **203** in `kanban/done/`, not a second architecture 179.
- Product change already on master via PR **300**.

## Session

- Repro: `git pull` on master after #299; hook path `.githooks/post-merge(6,1)`.
- Fix: tree-local Directory.Build.props (`RunAnalyzers=false`, `AnalysisLevel=none`, …).
- Board close: task 202 reserved 203 and renamed this kitchen out of the duplicate-179 pair (2026-08-26).
