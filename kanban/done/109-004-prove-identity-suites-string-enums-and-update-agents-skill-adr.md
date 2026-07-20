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

## Session
- Done: 2026-07-20
