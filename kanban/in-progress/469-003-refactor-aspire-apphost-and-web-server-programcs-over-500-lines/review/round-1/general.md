# Round 1 — general
**Date:** 2026-11-04
**Scope reviewed:** branch HEAD 8edf0ea9a vs merge-base b16e2ce15. The change is in commit 15e93cb37. Two
read-only passes: the AppHost passes (`aspire-app-host/*.cs`, `template.json`, dev-cli-tests,
aspire-tests) and the Web.Server pass (`web-server/*.cs` and the Design pointers that lead into it).

## Summary

Both composition roots were split into concern types. The AppHost `program.cs` went from 942 to 101
lines and the Web.Server `program.cs` from 679 to 381. A statement-by-statement comparison against
the old files found no behaviour change: same arguments, flag guards, and run/publish-mode
conditions. Order also holds where it matters. web-server's environment and api/grpc references
are attached before `AddEFMigrations`, the Neon pooled reference after it, and
`PreferHostedIdentitySessionState` is still registered after Web.Spa's. Both of the implementer's
dead-code claims check out. The `Ingress:Port` / `Ingress:HttpPort` pins were
`isRunMode && TryParse(...)`, so they were always null in the publish branch. `AddPublishedImage`
only runs outside run mode, and Aspire's operation is Run or Publish. The remaining issues are
comment pointers that still name methods or Design regions the move removed, plus two nits.

Gates re-run by the review oracle: `./bin/dev build` (0 warnings, 0 errors) and
`./bin/dev test --group fast` (14/14 projects, dev-cli-tests 342/342). The line counts are 101 and
381. Not re-run this round: `dev template-smoke --matrix` and `dev test --group integration`.

## Issues

### Issue 1 — Severity: suggestion
- File: source/container-apps/web/projects/web-server/configuration/sample-options-validator.cs:6 (and 7 more)
- Description: Purpose/Design regions in other files still say the validators and the identity-session
  cookie are wired in `Program.ConfigureSettings` / `Program.ConfigureAuthentication`. Both methods
  were removed from web-server `program.cs`, and the code now lives in `WebServerSettings` /
  `WebServerAuthentication`. Affected:
  `configuration/sample-options-validator.cs:6`, `configuration/site-metadata-validator.cs:6`,
  `features/identity/web-authn-options-validator-application.cs:7`,
  `features/identity/web-authn-options-application.cs:9`,
  `features/identity/agent-token-options-validator-application.cs:7`,
  `features/identity/sign-in-options-validator-application.cs:6`,
  `platform/abuse/abuse-rate-limit-options-validator-application.cs:7`
  (all `Program.ConfigureSettings`), and
  `platform/identity-host/cookie-browser-session-service-server.cs:3` (`Program.ConfigureAuthentication`).
- Suggestion: point them at `WebServerSettings.ConfigureServices` and `WebServerAuthentication`
  (web-server-settings.cs / web-server-authentication.cs). Leave `Web.Spa`'s own `ConfigureSettings`
  references alone, because that method still exists.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/container-apps/web/projects/web-server/web-server-state-dev-tools.cs:6; source/container-apps/web/projects/web-server/web-server-authentication.cs:177
- Description: the moved text still uses the old member names. The Design region says
  "AddStateDevToolsExport adds…", but the method is now `WebServerStateDevTools.AddExport`. The
  agent-token comment says the lifetime lives in "AgentTokenOptions (ConfigureSettings)", a method
  that no longer exists in web-server.
- Suggestion: rename to `AddExport` and `WebServerSettings` respectively.
- Status: open

### Issue 3 — Severity: suggestion
- File: source/container-apps/aspire/projects/aspire-app-host/postgres-resources.cs:107, :129
- Description: two comments in `PostgresResources` say "(see the Design region)". One is about the
  Neon pooled reference attached after `AddEFMigrations`. The other is about the
  `postgres-db-password` / `postgres-db-uri` container-app secrets. That reasoning now sits in
  `publish-target.cs`'s Design region (the aca-neon paragraph about the AddEFMigrations snapshot,
  and the aca Flexible Server secrets bullet), not in `postgres-resources.cs`'s. The rationale for
  the most order-sensitive statement in this file (`postgres-resources.cs:235-243`) is therefore in
  a different file, and the pointer sends readers to the wrong one.
- Suggestion: move the aca-neon snapshot/ordering paragraph (and the Flexible Server secrets note)
  into the `postgres-resources.cs` Design region, or change both pointers to name
  `PublishTarget`'s Design region (publish-target.cs).
- Status: open

### Issue 4 — Severity: nit
- File: source/container-apps/aspire/projects/aspire-app-host/ingress-resource.cs:126, :316; source/container-apps/aspire/projects/aspire-app-host/aca-neon-compute.cs:55
- Description: if a generated app turns off api, grpc, and web together, `projects`
  (in `AddRunModeProject`, `ConfigureRoutes`, `ApplyTo`) and `yarpConfiguration` (in
  `ConfigureRoutes`) have no remaining use. `.editorconfig:343` sets RCS1163 (unused parameter) to
  warning, so that build would fail. The old code had a lambda in that position. The smoke matrix
  only turns off one flag at a time, so this combination is not gated and this finding is unverified.
- Suggestion: confirm whether a no-backend app is a supported combination. If it is, use a discard
  or guard the parameter. If it is not, mark this wontfix with that rationale.
- Status: open

### Issue 5 — Severity: nit
- File: source/container-apps/web/projects/web-server/web-server-http-clients.cs:30; web-server-payment-services.cs:11; brand-stylesheet-endpoint.cs:12
- Description: these files have no blank line between `namespace …;` and the first `/// <summary>`.
  Other files in the folder have one, for example web-mcp-discovery-endpoint.cs.
- Suggestion: add the blank line.
- Status: open
