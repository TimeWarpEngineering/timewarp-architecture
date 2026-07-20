# Harden FastEndpoint generator HttpVerb enum Command Query and tests

## Parent
109

## Description
Fix blocking generator defects before web-server cutover: HttpVerb enum metadata resolves to Get for all non-Get verbs; always emits Query not Command.

## Requirements
- Resolve HttpVerb enum member name (like endpoint-coverage-analyzer)
- Emit BaseFastEndpoint<Op.Command|Query, Response> correctly
- Generator unit tests for Post/Command/Delete/Put
- Drop weather-only ExampleRequest or guard it
- weather api still builds

## Checklist
- [ ] HttpVerb resolution
- [ ] Command vs Query emission
- [ ] Generator tests green
- [ ] api-server weather still works

## Notes
Prerequisite for 109-003. See parent 109 plan.
