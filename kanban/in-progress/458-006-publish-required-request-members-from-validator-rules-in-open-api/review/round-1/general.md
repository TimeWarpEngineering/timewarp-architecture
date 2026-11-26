# Round 1 — general
**Date:** 2026-11-26
**Scope reviewed:** branch `task/458-006-publish-required-request-members-from-validator` vs `master`, product files in the framework, checked against `## Goal (implementer restatement)` and the original task requirements.

## Summary

`OpenApiValidatorRequired` reads the same `IValidator<TRequest>` instances `FluentValidationBehavior` runs and publishes a member as required when an unconditional default-ruleset `NotEmpty` or `NotNull` rejects the value an omitted member keeps. Body schemas get a `required` entry from the schema transformer. Query and header parameters are marked required by the operation transformer, which is registered after `OpenApiDeleteRequestBody`. The document check, registered last, throws when a recorded name is a body property missing from `required` or a parameter whose `Required` is false. `[Required]` is gone from `RequestAgentHumanLink.Command.HumanPrincipalId`. The skill states that the validator is the source of `required`.

The restatement matches the task. "Non-nullable members where that is the contract" is the omission rule (a `NotNull` on `= []`, or on a non-nullable value type, does not reject omission), and the same requirements make the validator the source of `required`. No misread.

The recorded `## Results` cover the change: `./bin/dev build`, the new required-members tests (6/6), the agent-link OpenAPI tests (2/2), the six existing OpenAPI suites that share `AddOpenApi`, `./bin/dev lint-skills`, `./bin/dev check-version`, and `ganda repo audit`. Those gates were not re-run.

One targeted inspection, not a gate: `ilspycmd` on `Microsoft.OpenApi.OpenApiParameter` and `FluentValidation.Internal.IncludeRule<T>`, to check whether `parameter?.Required != false` treats an unset parameter as required. `Required` is a non-nullable `bool` (default false), so an existing parameter that was not marked required fails the check. `Include` builds an `IncludeRule<T>` whose lambda is null and whose component is a `ChildValidatorAdaptor`, which `Collect` treats as a self rule. That matches `SubmitFeedback`'s `Include`.

No open findings.
