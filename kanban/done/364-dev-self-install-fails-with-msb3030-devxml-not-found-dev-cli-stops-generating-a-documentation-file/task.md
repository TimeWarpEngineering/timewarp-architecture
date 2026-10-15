# dev self-install fails with MSB3030 dev.xml not found; the dev CLI stops generating a documentation file

## Problem (2026-10-15, after the .NET 11 RC2 bump, task 362)

`dev self-install` (the AOT publish of the `tools/dev-cli/dev.cs` runfile) fails:

```
Microsoft.NET.Publish.targets(396,5): error MSB3030: Could not copy the file
"~/.local/share/dotnet/runfile/dev-<hash>/bin/release/dev.xml" because it was not found.
```

It reproduced on master and in fresh worktrees, and survived `ganda runfile cache --clear`. The
build writes `dev.xml` only under `obj/release`, while `bin/release` holds just `native/`, and the
publish step tries to copy the doc file from `bin/release`.

## Fix

`tools/dev-cli/Directory.Build.props` sets `GenerateDocumentationFile=false`.
- The root `Directory.Build.props` enables the documentation file only so **IDE0005** can run
  during build (Roslyn #41640).
- `tools/dev-cli` already suppresses IDE0005, so the dev CLI has no use for the file.
- The reasoning is in a comment beside the property.

## Checklist

- [x] `GenerateDocumentationFile=false` for the dev CLI, with the reason
- [x] Verified: `dotnet run tools/dev-cli/dev.cs -- self-install` succeeds (it failed in the same
      worktree before the change); `bin/dev --help` runs
- [x] `dev build`: 0 warnings, 0 errors
- [x] `ganda repo audit`; dev-cli-tests 226/226

## Session

- Created: 2026-10-15 (cockpit, from Steve's self-install failure)

## Results

- Boyscout: `ganda repo audit --fix` added the root `.gitignore` lines for the new `kanban-porcelain-gitignore` and `routine-journals-gitignore` checks.
- Boyscout: untracked the review-round markers (`kanban/done/{300,341,344,362,363}-*/review/round-1/closed`) the new checks flag; the files stay on disk, ignored.
