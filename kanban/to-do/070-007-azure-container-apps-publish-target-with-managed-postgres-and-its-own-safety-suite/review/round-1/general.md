# Round 1 — general
**Date:** 2026-10-07
**Scope reviewed:** master...HEAD (e1528cc83)

## Summary
The AppHost branching is correct. Run mode never reads `Publish:Target`, so it keeps the Postgres container, the volume and the REPL. In the aca branch, only YARP gets `WithExternalHttpEndpoints`, and the dashboard is off. The suite `AcaPublish_Given_` passes 7/7 locally (in-proc). I checked the generated Bicep in `artifacts/aspire-output/aca`, and it matches the claims about the dashboard, external ingress, Flexible Server and Key Vault.

The main risk is the open question about web routes. From the generated ingress Bicep, the web hop is HTTPS through ACA's host-routed internal ingress, carrying the public Host header, so the web routes will very likely not work. The other findings are smaller:
- the docs and Design region are inaccurate about where the Postgres password lives and about the firewall scope;
- one safety-suite regex can pass when it should fail;
- the dev-cli subscription override is subtle;
- the migration recipe does not say where the operator gets the database credentials.

## Issues

### Issue 1 — Severity: bug
- File: source/container-apps/aspire/projects/aspire-app-host/program.cs:592 (web routes `WithTransformUseOriginalHostHeader(true)`), and program.cs:516-528 (the inline comment on the web hop)
- Description: The generated `ingress/ingress.bicep` sets the `web-server-http` cluster address to `http://_http.web-server`. But in ACA, `services__web-server__http__0` resolves to `https://web-server.internal.${domain}`, because Aspire upgrades HTTP endpoints to HTTPS for ACA. So every web route (the SPA catch-all, the `/api/<web>` prefixes, `/api`) goes over TLS to the internal FQDN while carrying the client's public `Host` (`ingress.<domain>`).
  - ACA's Envoy ingress picks the target app by Host/SNI. .NET also uses the `Host` header as the TLS target name.
  - The request will therefore most likely route to the ingress app itself (a loop) or to no app (404), or fail certificate-name validation. The `*.internal.<domain>` cert does not cover the public name.
  - That would break the whole web surface on the first deploy, not just passkeys.
  - The code's own Design region names this as "the same mismatch the http web hop above avoids in Compose/Kubernetes". The inline comment at :516-528 ("TLS terminates at the ingress edge … not on this hop") is false for aca.
  - I could not prove this without a deploy, but the published Bicep makes the failure very likely, not a toss-up.
- Suggestion: Settle this before merge instead of leaving it to the maintainer's first deploy. One option is an aca-only route set without the original-host transform: YARP's default `X-Forwarded-Host` carries the public host, and web-server's RP-ID host accessor would honour it (`ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` is already emitted on web-server, though the env-var mode only processes For/Proto). If you keep the current choice, at least make the inline comment at :516 say the HTTP hop does not hold under ACA, and have the skill say web routes are expected to fail until this is decided, not just "check".
- Status: open

### Issue 2 — Severity: suggestion
- File: skills/tw-deploy/SKILL.md:205 (also SKILL.md:300-304, program.cs aca Design paragraph and the aca-branch comment, task.md Results)
- Description: The docs say the Postgres password "lives in a Key Vault". In the generated `web-server/web-server.bicep`, only the connection string is a Key Vault reference. The password is also in two plain container-app secrets built straight from the `@secure()` parameter: `postgres-db-password` (`POSTGRES_DB_PASSWORD`) and `postgres-db-uri` (`POSTGRES_DB_URI`). Aspire's `WithReference` emits both, and web-server does not use either. The password is not a literal, so the suite rightly passes. But the requirement ("stored in Key Vault by Aspire") and the skill's secrets table overstate the posture. The program.cs comment ("the generated postgres-username / postgres-password parameters … and the connection string land in a Key Vault") reads the same way.
- Suggestion: Make the docs accurate: Key Vault holds the connection string, and the password is also a container-app secret on web-server. Optionally add a suite assertion that pins this shape, so a later change that adds more secret copies is a deliberate choice.
- Status: open

### Issue 3 — Severity: suggestion
- File: skills/tw-deploy/SKILL.md:304 ("Server firewall: Azure services only"), and the matching Design-region wording in program.cs
- Description: The generated `postgres/postgres.bicep` adds `AllowAllAzureIps` (0.0.0.0–0.0.0.0) and leaves public network access on. That admits connections from any Azure-hosted IP in any tenant, not just this deployment's container apps. "Azure services only" sounds tighter than it is. The admin password is the only barrier, and the safety suite does not guard the firewall or network posture at all.
- Suggestion: Reword the skill and Design region to say what the rule actually allows: "any Azure IP, any tenant; password-protected; VNet integration is the hardening step". Also consider a suite fact pinning the firewall rule set, so a wider rule (for example an operator rule left at 0.0.0.0–255.255.255.255 in code) fails CI.
- Status: open

### Issue 4 — Severity: suggestion
- File: tests/container-apps/aspire/aspire-tests/aca-publish-tests.cs:363 (`ExternalIngress`), used at :258
- Description: `\bingress:\s*\{\s*external:\s*(true|false)` only matches when `external` is the first key of the `ingress` object. Aspire writes the keys in alphabetical order today (`external`, `targetPort`, `transport`). If any key that sorts earlier is emitted, the match fails and `External` silently becomes `false`. `allowInsecure` is the likely one, for example after a `WithHttpsUpgrade(false)` change, which the Design region discusses. For the ingress app the test would then fail loudly. For web-server, api-server or grpc-server it would pass even with `external: true`, which is exactly the regression the fact exists to catch.
- Suggestion: Slice the `ingress: { … }` block with `Block(...)` and read `Field(block, "external")`. If the field is missing, treat that as a failure for any app that has an ingress block, rather than defaulting to `false`.
- Status: open

### Issue 5 — Severity: suggestion
- File: tools/dev-cli/services/aspire-deploy-preflight.cs:65-72; tools/dev-cli/services/aspire-deploy.cs:116-119
- Description: The verb only looks at the process environment variable `Azure__SubscriptionId`. A subscription set in AppHost configuration or user secrets (`Azure:SubscriptionId`), or remembered by Aspire from an earlier interactive deploy, goes unseen. When the variable is unset, the verb injects `Azure__SubscriptionId=<az CLI subscription>` into the aspire process, and environment variables override user secrets in AppHost configuration. So an operator who pinned the subscription in user secrets, but whose `az account` currently points elsewhere, deploys into the wrong subscription. The same applies to `dev deprovision`, which targets the az CLI subscription rather than the recorded one. The printed plan also says the subscription came "from `az account show`", which hides the conflict. This meets the requirement as written ("when `Azure__SubscriptionId` is unset"), but the requirement's intent is a fallback, not an override.
- Suggestion: Either check the AppHost's configured value too (for example `dotnet user-secrets list` on the AppHost, or read `Azure:SubscriptionId` the same way the AppHost would) and pass the az value only when neither is set, or document in the skill and Design region that the az CLI subscription overrides user-secret and deployment-state values.
- Status: open

### Issue 6 — Severity: suggestion
- File: skills/tw-deploy/SKILL.md:320-333 (migration recipe)
- Description: The recipe uses `<postgres-username>` and `<postgres-password>` without saying where an operator finds them. Aspire generates both. The generated `main.bicep` even bakes a random username default into the output (`param postgres_username string = 'dxJgfVkVXr'`), which is different on every publish, so the copy from `dev publish aca` does not match the deployed one. The Key Vault `postgres-kv` uses RBAC authorization, and the Bicep grants the operator no role, so `az keyvault secret show` on `connectionstrings--postgres-db` fails by default.
- Suggestion: Add one line telling the operator where the deployed values are. One source is Aspire's deployment state or user secrets. The other is the Key Vault secret after the operator grants themselves `Key Vault Secrets User`. Optionally note that the published `main.bicep` username default is per-publish noise and not the deployed admin login.
- Status: open

### Issue 7 — Severity: nit
- File: source/container-apps/aspire/projects/aspire-app-host/program.cs:14; tools/dev-cli/endpoints/deploy-command.cs:14-15
- Description: The top Design line "Postgres is a container resource, not a project" is no longer true for the aca target, which uses a Flexible Server. The reconcile-on-edit rule applies. In deploy-command.cs, the new aca sentence was spliced into the middle of a line, so line 15 runs well past the region's wrap width and mixes the aca and Compose clauses.
- Suggestion: Qualify the line ("a container resource in run mode, Compose and Kubernetes; a Flexible Server for aca"), and re-wrap the deploy-command Design paragraph.
- Status: open
