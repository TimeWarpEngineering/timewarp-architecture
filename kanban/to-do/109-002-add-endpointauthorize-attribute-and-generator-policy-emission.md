# Add EndpointAuthorize attribute and generator policy emission

## Parent
109

## Description
New EndpointAuthorizeAttribute for policy/schemes/roles; generator emits Policies(...) or AllowAnonymous.

## Requirements
- Attribute in TimeWarp.Architecture.Attributes
- Generator reads EndpointAuthorize; emit Policies not bare RequireAuthorization
- Unit test for policy emission
- Default anonymous when attribute absent

## Checklist
- [ ] EndpointAuthorize attribute
- [ ] Generator emission
- [ ] Tests

## Notes
Prerequisite for GetAgentIdentity after shim deletion.
