# Round 1 — general
**Date:** 2026-11-24
**Scope reviewed:** branch `task/501-disable-ci-playwright-in-workflow-yml` vs `master`. Product diff is `.github/workflows/workflow.yml` and `tools/dev-cli/services/test-groups.cs`. Compared with `## Goal (implementer restatement)` and the original requirements. Gates were not re-run; the implementer's `## Results` and `### How to validate` cover build, dev-cli-tests, and audit.

## Summary

The workflow matrix now has only `fast`, `integration`, and `publish`. The `Upload Playwright screenshots` step is gone. The header comment names those three jobs and says the playwright group is off in CI while task 502 weighs the suite, and that `dev test --group playwright` still runs it. `TestGroups.Playwright`, `Names`, and `GroupsOf` are unchanged; only the Purpose line and the playwright Design line changed, and both now say CI does not run that group.

The `ci` job still needs `detect-paths` and `ci-group`, and it fails unless detect-paths succeeded and `ci-group` is `success` or `skipped`. A three-leg matrix that passes reports `success`, so the stable required check name stays `ci`. Task 366 records that protection, when it requires CI, requires that aggregate name. The coverage upload still keys off `matrix.name != 'publish'`, which still matches `fast` and `integration`.

Recorded gates match the diff: `./bin/dev build` succeeded with 0 warnings and 0 errors; `dotnet run --project tests/tools/dev-cli-tests -c Release --no-build` was 343/343; `ganda repo audit` passed. The dev-cli tests that read `workflow.yml` check Aspire version drift and skill-lint pin drift, not the matrix names, so a green run does not by itself prove the leg is gone. The diff does. The restatement matches the original task: drop the CI leg, keep the local suite, and leave the required `ci` check able to pass. No misread.

Sentences in the Playwright test Design regions and in `skills/tw-blazor-css-strategy/SKILL.md` still say CI uploads screenshots. The task keeps that test project intact and names only the `test-groups.cs` comment as the comment to correct. Those sentences are leftover description, not a broken workflow contract. Not raised.

## Issues

None.
