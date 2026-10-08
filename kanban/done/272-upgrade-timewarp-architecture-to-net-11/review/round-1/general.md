# Round 1 — general
**Date:** 2026-10-09
**Scope reviewed:** branch task/272 vs master (bf7bb258c)

## Summary
The upgrade is consistent. All 66 global.json files pin 11.0.100-rc.1.26425.128, and the TFM, CPM pins, dotnet-ef, CI setup-dotnet and the template pack csproj are updated. The pack csproj lost no content items. No remaining net10 references outside intentional or historical places. Findings are one suggestion on suppression scope, one on a production retry budget, and two nits.

## Issues

### Issue 1 — Severity: suggestion
- File: tools/dev-cli/Directory.Build.props:5-14
- Description: The tool-wide NoWarn adds ten IDE ids (IDE0005, IDE0022, IDE0046, IDE0055, IDE0058, IDE0065, IDE0066, IDE0078, IDE0160, IDE0290) plus IDE0211. The justification is that they fire in TimeWarp.Nuru.DevCli content files compiled into the runfile. The task says "no blanket suppressions". The suppression is scoped to tools/dev-cli, but it also hides these ids in the repo's own CLI files.
- Suggestion: If the Nuru content files have a stable path or pattern, scope the suppression with a per-file `.editorconfig` section (`generated_code = true` or per-id severity none) instead of a project-wide NoWarn. Otherwise keep it and record the limitation in the task Results.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/container-apps/web/features/identity/site-settings-seed-hosted-service-server.cs:41-42
- Description: The production seed retry budget goes from 30 to 180 attempts (QuietAttempts 5 to 120) to absorb a ~90s dev-time EF tool verbose stream under Aspire. In a real deployment where migrations genuinely failed, the host now blocks startup for 3 minutes. It logs Information-level (not Warning) for the first 2 minutes. The Design header is updated, but the rationale is an RC1 dev-loop artifact.
- Suggestion: Make this a conscious decision. Either bound the long budget to the Aspire/Development case (options or config), or confirm that the 180s budget plus quiet-until-120s logging is intended for production. Revisit at GA if the verbose startup cost goes away.
- Status: open

### Issue 3 — Severity: nit
- File: tests/container-apps/web/web-server-integration-tests/web-server-integration-tests.csproj:24-26
- Description: The comment says a direct reference pins Microsoft.Extensions.Hosting 10.0.9 to resolve NU1107. That PackageReference no longer exists (NU1510 removal), so the comment describes nothing and names a stale version.
- Suggestion: Delete the comment, or rewrite it if the NU1107 issue is still handled some other way.
- Status: open

### Issue 4 — Severity: nit
- File: timewarp-templates/source/timewarp-architecture-template/timewarp-architecture-template.csproj:1
- Description: The commit converts the file from CRLF to LF, so the whole file shows as changed for a one-line TFM edit (`git diff -w --ignore-cr-at-eol` shows 1 line). No content was dropped.
- Suggestion: None needed (the boyscout policy allows it). Mention it in the PR body so reviewers diff with `-w`.
- Status: open
