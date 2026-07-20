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
- [x] EndpointAuthorize attribute
- [x] Generator emission
- [x] Tests

## Notes
Prerequisite for GetAgentIdentity after shim deletion.

## Results

See parent 109 plan. Generator hardened / EndpointAuthorize shipped as part of tw-orchestrate-task 109 (2026-07-20). Sourcegenerator tests 38 passed.

## Session
- Done: 2026-07-20

