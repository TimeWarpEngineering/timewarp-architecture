# Prove identity suites string-enums and update AGENTS skill ADR

## Parent
109

## Description
Prove identity/passkey/agent integration suites + string-enum wire through FastEndpoints; update AGENTS.md, web-api-contracts skill, generator docs; short ADR.

## Checklist
- [x] Identity integration green
- [x] String-enum assert green on FE path
- [x] AGENTS.md endpoint story uniform
- [x] Skill + generator doc + ADR
- [x] dev build 0/0; dev test

## Notes
Depends on 109-003.

## Results

### Summary
Docs/ADR closeout for FastEndpoints on both servers. Identity suites (incl. Agent_Protected_Endpoint_Tests string-enum asserts) green on FE path. No large code changes.

### Doc updates
- AGENTS.md: both servers generate FastEndpoints; validation = mediator FluentValidationBehavior
- skills/web-api-contracts/SKILL.md: [ApiEndpoint] + [EndpointAuthorize]; no hand BaseEndpoint shims
- documentation/developer/reference/ApiEndpointSourceGenerator.md: ApiRoute (not RouteMixin), auth, opt-in MSBuild, both hosts
- ADR-0007 (accepted): HTTP endpoints are generated FastEndpoints from contracts on both servers

### Confirmed
- Agent_Protected_Endpoint_Tests.Ok_Given_Valid_IdentityRead_Token asserts PascalCase string enums (`"kind":"Agent"`, `"trustTier":"Keyed"`, rejects integer wire)

### Build / tests
- dev build: 0 Warning(s), 0 Error(s)
- web-server-integration-tests: 53 passed, 1 skipped

### Post-completion review finding (2026-07-20 — tracked in task 110, NOT a regression)
Follow-up review found the migration left a **fail-open auth-intent gap** that the 109 review's
"clean" disposition missed: the generator honors only `[EndpointAuthorize]` and reads
`IAuthApiRequest` zero times, so the seven `IAuthApiRequest` contracts (admin roles CRUD,
get-sign-in-token, get-current-user) generate `AllowAnonymous()` — public endpoints whose contracts
declare auth. Not a live regression (old MVC shims had no `[Authorize]`; web-server had no
`UseAuthentication` before 104-003 — server-anonymous before and after), but it contradicts 109's
own acceptance criterion ("contract is the single source of auth intent") and bakes generated-
anonymous admin endpoints into the template. Reconciliation + fail-closed analyzer guard is **task
110**. Recorded here so 109's disposition is not read as vetting the auth posture of these seven
contracts.

## Session
- Done: 2026-07-20
