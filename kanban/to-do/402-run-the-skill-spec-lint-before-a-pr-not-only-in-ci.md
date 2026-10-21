# Run the skill spec lint before a PR, not only in CI

## Description

CI's "Lint skill specs" job (`vally lint skills`, `.github/workflows/workflow.yml`) checks every
`skills/*/SKILL.md` against the skill spec: at most 500 lines and a description of at most 1024
characters. Nothing runs it before a PR, so it has failed only in CI three times:

- 386: `tw-web-api-contracts/SKILL.md` 504 lines
- 393: `tw-deploy/SKILL.md` 516 lines and a 1203-character description
- 396: `tw-deploy/SKILL.md` 503 lines

Each time the PR went red after the worker's gates passed, and the fix was a hand trim.

## Checklist

- [ ] A `dev` verb (e.g. `dev lint-skills`) runs the same `vally lint skills` (and the eval-spec lint) CI runs, with the same vally version; CI calls that verb so the two cannot drift
- [ ] It fails with vally's message and the file, and exits non-zero
- [ ] AGENTS.md "Before a PR" row: run it when `skills/` changes
- [ ] Decide with Steve whether `dev build` or `ganda repo audit` should run it too (proposal: the `dev` verb plus the AGENTS.md gate; audit is ganda's and is out of this repo)
- [ ] When vally (Node) is missing, the verb says how to install it instead of skipping silently
- [ ] Test or smoke: an over-limit SKILL.md fails the verb

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-22)

## Notes

- Origin: Steve 2026-10-22, "yes" to filing this after the third CI-only lint failure (PR #548).
- `tw-deploy` was trimmed to 453 lines in 396 (`references/deploy-migrate.md`), so it has headroom now.
