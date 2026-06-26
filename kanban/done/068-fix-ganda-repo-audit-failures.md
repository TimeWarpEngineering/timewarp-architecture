# Fix ganda repo audit failures

## Description

`ganda repo audit` reports 6 failing checks (14 pass, 2 skip) on the `dev` worktree.
These are NEW failures distinct from the set fixed in task 057 (all of 057's checks —
assembly-metadata, banned-symbols, the 55 missing PackageVersions, directory-structure,
workflow-file — still pass). Some are regressions/version drift since 057 (2026-06-11);
others come from audit checks added later. Bring the repo back to a clean audit.

## Requirements

All 6 failing checks below pass and the audit reports no blocking failures.

### 1. bin-dev (Error)

`bin/dev` is missing — `bin/` does not exist at all. `.envrc` still has `PATH_add bin`,
so the dev CLI entrypoint is expected there. Restore/regenerate `bin/dev`.

### 2. dev-cli-capabilities (Error)

Cascade of bin-dev — "cannot run --capabilities" because `bin/dev` is missing. Should
clear once bin-dev is fixed; re-verify.

### 3. cpm-consistency (Error)

1 orphaned `PackageVersion`: `Microsoft.Playwright` (1.55.0 in
`Directory.Packages.props`) is declared but no longer referenced by any project. Either
remove the orphaned entry or restore the reference if a project still needs it.

### 4. nuru (Error)

`TimeWarp.Nuru` is outdated: `Directory.Packages.props` pins `3.0.0-beta.70`, latest is
`3.0.0-beta.71`. Bump the pin (and verify build/tests still pass).

### 5. runfile-shebang (Error)

1 runfile has a non-standard shebang: `tools/dev-cli/dev.cs` uses
`#!/usr/bin/dotnet --`. Replace with the standard runfile shebang the audit expects.

### 6. memsearch-scaffold (Warning)

Memsearch scaffold incomplete — missing `.memsearch.toml`, `.githooks/post-commit`,
`.githooks/post-merge`, and `git config core.hooksPath=.githooks`. Scaffold these (or
confirm intentionally opted out). Warning-level, not blocking.

## Checklist

- [x] Restore `bin/dev` (fixes bin-dev + dev-cli-capabilities) — `--fix` self-install
- [x] Remove orphaned `Microsoft.Playwright` PackageVersion (or restore its reference)
- [x] Bump `TimeWarp.Nuru` 3.0.0-beta.70 → beta.71
- [x] Fix `tools/dev-cli/dev.cs` shebang to the standard runfile form
- [x] Complete memsearch scaffold (.memsearch.toml, .githooks/*, core.hooksPath)
- [x] `ganda repo audit` passes with no blocking failures

## Notes

Audit run on 2026-06-25 against the dev worktree: 14 passed, 6 failed, 2 skipped.
Task 057 (done) fixed an earlier, unrelated set of audit failures — do NOT reopen it.

## Implementation Notes

Completed 2026-06-25. Final audit: 20 passed, 0 failed, 2 skipped.

- `ganda repo audit --fix` auto-resolved 5 of 6: bin-dev (self-install regenerated
  `bin/dev`, which is a generated/gitignored artifact), dev-cli-capabilities (cascade),
  nuru (pinned beta.71 in Directory.Packages.props), runfile-shebang (rewrote
  `tools/dev-cli/dev.cs`), memsearch-scaffold (created `.memsearch.toml`, `.githooks/`
  post-commit + post-merge, and set core.hooksPath).
- cpm-consistency was the only non-auto-fixable check (needs a per-package decision).
  Verified `Microsoft.Playwright` had zero references across source/tests/runfiles/tools
  (the E2E Playwright project is not in the audited root tree), so removed the orphaned
  `PackageVersion` from Directory.Packages.props.

## Session

- Created: ses_current (2026-06-25)
- Implementation: ses_current (2026-06-25)
