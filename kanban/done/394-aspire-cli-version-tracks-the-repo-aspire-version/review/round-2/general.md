# Round 2 — general
**Date:** 2026-10-21
**Scope reviewed:** fix delta `06881a0dc..949eb3dd8` (product paths: `tools/dev-cli/services/aspire-run.cs`,
`tools/dev-cli/services/aspire-cli.cs`, `tests/tools/dev-cli-tests/aspire-run-tests.cs`) and the updated
`## Goal (implementer restatement)`. Re-verified round-1 M1 and M2.

## Summary

The fix pass closes both round-1 findings. The workflow check now strips `#` comments outside quotes
and looks only at `dotnet tool install … Aspire.Cli` lines. Each line must pass `--version` with a shell
variable, not a literal. That variable must be assigned on a line that runs `sed` over
`<AspireVersion>` in `Directory.Packages.props`. `ParseRepoAspireVersion` now shares `ParseVersionCore`
with `ParseCliVersion`, so `14.0.0-preview.1.26600.1` compares as `14.0.0`. A present value that is not a
version is named in the refusal, both in the agreement check and in `AspireCli.ValidateVersionAsync`.

Re-verified here:
- `cd tests/tools/dev-cli-tests && dotnet test -c Release`: 308 passed, 0 failed. That includes the five new
  cases, and `RepoFiles_Should_ShareOneAspireVersion` against the real `workflow.yml`.
- `dotnet build tools/dev-cli/dev.cs -c Release`: succeeded with no warnings.
- `.github/workflows/workflow.yml:232` (`version=$(sed -n 's/.*<AspireVersion>…/p' Directory.Packages.props)`)
  and `:238` (`--version "$version"`) satisfy the new check. The comment at `:227` is stripped, so it no
  longer satisfies the check on its own.
- The new tests match the round-1 failure cases: comment-only with no `--version`,
  `${{ vars.ASPIRE_CLI }}`, `$other` vs the `sed`-assigned `$version`, a prerelease property, and an
  unparseable property.
- The Goal restatement now describes the M1/M2 behavior, and it matches the code.

Non-blocking observations, not raised as findings:
- `ReadRepoAspireVersionError` (aspire-run.cs:161) and `DescribeMissingRepoAspireVersion` (aspire-run.cs:147)
  both re-read the element and word the same condition differently. The first could call the second.
  This is cosmetic. No one has to keep the two messages in sync.
- `StripUnquotedHashComment` treats a bare apostrophe in YAML prose as an open quote. The only effect is that
  the comment stays in. A false pass would also need a fake `sed` assignment hidden in that comment. That is
  contrived.

## Issues

None new. Round-1 M1 and M2 are fixed (see merged.md).
