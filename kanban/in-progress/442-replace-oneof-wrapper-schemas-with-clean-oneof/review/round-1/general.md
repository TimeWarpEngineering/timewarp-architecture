# Round 1 — general
**Date:** 2026-10-29
**Scope reviewed:** branch task/442-… vs 841e9b637 (product files only)

## Summary

The change inlines OneOf types (null schema reference id), rewrites OneOf schemas to `oneOf` over their `asT{n}` members, and splits a `OneOf<…, SharedProblemDetails>` response into a bare success schema at 2xx plus SharedProblemDetails under `application/problem+json` (4xx/5xx rewrite and a new `default`). For the common contract shape (no XML summary on the request) the code is correct and matches BaseFastEndpoint's wire shape. One latent gap: the problem arm is only discovered from a 2xx `oneOf`, so operations whose 200 is declared explicitly by the generator's summary chain are silently skipped. Remaining items are test-coverage and loudness suggestions.

## Issues

### Issue 1 — Severity: bug
- File: source/foundation/foundation-server/open-api-one-of.cs:141-157
- Description: The problem arm (`problem`) is taken only from a 2xx media type whose schema is a `oneOf` with `arms.Length` members (line 141, `problem ??= union[problemIndex]`). When it is not found, the transformer returns at line 152 (`if (problem is null) return`), so neither the 4xx/5xx rewrite nor the `default` response happens. That is the case for any generated endpoint that has an XML `<summary>` on its request class: the generator emits `Description(d => d….Produces<{Response}>(200, "Success").ProducesProblem(400, "Bad Request"))` (fast-endpoint-source-generator.cs:436-439; summary is read from the request class XML doc at models/endpoint-metadata.cs:241-246). FastEndpoints' default `Produces(200, ResDtoType)` is added through `ProducesDeDuped`, which skips when any 2xx metadata already exists (decompiled FastEndpoints 8.3.0 `EndpointProducesMetadata.AddSwaggerDefaults` / `ProducesDeDuped`). So for such an operation the 200 schema is the bare `Response` (no `oneOf`), the guard at 141 `continue`s, and the document keeps ASP.NET's `ProblemDetails` at 400 with no `default` and no SharedProblemDetails. This contradicts the acceptance wording "problem as SharedProblemDetails under application/problem+json" for generated contracts. No contract in this repo currently has a request-class summary (grep of `</summary>` within 3 lines before `[ApiRoute]` finds none), so it is latent here but live in generated apps, and the 4xx-rewrite branch in the Design region ("e.g. the generator's ProducesProblem(400)") is effectively unreachable for exactly that case. The failure is silent.
- Suggestion: Do not derive the problem arm from the 2xx response. Resolve it once for any endpoint whose `ResDtoType` arms contain SharedProblemDetails, e.g. `await context.GetOrCreateSchemaAsync(typeof(SharedProblemDetails), null, cancellationToken)` (available on `OpenApiOperationTransformerContext` in Microsoft.AspNetCore.OpenApi 10.0.11), make the method async, and keep the 2xx split as a separate step that runs only when a union is present. Add a probe endpoint with explicit `Description(d => d.Produces<X>(200).ProducesProblem(400))` and a test that its 400 and `default` are SharedProblemDetails.
- Status: open

### Issue 2 — Severity: suggestion
- File: tests/container-apps/web/web-server-integration-tests/open-api-one-of-tests.cs:124
- Description: The tests do not pin several behaviors the transformer implements. (a) `Every_Default_Response_Should_Be_Shared_Problem_Details` only asserts `defaults > 0`, so it passes if a single probe operation has a `default`; it never asserts that every contract operation (every endpoint with a OneOf ResDtoType) has one, so Issue 1 style regressions are invisible. (b) The 4xx/5xx rewrite loop (open-api-one-of.cs:154-163) has no test: neither probe endpoint declares a 4xx/5xx problem+json response. (c) A OneOf without SharedProblemDetails (should remain `oneOf` at 200, no `default`), an existing `default` (left untouched, line 166) and the "2XX" range key have no coverage. (d) `Contract_Operation_Should_Publish_Response_And_Problem_Json` checks the 200 `$ref` and `default` but not that no `application/problem+json` or `oneOf` remains under 200.
- Suggestion: Assert that every operation of a OneOf-typed endpoint has `default` = SharedProblemDetails (iterate `paths`, require `default` on all web-server contract operations or compare against a known list), and add a probe with an explicit `ProducesProblem(422)` plus a probe `OneOf<A,B>` without a problem arm.
- Status: open

### Issue 3 — Severity: suggestion
- File: source/foundation/foundation-server/open-api-one-of.cs:98-101
- Description: Both guards bail silently and leave the wrapper in place: a missing `asT{n}` member returns without rewriting (lines 98-101), and `union.Count != arms.Length` skips the split (line 141). Because `CreateSchemaReferenceId` already returns null for every OneOf, the unrewritten wrapper is then inlined at each use, with `value/index/isT0/asT0` members and no diagnostic. The lookup is `OrdinalIgnoreCase` on `asT{index}`, which covers camelCase and PascalCase, but a host with a snake_case or other naming policy (`as_t0`) would silently regress to inlined wrappers; the test's `CollectWrapperShapes` also uses the same exact member names (`isT0`, `asT0`, `index`, case-sensitive `TryGetProperty`), so it would not catch that either. Silent no-op on unexpected input conflicts with this repo's "when two things must agree, add a build check" posture.
- Suggestion: Throw (or log at warning) with the offending OneOf type name when an arm member is missing or counts disagree, so a changed serializer naming policy or OneOf version fails the document build/test instead of publishing a wrapper.
- Status: open

### Issue 4 — Severity: nit
- File: source/foundation/foundation-server/open-api-one-of.cs:106-109
- Description: `Arms()` unwraps `Nullable<T>`, so a nullable OneOf property reaches the schema transformer, but the rewrite then sets `schema.Type = null`. If the exported schema for `OneOf<…>?` carries `type: ["object","null"]`, the `null` is lost and the published `oneOf` no longer permits null. `Description`/`Title`/`AdditionalProperties` set on the wrapper schema are also left in place. Not verified by running; no current contract has a OneOf property (the Design region notes the property rule is future-proofing).
- Suggestion: Preserve the null flag, e.g. keep `schema.Type = schema.Type & JsonSchemaType.Null` when set, or add an `OpenApiSchema { Type = JsonSchemaType.Null }` arm; or drop the nullable unwrap if nullable OneOf is out of scope.
- Status: open

### Issue 5 — Severity: nit
- File: source/foundation/foundation-server/common-server-module.cs:30
- Description: The inserted Design-region sentence leaves line 30 at 114 characters (neighbouring lines are 85-88), and the following "OpenApiPublicServer then replaces servers" now reads as if it follows OneOf rather than the bearer step. Separately, the open-api-one-of.cs Design says the problem arm is published "on default", but the code only adds `default` when absent (line 166) and never rewrites an existing one.
- Suggestion: Re-wrap the comment at the file's usual width, and add "when absent" to the `default` clause in the open-api-one-of.cs Design region.
- Status: open

## Verified non-issues

- `Arms()` (open-api-one-of.cs:47-66): `typeof(IOneOf).Namespace` is `"OneOf"`; the base-type walk handles `OneOf<…>` structs and `OneOfBase<…>` subclasses in other namespaces; non-union types and custom `IOneOf` implementers with no `OneOf`-namespace generic base return null; `Nullable<T>` is unwrapped. No mis-detected shape found.
- Shared `IOpenApiSchema` reuse: `problem` is the same instance placed under several responses and the `default`; the code never mutates it, and the 2xx rewrite assigns `mediaType.Schema` rather than mutating the (possibly shared) union instance, so serialization is safe. Collections are not modified during enumeration (only `mediaType.Schema` assignments in the loops; `default` is added after).
- Wire shape: rewriting 4xx/5xx `application/problem+json` to SharedProblemDetails matches BaseFastEndpoint (base-fast-endpoint.cs:42-47, status `problem.Status ?? 400`); `default` plus an explicit 4xx is intentional and only overlaps when a 4xx problem+json response exists.
- `IsStatusClass` accepts `200`, `4xx` codes and `2XX`/`4XX`/`5XX` range keys (length 3, first char); it correctly rejects `default`.
- Ordering: OpenApiDeleteRequestBody reads the request body only; bearer and payment transformers add 401/403/402/503 and only append to the 200 description (`NoteSuccessDebit`), none read the 2xx schema or the OneOf component. Bearer's ad-hoc 401/403 schema is correct because those come from the bearer handler, not BaseFastEndpoint. `ConfigureOpenApi` runs after FastEndpoints' own registration, so wrapping `CreateSchemaReferenceId` keeps FE names for other types.
- api-server: shares `CommonServerModule.AddOpenApi` (api-server program.cs:114); `OpenApiOneOf` references only `SharedProblemDetails` and FastEndpoints types, nothing web-specific.
- Test isolation: other OpenApi test hosts use `Program` assembly (tags, anonymous-intent with name filter) or namespace/name filters (bearer, payment, delete) so the `OpenApiOneOf_` probes do not leak into them; the OneOf host's `Filter` admits Program-assembly endpoints plus only the probe namespace. Reusing `Document_Given_` under distinct namespaces already exists in the sibling files (tags, anonymous-intent, payment, bearer).
- Conventions: Purpose and Design regions present in both new files (TWA0004), kebab-case file names, Jaribu/Shouldly, no estimates, `_ = cancellationToken;` matches siblings. No stub/no-op/suppression that only makes a count pass; the two assertion families (no `OneOf*` component, no wrapper-member shape anywhere in the document) test the real output.
- Design-region claims about the schema pipeline (inlining, arms staying `$ref`, 2xx splitting) agree with the code and with the passing tests.
