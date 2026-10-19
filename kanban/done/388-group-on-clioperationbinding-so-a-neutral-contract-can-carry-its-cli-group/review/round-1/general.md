# Round 1 — general
**Date:** 2026-10-20
**Scope reviewed:** branch `task/388-group-on-clioperationbinding-so-a-neutral-contract` vs `master` (product commit `3f31c686e`)

## Summary

`OperationAttribute.Group` stores a purpose group and treats null, empty, and whitespace as ungrouped with `IsNullOrWhiteSpace`. `OperationDescriptor.Group` carries that value. The contracts generator emits `OperationGroup` only when the group is non-blank, and `EndpointEmitModel` reports the same value. Neither reader inserts the group into `RouteTemplate`, `GetRoute`, or the FastEndpoint route. `CliOperationBinding` stays the route pattern. The goal restatement matches the task body, including Steve's decision that the group lives on `[Operation]`. Jaribu covers a set group, an omitted group, a blank group, dotted and spaced segments, and catalog JSON that keeps `name` and `pattern` unprefixed. Re-ran `Group_Given_` (7 passed), `EndpointOperationMetadata_Tests` (8 passed), and `ContractsGenerator_Tests` (26 passed). Follow-ups timewarp-nuru 508-014 and Roslynk 003-001 are on those repos' to-do columns and match the acceptance text. No issues.

## Issues
