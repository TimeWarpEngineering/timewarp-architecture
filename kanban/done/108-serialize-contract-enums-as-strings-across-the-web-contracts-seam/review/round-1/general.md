# Round 1 — general
**Date:** 2026-07-20

## Summary

Task 108 wires PascalCase string enums through the single contract-seam authority and closes the server/client split.

**ContractSerializationDefaults** adds `JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false)` inside `Apply`, so `Options` and every DI `Configure` consumer share the same shape. Design records the strings decision, plain-enum (not Bogard Enumeration) rationale, fail-closed integers/unknowns, and the rejected numeric+TWA renumber-guard alternative.

**CommonServerModule.ConfigureServices** Applies the same defaults to both MVC `JsonOptions` and `HttpJsonOptions` — the critical host fix so the server does not keep emitting STJ integer enums while SPA/CLI/tests use strings.

**web-contracts-tests** pin emission (`"kind":"Agent"`, `"trustTier":"Keyed"`), reject integers and unknown names with `JsonException`, accept case-insensitive string reads (documented STJ default), and round-trip `CredentialType` as `"Passkey"` / `"AgentKey"`. Contract-facing enums remain plain C# (`PrincipalKind`, `TrustTier`, `CredentialType`).

**Agent_Protected_Endpoint_Tests** asserts raw response JSON (not only typed deserialize) so integration proves the live server wire.

**CLI** `CliJson` mirrors the converter settings without referencing web-contracts; Design on both `cli-json.cs` and `agent-wire-dtos.cs` documents the match; `whoami-wire-tests` fixture is string-shaped and rejects numeric `kind`.

Checklist items for the product change are satisfied. No open defects found.

## Issues

_None._
