# Round 1 — general
**Date:** 2026-10-07
**Scope reviewed:** commits 2094f4636 and 98bdb0a3c: deploy-command.cs, deprovision-command.cs, aspire-deploy.cs, aspire-deploy-preflight.cs, template-smoke-harness.cs (skill count), aspire-app-host program.cs Design region, skills/tw-deploy/SKILL.md deploy/deprovision/kind sections. Argument construction checked against `aspire deploy --help` / `aspire destroy --help` (CLI 13.6.0).
## Summary
No correctness defects found. Argument lists match the installed CLI (`--apphost`, `--environment`, `--non-interactive`, `destroy --yes`, `--` passthrough). Destroy cannot run without --yes and a recorded deployment, deploy cannot run non-interactively without --yes, and no container CLI is invoked. Only two minor suggestions/nits remain.
## Issues
### Issue 1 — Severity: suggestion
- File: tools/dev-cli/endpoints/deprovision-command.cs:59
- Description: For kubernetes, the preflight prints the CURRENT kubectl context, but the deployment record (Helm:k8s ReleaseName/Namespace) does not store which cluster the deploy went to. If the operator switched context since `dev deploy`, `--yes` runs `aspire destroy` against whichever cluster is current (at worst a same-named release in another cluster is uninstalled).
- Suggestion: In the --yes-less refusal output and the skill, call out explicitly that the printed context is where destroy will act; optionally have `dev deploy` record the context (e.g. in a sidecar file) and warn on mismatch.
- Status: open
### Issue 2 — Severity: nit
- File: tools/dev-cli/endpoints/deploy-command.cs:55
- Description: When the operator answers "n" at the prompt, the message printed is DeployConfirmationRefusal ("no confirmation. Re-run with --yes ... or from a terminal to answer the prompt"), which is worded for the non-terminal case and reads oddly after a deliberate "no".
- Suggestion: Print a short "Deploy cancelled." for the declined case and keep the refusal text for the redirected-stdin case.
- Status: open
