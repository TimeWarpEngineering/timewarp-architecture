# Round 1 — general
**Date:** 2026-11-26
**Scope reviewed:** branch `task/458-004-one-problem-details-shape-on-the-wire-and-in-open` vs `master`. Wire writer, OpenAPI problem component, bearer, payment, and Entra call sites, and the new OpenAPI and Hello tests. Implementer `## Results` and `### How to validate` read and not re-run.

## Summary

Validation failures still travel mediator `FluentValidationBehavior` to `SharedProblemDetails`, then out through `BaseFastEndpoint` (and the feedback and Entra writers). `SharedProblemDetailsHttp.WriteAsync` passes `application/problem+json; charset=utf-8` into `WriteAsJsonAsync`, which is the overload that was replacing that header with `application/json`. `OpenApiProblemDetails` adds `errors` (object of string arrays) and `additionalProperties` on the one component, documents 400 when `IValidator<TRequest>` is registered, points bearer and payment responses at that component (payment as `allOf` so `payment`, `reason` / `error`, `ok`, and `tip` stay named), and fails document generation when a `problem+json` response does not reference it. Entra 400 and 403 use `Produces<SharedProblemDetails>` instead of `ProducesProblem`, and unreferenced MVC problem components are removed. The restatement matches the task: the owner hint named the FastEndpoints error builder, and the wire path that produced the reported `application/json` body is the mediator result written by `BaseFastEndpoint`. Recorded gates cover the change (build, the new problem-details file, Hello on the real web host, the OpenAPI suites, the edited writers' tests, and audit). No product defect found.

## Issues

<!-- None. -->
