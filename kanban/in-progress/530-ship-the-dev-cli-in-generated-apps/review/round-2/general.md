# Round 2 — general
**Date:** 2026-11-26
**Scope reviewed:** fix delta `f88c6cd88` vs its parent (M1, M2). Product files: `tools/dev-cli/services/app-features.cs`, `tools/dev-cli/endpoints/deploy-command.cs`, `tests/tools/dev-cli-tests/app-features-tests.cs`, `readme.md`.

## Summary

`AppFeature` now carries `Requires`. Postgres requires web, and `Require` reports the first missing required feature, so `dev deploy migrate` in a `--web false` app names web. `dev deploy` checks yarp, then web, and returns before preflight. The new marker test allows an exclude to cover a marker only when every negated flag in that condition is the feature's own flag or a required one. `readme.md` says the same thing.

The Goal restatement still matches the original task. The fix-pass paragraph is the review fix, not a misread.

No new issues. The recorded fix-pass gates cover the delta (build, 6/6 `app-features-tests`, the two manual refusals, audit). They were not re-run. A targeted Python replay of the marker rule against `.template.config/template.json` found the postgres marker removed only by `(!web)` and `(!postgres)`, both allowed.

## Issues

<!-- None. -->
