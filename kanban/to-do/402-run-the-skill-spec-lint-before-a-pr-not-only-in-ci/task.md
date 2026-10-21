# Run the skill spec lint before a PR, not only in CI

## Goal (implementer restatement)

Skill spec lint (`vally lint skills` and `vally lint --eval-spec` on each `skills/*/evals/eval.yaml`) runs only in the CI job "Lint skill specs". A PR can pass every local gate and still go red because a `SKILL.md` is over 500 lines or its description is over 1024 characters. That happened on 386, 393, and 396.

This task adds one `dev` verb, `dev lint-skills`, that runs those same vally commands. The vally package version is pinned in one file. CI installs that pin and calls the verb, so the commands and the CLI version cannot drift from what a developer runs. The verb exits non-zero and shows vally's own message, including which skill failed. When `vally` is not on `PATH`, it prints how to install Node.js and that pinned package instead of succeeding. `AGENTS.md` tells you to run the verb before a PR when `skills/` changes.

`dev build` stays the compile gate. `ganda repo audit` stays ganda's; this repo does not add a skill-lint audit check. An over-limit `SKILL.md` must fail the verb.

## Description

CI's "Lint skill specs" job (`vally lint skills`, `.github/workflows/workflow.yml`) checks every
`skills/*/SKILL.md` against the skill spec: at most 500 lines and a description of at most 1024
characters. Nothing runs it before a PR, so it has failed only in CI three times:

- 386: `tw-web-api-contracts/SKILL.md` 504 lines
- 393: `tw-deploy/SKILL.md` 516 lines and a 1203-character description
- 396: `tw-deploy/SKILL.md` 503 lines

Each time the PR went red after the worker's gates passed, and the fix was a hand trim.

## Checklist

- [x] A `dev` verb (e.g. `dev lint-skills`) runs the same `vally lint skills` (and the eval-spec lint) CI runs, with the same vally version; CI calls that verb so the two cannot drift
- [x] It fails with vally's message and the file, and exits non-zero
- [x] AGENTS.md "Before a PR" row: run it when `skills/` changes
- [x] Decide with Steve whether `dev build` or `ganda repo audit` should run it too (proposal: the `dev` verb plus the AGENTS.md gate; audit is ganda's and is out of this repo)
- [x] When vally (Node) is missing, the verb says how to install it instead of skipping silently
- [x] Test or smoke: an over-limit SKILL.md fails the verb

## Results

`dev lint-skills` runs `vally lint skills` and then `vally lint --eval-spec` for each `skills/*/evals/eval.yaml`. The vally version is `tools/dev-cli/vally-cli.version` (`0.19.0`, the package CI was installing unpinned). The skill-lint job reads that file, installs `@microsoft/vally-cli@$version`, and runs `dotnet run tools/dev-cli/dev.cs -- lint-skills`. A workflow that calls `vally lint` itself, or installs a literal or unpinned package, fails `SkillSpecLint.WorkflowDriftError`. The job also runs when `tools/dev-cli/` changes.

A missing `vally` exits 1 and names Node.js plus `npm install -g @microsoft/vally-cli@0.19.0`. A different version exits 1 with the same install. A lint failure exits with vally's code and vally's text. `./bin/dev` is the AOT binary and does not include this verb until the next self-install. The checks below used `dotnet run tools/dev-cli/dev.cs`.

`dev build` does not run the lint. `ganda repo audit` is unchanged. That is the proposal already in the checklist: the verb plus the AGENTS.md "Before a PR" row. Audit lives in ganda, outside this repo.

### How to validate

Smoke:

```bash
dotnet run tools/dev-cli/dev.cs -c Release -- lint-skills
cd tests/tools/dev-cli-tests && dotnet test -c Release
```

Expect:

With `@microsoft/vally-cli@0.19.0` on `PATH`, `lint-skills` prints `9 skill(s) linted, 9 passed`, then `skills/tw-web-api-contracts/evals/eval.yaml is valid`, then `Skill specs linted successfully!`, and exits 0. With no `vally` on `PATH`, it exits 1 and prints `vally is not on PATH` and `npm install -g @microsoft/vally-cli@0.19.0`. With another version (this machine had 0.6.0), it exits 1 and prints that version, the pin, and the same install command. An otherwise empty git repo whose `skills/too-long/SKILL.md` is over 500 lines exits 1 with vally's text `too-long` and `File length (518 lines) exceeds limit (500).` dev-cli-tests passes (the pin, the CI agreement, and the install messages are in that run).

Ran those checks on 2026-10-22. Missing `vally`: exit 1, install text as above. Installed 0.6.0: exit 1, `vally 0.6.0 is on PATH` and the 0.19.0 install. Pinned 0.19.0 on this repo: exit 0, 9/9 skills and the eval spec. Over-limit fixture: exit 1, `File length (518 lines) exceeds limit (500).` `dotnet build tools/dev-cli/dev.cs -c Release` succeeded. `cd tests/tools/dev-cli-tests && dotnet test -c Release`: 333 passed, 0 failed. `ganda repo audit`: Passed 32, Failed 0.

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-22)

## Notes

- Origin: Steve 2026-10-22, "yes" to filing this after the third CI-only lint failure (PR #548).
- `tw-deploy` was trimmed to 453 lines in 396 (`references/deploy-migrate.md`), so it has headroom now.
- Decision on the checklist's `dev build` / `ganda repo audit` item: adopt the proposal in that item. `dev lint-skills` and the AGENTS.md row are the gate. `dev build` stays the compile gate. This repo does not add a ganda audit check.
