# Round 1 — general
**Date:** 2026-10-11
**Scope reviewed:** branch vs master: yarp `trusted-proxy-forwarded-headers.cs`, `program.cs`, `appsettings*.json`; AppHost `program.cs` / `constants.cs` / csproj; `ServiceNames`; test infra and both ingress smokes; task 112 runbook; `skills/tw-deploy`.

## Summary

The fix lands at the right hop: the yarp project applies X-Forwarded-Proto/Host only from
configured trusted peers before `MapReverseProxy`, and run mode now hosts that project on a
non-proxied pinned http port so Caddy's `https` survives to the `Set` transform. Web.Server's trust
model is unchanged. Tests cover trusted and untrusted peers through the real YARP pipeline. Two
correctness gaps follow from the new design: run mode now takes its route table from a config file
the template never gates per feature flag, and the "empty list" semantics the docs promise do not
hold for JSON configuration. One doc claim about ACA is not supported.

## Issues

### Issue 1 — Severity: bug
- File: source/container-apps/yarp/appsettings.Development.json:10
- Description: Run mode used to build the ingress routes in the AppHost under `#if api` / `#if grpc` /
  `#if web`. It now loads them from the yarp project's Development config, which has no template
  conditionals. A generated app with `--api false` (a template-smoke matrix variant) or
  `--grpc false` gets an ingress that sends `/api/{**catch-all}` to `https://api-server` and
  `/grpc/**` to `https://grpc-server`. Neither service exists. Before this change those paths fell
  through to the web catch-all. Now they return 502.
- Suggestion: gate the Api/Grpc/Web routes and clusters with `//#if (api)` / `(grpc)` / `(web)`
  JSON conditionals. Verified in a scratch template hive that the template engine strips
  `//#if (api)` blocks in `.json` files. The config parser allows comments and trailing commas.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/container-apps/yarp/trusted-proxy-forwarded-headers.cs:37
- Description: Presence is detected with `IConfigurationSection.Exists()`. An empty JSON array is
  stored as a key with a null value, and `Exists()` returns false for it. Checked with
  Microsoft.Extensions.Configuration.Json: `{"T":{"P":[],"N":[]}}` gives P/N exists=False and
  T exists=True. So `"KnownProxies": [], "KnownNetworks": []` falls back to the absent-section
  default, which trusts loopback, instead of skipping the middleware. The appsettings comment, the
  Design region, the task 112 runbook and `skills/tw-deploy` all say an empty list means the
  headers are ignored.
- Suggestion: treat a list as configured when its key is declared under the section
  (`GetChildren()`), and read declared-but-empty lists as empty. Keep the error for a scalar value.
- Status: open

### Issue 3 — Severity: suggestion
- File: skills/tw-deploy/references/azure-container-apps.md:124
- Description: "ACA's external ingress is already https" (also in task Results) implies the
  published AddYarp ingress keeps the client scheme on ACA. ACA's managed ingress terminates TLS
  and forwards to the container target port over http, so the stock image's `Set` likely writes
  `http`, which is the same failure class as task 305. Nothing in the diff verifies the ACA claim.
- Suggestion: say plainly that task 305 does not change the published ingress. Name the
  workaround there (`SiteMetadata:BaseUrl`, `PublicOrigin`) instead of asserting the scheme is kept.
- Status: open
