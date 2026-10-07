# Round 2 — general
**Date:** 2026-10-07
**Scope reviewed:** e1528cc83..5e759c6b7 (+ re-verification of round-1 M1–M7)

## Summary

All round-1 findings are addressed. M2–M7 are fixed. M1 was fixed in the docs only, as intended,
and the docs are accurate, so it is wontfix until the maintainer decides. I re-ran the gates:
`AcaPublish_Given_` passed 9/9 in-proc and `Preflight_Given_` (dev-cli-tests) passed 14/14.

I also mutation-tested the suite by pointing `TIMEWARP_ACA_OUTPUT` at edited copies of
`artifacts/aspire-output/aca`. Each of these fails exactly one fact:
- web-server with `external: true` placed after `targetPort`
- web-server's ingress with `external` removed
- an extra `pg-copy` secret built from `postgres_password_value`
- the AllowAllAzureIps rule widened to `255.255.255.255`
- a second top-level firewall rule

One mutation passes: a firewall rule written as a Bicep `[for …]` loop (N1, a nit).

`dotnet user-secrets list` behaves as the parser expects. With no `UserSecretsId` it exits 1 and
prints the error to stderr, so `ParseUserSecret` returns null and the az fallback applies. An
empty store prints "No secrets configured…" with exit 0, which also gives null. A value that
contains ` = ` (`X = a = b`) splits on the first separator, which is correct. The AppHost does
declare a `UserSecretsId`. No new bugs were found.

## Prior findings

- M1 — Status: wontfix (pending maintainer decision on the aca web-route host strategy) — These
  places now say web routes are expected to fail through the ACA ingress until that decision is
  made:
  - the inline comment at program.cs:534-539 now limits "TLS terminates at the ingress edge" to
    run mode, Compose and Kubernetes, and says the aca hop goes to `https://web-server.internal.*`
    with the public Host
  - the Open Questions entry (program.cs:216-229) says EVERY web route is expected to fail and
    that the aca web surface is not usable until the decision, and it lists both candidates
  - tw-deploy's "expect" bullet (SKILL.md:387-395) says "Web routes are expected to fail… do not
    rely on the aca target for the web surface" and notes that api and grpc are not affected
  - task.md Results and "How to validate" say the same, and the maintainer deploy check is gated
    on the decision

  The Ingress topology section's ACA bullet (SKILL.md:249-250) only describes external/internal
  ingress and claims nothing about web routing, so it does not contradict this.
- M2 — Status: fixed — The secrets table (SKILL.md:208), the provisioned-resources list
  (SKILL.md:304-310), the AppHost Design region and task.md now name `postgres-db-password` and
  `postgres-db-uri` as plain container-app secrets on web-server. The new fact
  `Publish_Should_PinThePostgresSecrets` pins that set: exactly three postgres secrets on
  web-server, the KV one carries `keyVaultUrl:`, the other two are built from the @secure()
  `postgres_password_value`, and no other app has any. This matches the published web-server.bicep.
  Because the filter matches on the secret's value as well as its name, a renamed copy is also
  caught (mutation verified).
- M3 — Status: fixed — "Azure services only" is reworded everywhere to "AllowAllAzureIps… any
  Azure-hosted IP in any tenant; the password is the barrier; VNet is the hardening step". The new
  fact `Publish_Should_PinTheFlexibleServerFirewall` asserts a single rule across all modules, in
  postgres.bicep, named AllowAllAzureIps, from '0.0.0.0' to '0.0.0.0'. It catches a widened rule
  and an extra top-level rule (mutations verified). One gap remains: a loop-form rule (N1).
- M4 — Status: fixed — The ingress block is now sliced with `Block`, and `external` is read with
  `Field`. A missing field, or a value that is not a literal, fails. An app without an ingress
  block is only allowed when it is not the YARP ingress. Both mutations from round 1 (key order,
  missing field) now fail.
- M5 — Status: fixed — The new precedence is: env var, then AppHost user secret
  `Azure:SubscriptionId` (read with `dotnet user-secrets list --project`), then
  `az account show`. Only the az fallback sets `PassToAspire`, and the probe is skipped when the
  env var is set. Tests cover all three branches, the argument builder, case-insensitive parsing,
  a value containing ` =`, a blank value, a key that only shares a prefix, and a failed probe.
  The one remaining gap, a subscription that Aspire remembered in its deployment state, is
  documented in the skill and the Design region.
- M6 — Status: fixed — SKILL.md:342-362 says where the username and password are:
  - Aspire deployment state `~/.aspire/deployments/<hash>/production.json`. I checked locally that
    the keys `Parameters:postgres-username` and `Parameters:postgres-password` exist.
  - AppHost user secrets
  - the Key Vault secret, with the `Key Vault Secrets User` role grant first. The
    `aspire-resource-name` tag the `az keyvault list` query uses is present in postgres-kv.bicep.

  It also warns that the `postgres_username` default in `main.bicep` is not a record of the
  deployed login.
- M7 — Status: fixed — program.cs:14-15 now qualifies the Postgres resource kind per target, and
  the Design paragraph in deploy-command.cs is re-wrapped and reordered (compose runtime, then
  aca subscription).

## Issues

### N1 — Severity: nit
- File: tests/container-apps/aspire/aspire-tests/aca-publish-tests.cs:282 (regex at `FirewallRuleDeclaration`, Design claim at line 34)
- Description: `FirewallRuleDeclaration` only matches a top-level `resource … 'firewallRules@…' = {`.
  Three shapes slip past it:
  - a loop-form rule (`= [for … : { … }]`). Verified: I added a 0.0.0.0–255.255.255.255 loop rule
    and the suite still passed 9/9.
  - an indented nested child resource
  - a `.../flexibleServers` resource with an inline `firewallRules` child

  The Design region says "Any other or wider rule in the published Bicep fails", which is
  stronger than what the suite checks. Azure.Provisioning emits flat top-level resources, so this
  is unlikely in practice; that is why it is only a nit.
- Suggestion: Either count every occurrence of `flexibleServers/firewallRules@` (and any
  `startIpAddress:`) in all modules and require exactly one, or soften the Design sentence to "any
  other top-level rule".
- Status: open
