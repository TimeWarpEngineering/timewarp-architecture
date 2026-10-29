# Round 1 — merged findings
**Date:** 2026-10-29
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 1 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: open
- File: tests/container-apps/web/web-server-integration-tests/open-api-delete-request-body-tests.cs:92
- Description: The transformer ships in `TimeWarp.Foundation.Server` to every generated app, and its Design region (source/foundation/foundation-server/open-api-delete-request-body.cs:11-13) promises two behaviors that no test exercises: a leftover object or collection property keeps the request body with `required: false`, and a nullable leftover scalar publishes a query parameter with `required: false`. Only the DELETE Roles test exercises the new code, and only for one shape (a required `Guid`). The GET Roles and Clear xAI key tests pass without the transformer. The repo rule is that when two things must agree (here the Design claim and the code) a check enforces it.
- Suggestion: Add a synthetic DELETE FastEndpoint to the test assembly (the test project already references FastEndpoints and filters endpoints by name) with a route id, an optional scalar, and a second variant with a complex property. Include them in `EndpointNames`. Assert that the optional scalar publishes `required: false` and that the complex variant keeps `requestBody` with `required: false`. Keep the existing three tests.
- Source: general
- Disposition notes:

### M2 — Severity: nit — Status: open
- File: source/foundation/foundation-server/open-api-delete-request-body.cs:309
- Description: The `SchemaFor` fallback (used when the body schema lookup finds no matching property) is wrong for some types that `IsScalar` accepts. `DateOnly` maps to `format: date-time` instead of `date`. `uint`, `ulong`, `ushort`, and `sbyte` are not in the integer list, so they publish as `type: string`. Enums publish as a bare `string` with no values. The test's `uuid` assertion is produced by either path, so it cannot show which path ran.
- Suggestion: Map `DateOnly` to `format: date` and `TimeOnly` to `format: time`. Include the unsigned and signed-byte integer types in the integer branch. For enums, emit `type: string` with the enum names, or state in the Design region that the fallback is approximate and the body schema is the primary source.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- Single reviewer; no duplicates. Reviewer issue numbers map 1:1 to M1 and M2.

## Verified, no finding

- Transformer ordering after the FastEndpoints operation transformer is proven by the passing test (a wrong order would leave `RequestBody` null and return early). The bearer-security and public-server transformers registered after it do not read `RequestBody` or `Parameters`.
- No other DELETE in this repo has leftover properties, so the transformer changes only DELETE Roles today (live spec checked).
- Goal restatement matches the original task: body confirmed not required, body dropped, path parameter kept. No misread.
- Conventions: Purpose and Design regions present, kebab file names, Jaribu with Shouldly, fixture mirrors `open-api-anonymous-intent-tests.cs`.
- Version: `dev check-version` reports the source version already ahead of NuGet; no bump required on this diff.
