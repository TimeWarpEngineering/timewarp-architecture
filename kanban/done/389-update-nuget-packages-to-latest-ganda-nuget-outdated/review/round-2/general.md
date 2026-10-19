# Round 2 — general
**Date:** 2026-10-19
**Scope reviewed:** fix delta since round 1, `3a92e854a..a1c69f3fb`. Product file:
`Directory.Packages.props` (7 insertions, 6 deletions, XML comments only).

## Summary

The fix pass moves the TimeWarp.Nuru / DevCli 4.0.0-beta.2 rationale into a comment directly
above the Nuru pins and deletes it from the Microsoft.OpenApi block (M1). It restates the
Microsoft.Extensions.AI.OpenAI warning as observed on adapter 10.10.0 and not re-checked on
10.11.0 (M2). No `Version=` attribute changed. Risk is nil.

Checks run by the reviewer:

- `Directory.Packages.props` parses as well-formed XML (no `--` inside the new comments).
- `ganda repo audit`: "Repository passes all audit checks."
- The OpenApi comment block still records why Microsoft.OpenApi stays 2.12.2 and no longer
  mentions Nuru.
- Build, test, check-version and template-smoke were not re-run. The delta is comment-only, and
  the round-1 reviewer re-ran build, test, check-version and audit on the same pins.

Goal restatement vs task: the restatement matches the original task and adds the M1/M2
acceptance lines for this fix pass. No misread.

## Issues

<!-- None. -->

## Prior findings

- M1 — suggestion — fixed. `Directory.Packages.props:12` comment sits directly above the Nuru pins;
  the sentence is gone from the OpenApi block ending at line 154.
- M2 — nit — fixed. `Directory.Packages.props:165` now says the TypeLoadException was observed on
  adapter 10.10.0 and was not re-checked on 10.11.0.
