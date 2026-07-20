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
- [ ] Contracts annotated
- [ ] web-server FE wired
- [ ] Shims deleted
- [ ] Build green TWA0006 clean

## Notes
Depends on 109-001 and 109-002.
