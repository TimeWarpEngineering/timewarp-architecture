# Round 1 — general
**Date:** 2026-10-20
**Scope reviewed:** branch `task/386-roles-slice-operation-info-analyzer-skill-and-docs` vs `master`

## Summary

The change adds TWA0035 (`OperationAttributeAnalyzer`, info) on the same hosted gate the generators use, annotates the six roles requests with `[Operation]`, and moves contract requests whose kind is clear from `IRequest<T>` to `IQuery<T>`, `ICommand<T>`, or `IIdempotentCommand<T>`. `SubmitTip.Query` and `InvokeMeteredCapability.Query` stay on bare `IRequest` with Design reasons. The skill and `references/neutral-operations.md` describe the neutral layer and the HTTP, CLI, and MCP bindings, and they state that `IIdempotentCommand` is not a verb rule.

Checked against the goal and the gate-lift checklist. The analyzer matches `HostedRouteDiscovery.TryGetHostedOperation`, the `TimeWarp.Contracts.OperationAttribute` full name, and the whitespace-name rule the contracts generator already uses. `IQuery<T>`, `ICommand<T>`, and `IIdempotentCommand<T>` in TimeWarp.Mediator.Contracts 14.0.0 all extend `IRequest<T>`, and the roles handlers stay `IRequestHandler`. Web-contracts Release build: 0 warnings, 0 errors. SARIF has 54 `TWA0035` results at level `note`, and none name a `Features.Admin.Roles` type. The roles assembly strings include `roles.list`, `roles.get`, `roles.create`, `roles.update`, `roles.delete`, `roles.set-permissions`, and `admin.roles`. Analyzer tests `Should_Suggest_Operation`: 7 passed, 0 failed.

No product defects found.

## Issues
