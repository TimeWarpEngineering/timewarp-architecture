# Annotate web-contracts enable FE on web-server and delete MVC shims

## Parent
109

## Description
Atomic cutover: [ApiEndpoint] on hosted web operations, EndpointAuthorize on GetAgentIdentity, EnableApiEndpointGeneration + FE pipeline, delete 19 BaseEndpoint shims, drop MapControllers.

## Requirements
- Auth middleware before UseFastEndpoints
- Routes unchanged
- IncludeAbstractValidators false
- TrackEvent preserve route string

## Checklist
- [x] Contracts annotated
- [x] web-server FE wired
- [x] Shims deleted
- [x] Build green TWA0006 clean
## Notes
Depends on 109-001 and 109-002.


## Results

### Summary
Atomic cutover: web-contracts annotated with [ApiEndpoint]; GetAgentIdentity has EndpointAuthorize; web-server enables FastEndpoint generation; 19 MVC BaseEndpoint shims deleted; MapControllers/AddMvc removed. Identity integration suite 53 passed.

### Key decisions
- Policy string literal agent-scope:identity:read on GetAgentIdentity
- ApiEndpointContractAssemblies filter for web-contracts only
- EmptyRequestBinder for empty ceremony commands
- DisableAutoDiscovery to avoid duplicate routes

### Build / tests
- dev build 0/0
- web-server-integration-tests 53 passed, 1 skipped

## Session
- Done: 2026-07-20
