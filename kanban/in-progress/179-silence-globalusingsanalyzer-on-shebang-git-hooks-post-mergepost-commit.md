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
- [ ] PR green; merge

## Session

- Repro: `git pull` on master after #299; hook path `.githooks/post-merge(6,1)`.
- Fix: tree-local Directory.Build.props (`RunAnalyzers=false`, `AnalysisLevel=none`, …).
