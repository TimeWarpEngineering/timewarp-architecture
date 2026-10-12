# Update TimeWarp.Nuru to 3.0.0-beta.80

## Description

`ganda repo attest` refuses to sign master because its outdated-package audit flags TimeWarp.Nuru (and TimeWarp.Nuru.DevCli) at 3.0.0-beta.79 while 3.0.0-beta.80 is published. Task 331's publish commit 487bfde is unsigned as a result. Bump both pins in `Directory.Packages.props` to 3.0.0-beta.80 so the audit clears and master can be attested. Requested by Steven 2026-10-13. Precedent: task 283 (beta.79 bump).

## Checklist

- [ ] Bump `TimeWarp.Nuru` and `TimeWarp.Nuru.DevCli` to 3.0.0-beta.80 in `Directory.Packages.props`
- [ ] Fix any breaking changes from beta.80 (check Nuru release notes)
- [ ] `dev build` passes
- [ ] `ganda repo audit` reports no outdated Nuru packages

## Session

- Created: 1202770 (2026-10-12)

## Notes

Scope is the Nuru bump only. Do not change other packages unless beta.80 requires it.

## Learnings
