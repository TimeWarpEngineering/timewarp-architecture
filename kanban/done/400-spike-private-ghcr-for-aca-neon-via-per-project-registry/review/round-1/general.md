# Round 1 — general
**Date:** 2026-10-22
**Scope reviewed:** `git diff e8f753ead..HEAD`: the aca-neon guide section and the AppHost Design comment

## Summary

The spike result is correct and complete against the task and the restatement. On Aspire 13.6.2,
per-project `WithContainerRegistry(ghcr)` still publishes `aca-env-acr` at SKU Basic. Each container
app still pulls through managed identity. The probe was removed. There was no deploy and no token,
and the live-proof item stays open on purpose. Spot checks on the local 13.6.2 assemblies match the
guide. `Aspire.Hosting.Azure.AppContainers` contains `CreateDefaultAzureContainerRegistry`,
`DefaultContainerRegistry`, `AllocateContainerRegistryParameters`, `AddContainerRegistryParameters`,
the `-acr` suffix literal, and the "not an Azure Container Registry" message. `Aspire.Hosting`
contains `AddContainerRegistry`, `WithContainerRegistry`, `RegistryTargetAnnotation`, and
`ContainerRegistryReferenceAnnotation`. `dev publish aca-neon` needs no credentials, so the
validation smoke can run. The AppHost change is comment-only, inside the existing Design region.
There is no build risk. The remaining issue is the guide's register. It ships in generated apps.

## Issues

### Issue 1 — Severity: suggestion
- File: skills/tw-deploy/references/azure-container-apps-neon.md:23
- Description: The new "Why the registry stays ACR" text is a spike log, not reference material.
  `skills/` ships in every generated app (AGENTS.md: "Repo skills under `skills/` ship in generated
  apps … A live rule belongs in the skill that owns it"). Skills state current facts. History
  belongs in the task. The section narrates this task's run in past tense:
  - "Task 400 published that shape and stopped."
  - "No GitHub token was created, and nothing was deployed." (line 23)
  - "Task 400 did not attach the GHCR registry to the environment." (line 49)
  - "Probe (removed from the AppHost after publish; Steve decides before any template change)",
    which names the maintainer, and `--output-path /tmp/task-400-aca-neon-ghcr`, "placeholder Neon
    parameter values" (line 51)
  - "`aspire deploy` was not run. The checklist stops …" (line 56)
  - "That callback was not added" (line 73)
  - "No token was created, stored, or printed." (line 77)
  - "This spike did not add that secret." (line 79)

  A generated-app operator cannot act on that text. The same narrative is already in task.md
  `## Results`.
- Suggestion: Rewrite the section in present tense as facts about Aspire 13.6.2. Example: "With
  `WithContainerRegistry(ghcr)` on every project, `aspire publish` still writes
  `aca-env-acr/aca-env-acr.bicep` (SKU Basic) …". Short task-id provenance in parentheses is fine;
  sibling guides use "(task 070-008)" the same way. Drop the probe and run log: the /tmp path, the
  "not run / not created / not added" lines, and "Steve decides". Those stay in task.md Results.
  Keep these: the API table, the default-ACR mechanics, the pull shape with the Bicep excerpt, the
  token scopes and where the token would live, the recommendation, and the revisit condition.
- Status: open

### Issue 2 — Severity: nit
- File: skills/tw-deploy/references/azure-container-apps-neon.md:39
- Description: "The Aspire CLI docs article 'Container registry configuration' shows the ACA example
  as `WithContainerRegistry(environment)` …" names a docs page but gives no link. Commit
  `c58e4ef1c` removed the link because the guessed URL returned 404. The reader cannot check the
  claim. The task asks for the docs to be cited.
- Suggestion: Cite a reachable aspire.dev page that shows `WithContainerRegistry(environment)` for
  ACA, or drop the sentence. The next sentence's point stands without it: the environment's ACR is
  the managed-identity path, and a `ghcr.io` `ContainerRegistryResource` is a different registry.
- Status: open
