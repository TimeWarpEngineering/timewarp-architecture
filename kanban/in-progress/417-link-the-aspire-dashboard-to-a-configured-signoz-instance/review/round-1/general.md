# Round 1 — general
**Date:** 2026-10-26
**Scope reviewed:** `skills/tw-deploy/SKILL.md`, `source/container-apps/aspire/projects/aspire-app-host/program.cs`, `tests/container-apps/aspire/aspire-tests/{signoz-link-model,compose-publish,kubernetes-publish,aca-publish,aca-neon-publish}-tests.cs` (branch vs merge-base with master)

## Summary

The AppHost reads `Observability:SigNoz:Url`, validates it as an absolute `https` URL (throwing with
the key name otherwise), and in run mode only adds `AddExternalService("signoz", uri)` and sets the
display text of its single `ResourceUrlAnnotation` to "SigNoz". The code is correct. Decompiling
Aspire.Hosting 13.6.2 shows that `AddExternalService(name, Uri)` calls `WithUrl(uri.ToString())`
synchronously (one direct `ResourceUrlAnnotation`) and `ExcludeFromManifest()`. The orchestrator
projects `DisplayText` into the dashboard `UrlSnapshot`. The init handler's `AddUrlIfNotPresent`
de-dupes by URL, so mutating the annotation gives one link labeled SigNoz. No URL is committed, and
the Goal restatement matches the task. The problems are a structural break in the shipped
`tw-deploy` skill, publish-output assertions that never exercise the configured case, and Design
region placement.

Verified locally: `dotnet test -- --filter-class SigNozLinkModel` passed 6/6, and
`dotnet test --no-build -- --filter-method HaveNoSigNoz` passed 3/3. A full `dev build` was not
re-run (the test build of the AppHost and test project compiled clean).

## Issues

### Issue 1 — Severity: suggestion
- File: skills/tw-deploy/SKILL.md:288
- Description: The new `### SigNoz link on the local dashboard` heading and its two code fences were inserted in the middle of the "Rules for new code:" bullet list. The last rule, "- Without the `yarp` flag there is no ingress: the published output exposes nothing…", now sits after the second code fence, under the SigNoz heading. That cuts it off from the rules list and attributes it to the SigNoz section. The skill ships in every generated app.
- Suggestion: Move the SigNoz subsection after the complete Rules list, after the `yarp` bullet and before `## Secrets and parameters`, so the Rules list stays intact.
- Status: open

### Issue 2 — Severity: suggestion
- File: tests/container-apps/aspire/aspire-tests/compose-publish-tests.cs:144 (also kubernetes-publish-tests.cs:168, aca-publish-tests.cs:148, aca-neon-publish-tests.cs:131)
- Description: The new `Publish_Should_HaveNoSigNoz` checks and the aca-neon `ShouldNotContain("signoz")` run against publish fixtures that never set `Observability:SigNoz:Url` (for example `["--operation", "publish", "--step", "publish-compose"]`). With the key unset, nothing is added in any mode. These assertions would still pass if the `IsRunMode` gate and `ExcludeFromManifest` were both removed. The task requires a check that `aspire publish` output is unchanged when the URL is set, and these checks prove nothing about that case. Only the model-level `PublishMode_Should_OmitSigNozWhenUrlIsSet` covers it.
- Suggestion: Pass `--Observability:SigNoz:Url=https://observability.example` to each publish fixture's args so that the output assertions cover the configured case. The URL is valid, so it does not change the rest of the published output. This also proves that a configured operator value does not break `aspire publish`. Otherwise, drop the vacuous output checks and rely on the model test.
- Status: open

### Issue 3 — Severity: nit
- File: source/container-apps/aspire/projects/aspire-app-host/program.cs:107
- Description: The SigNoz Design paragraph was inserted between the `Ingress:PublicUrl` paragraph and its continuation ("Authentication:Entra:PublicOrigin … is not copied from Ingress:PublicUrl …"). That splits one topic in two, and the PublicOrigin sentence now follows an unrelated SigNoz paragraph.
- Suggestion: Move the SigNoz paragraph after the PublicOrigin sentences, before "Public-host forwarding", or to another point that does not split the PublicUrl/PublicOrigin topic.
- Status: open
