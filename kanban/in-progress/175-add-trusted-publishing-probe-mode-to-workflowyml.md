# Add trusted-publishing probe mode to workflow.yml

## Description

org 458-009 probe (NuGet has no policy-enumeration API; probe = dispatch mode that runs only the nuget/login OIDC exchange and stops — success proves the workflow.yml policy matches; reference timewarp-nuru's workflow.yml).

## Checklist

- [x] probe input added
- [x] login step condition extended
- [x] probe-result step added
- [x] pipeline step skipped in probe mode
- [x] YAML valid

## Results

- Added `workflow_dispatch.inputs.mode` (choice: merge/probe, default merge) to `.github/workflows/workflow.yml`.
- In the `ci` job: extended the "NuGet login (OIDC Trusted Publishing)" step condition to also run on `workflow_dispatch` with `mode == 'probe'`.
- In the `ci` job: added a new "Trusted publishing probe result" step that echoes success once probe mode's OIDC login completes.
- In the `ci` job: gated the "Run CI Pipeline" step to skip when `workflow_dispatch` + `mode == 'probe'`, so probe mode never builds or publishes. Left the `ci` job's own job-level `if:` untouched — it already enters unconditionally on any `workflow_dispatch`, which is correct for probe mode too.
- In the `template-smoke` job: added `&& inputs.mode != 'probe'` to its job-level `if:` so a `dotnet pack`/install/generate/build smoke cycle does not run during a probe dispatch. `skill-lint` (PR-only) and `detect-paths` were left untouched as instructed.
- Two `git commit`s in this repo triggered the repo's known-broken post-commit hook (`GlobalUsingsAnalyzer: Move using TimeWarp.Amuru to GlobalUsings.cs` build failure). Post-commit hooks cannot block a commit that has already been made, so both commits succeeded (exit 0) without needing `--no-verify`.

### How to validate

**Smoke:** `gh workflow run workflow.yml -f mode=probe` after push → expect the "Trusted publishing probe result" step to run and go green.
**Expect:** a failure of the NuGet login step means the trusted-publishing policy is missing or misconfigured on NuGet.org for this repo + workflow.yml — not a bug in this change.
