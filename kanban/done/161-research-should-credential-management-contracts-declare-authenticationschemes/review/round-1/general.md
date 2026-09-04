# Round 1 — general
**Date:** 2026-09-04
**Scope reviewed:** Diff vs `origin/feature/overnight` (product commit `b88603c0`) — probe suite, GetAgentBearerIdentity fold-in, Design/skill/ADR litmus, surrounding `[EndpointAuthorize]` / permission-policy / generator call sites

## Summary

Research task 161 correctly pins when FastEndpoints `Policies(...)`-only authenticates non-default schemes: ASP.NET Core combines named-policy `AddAuthenticationSchemes` with `IAuthorizeData.AuthenticationSchemes`, and an empty combined list is a PolicyEvaluator no-op (default scheme only). The isolated `ProbeScheme_Given_` TestServer suite (4/4) and `get-agent-bearer-identity-tests.cs` (4/4) both passed on re-run. Fold-in matches the hybrid litmus (skill/ADR/Design + last Policies-only hosted contract now lists `agent-token`); overall risk is low. One coverage-table nit on suite bucketing for InvokeMetered anonymous.

## Issues

### Issue 1 — Severity: nit
- File: kanban/in-progress/161-research-should-credential-management-contracts-declare-authenticationschemes/task.md:109-119
- Description: The coverage audit section is titled “In-proc HostGraph (`web-server-integration-tests`)” and marks InvokeMeteredCapability anonymous as `401`. That anonymous case lives in the co-located Jaribu runfile `source/container-apps/web/features/metered-capability/invoke-metered-capability/invoke-metered-capability-tests.cs` (`Unauthorized_Given_No_Bearer`), not under `tests/.../web-server-integration-tests`. Bearer coverage for that route in the suite project is via `program-104-sunny-paths-tests.cs`; cookie-isolation gap remains correctly noted.
- Suggestion: Broaden the section label to “in-proc HostGraph” (or footnote the co-located runfile) so the anonymous cell is not attributed to `web-server-integration-tests` alone.
- Status: open
