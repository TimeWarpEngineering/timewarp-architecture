# Round 1 — merged findings
**Date:** 2026-11-04
**Sources:** general (general-apphost pass, general-web-server pass)

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 3 | 0 |
| nit | 0 | 1 | 1 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-server/configuration/sample-options-validator.cs:6 (+7 files)
- Description: 8 Purpose/Design regions outside web-server `program.cs` still name the removed
  `Program.ConfigureSettings` (7 files) or `Program.ConfigureAuthentication` (1 file). See general.md
  Issue 1 for the list.
- Suggestion: point them at `WebServerSettings.ConfigureServices` / `WebServerAuthentication`.
- Source: general (web-server pass); re-verified by grep
- Disposition notes: fix pass 2026-11-04. All 8 files now name `WebServerSettings.ConfigureServices` (7 validators/options) or `WebServerAuthentication` (cookie-browser-session-service-server.cs). sign-in-options-validator-application.cs said "web-server ConfigureSettings" and was fixed too. Grep for `Program.ConfigureSettings` / `Program.ConfigureAuthentication` across source, tests and skills is empty.

### M2 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-server/web-server-state-dev-tools.cs:6; web-server-authentication.cs:177
- Description: the moved text names `AddStateDevToolsExport` (now `WebServerStateDevTools.AddExport`)
  and "(ConfigureSettings)" (now `WebServerSettings`).
- Suggestion: rename in the text.
- Source: general (web-server pass); re-verified by grep
- Disposition notes: fix pass 2026-11-04. web-server-state-dev-tools.cs Design says `AddExport`; web-server-authentication.cs says "AgentTokenOptions (WebServerSettings)".

### M3 — Severity: suggestion — Status: fixed
- File: source/container-apps/aspire/projects/aspire-app-host/postgres-resources.cs:107, :129
- Description: "(see the Design region)" points to reasoning that now lives in
  `publish-target.cs`: the aca-neon pooled reference after the AddEFMigrations snapshot, and the
  Flexible Server postgres-db-password / postgres-db-uri secrets.
- Suggestion: move those paragraphs into the postgres-resources.cs Design region, or change the
  pointers to name publish-target.cs.
- Source: general (AppHost pass); re-verified by reading both Design regions
- Disposition notes: fix pass 2026-11-04. Both pointers now name PublishTarget's Design region in publish-target.cs (the aca-neon paragraph and the aca Postgres bullet). The reasoning stays in one place, beside the other per-target notes.

### M4 — Severity: nit — Status: wontfix
- File: source/container-apps/aspire/projects/aspire-app-host/ingress-resource.cs:126, :316; aca-neon-compute.cs:55
- Description: with api, grpc, and web all off, these parameters are unused, and RCS1163 is a
  warning, so the build would fail. The smoke matrix does not cover that combination, so this is
  unverified.
- Suggestion: use a discard or guard if the combination is supported. Otherwise mark wontfix with
  the rationale.
- Source: general (AppHost pass)
- Disposition notes: fix pass 2026-11-04. Verified by building the AppHost with only `postgres` defined and with `yarp;postgres` (`-p:DefineConstants=…`). RCS1163 fires at all three sites, so the finding is real for that combination. But the yarp-on form already failed on master before this task: `global using Aspire.Hosting.Yarp.Transforms` is IDE0005 when web and grpc are both off. The yarp-off form is an AppHost with no resource except the SigNoz link. Two fixes were tried and dropped. An `AppHostProjects.All` list just moves the diagnostic to CA1822 when the list is empty. Guarding ConfigureRoutes moves RCS1163 to AddPublishedImage. A real fix is a `(api || grpc || web)` guard across program.cs, aca-neon-compute.cs, ingress-resource.cs, global-usings.cs and template.json. It would also need a smoke-matrix variant, or it rots again: the matrix only turns off one flag at a time. That is a support decision for the template's flag set, not part of this refactor.

### M5 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-server/web-server-http-clients.cs:30; web-server-payment-services.cs:11; brand-stylesheet-endpoint.cs:12
- Description: no blank line between the namespace declaration and the first XML doc comment, unlike
  the other files in the folder.
- Suggestion: add the blank line.
- Source: general (web-server pass)
- Disposition notes: fix pass 2026-11-04. Blank line added after `namespace …;` in all three files.

## Duplicates / conflicts

- The two passes had no overlapping findings. The web-server pass reported the stale pointers as
  three items. Its pointers in other files became M1, and its two pointers inside the moved files
  were merged into M2.

## Verified equivalent (no finding)

- AppHost: PublishTarget.Declare, the project declarations, WebServerResource, and PostgresResources
  (the three database branches, the migrations chain, the AddEFMigrations snapshot order, and no
  bundle for Kubernetes). Also IngressResource in run and publish mode (routes and transforms in the
  same order, passkey RP hosts), AcaNeonCompute.ApplyTo after the ingress, and SigNoz first.
  Resource names come from constants.
- Web.Server: service registration order (with `PreferHostedIdentitySessionState` after Web.Spa),
  cookie/agent-token/mock/Entra schemes, ValidateOnStart options, `#if grpc` / `#if postgres`
  guards, middleware unchanged, and the brand stylesheet still mapped first.
- template.json exclusions match where each file is used. For each single flag turned off, no
  excluded symbol is referenced.
- dev-cli-tests read every top-level AppHost `.cs`. No other file adds an `AddParameter` call that
  would change their results.
