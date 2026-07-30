# Web API Contracts Documentation

This directory contains documentation for HTTP-based API contracts used in the TimeWarp.Architecture template.

## Scope

This documentation covers contracts for **JSON-over-HTTP APIs** in these projects:

- **Api.Contracts** - Contracts for the dedicated API service
- **Web.Contracts** - Contracts for the Web service APIs

Both projects use HTTP transport with JSON serialization and follow the same contract patterns.

## What This Does NOT Cover

- **Grpc.Contracts** - Uses protobuf serialization with different patterns (see separate gRPC documentation)

## Documentation Contents

- **[How to Write BFF API Contracts](how-to-write-bff-api-contracts.md)** - Primary guide for creating API contracts
- **[Handling Mutability in API Contracts](handling-mutability-in-api-contracts.md)** - Managing mutable data patterns
- **[Handling Nullability in API Contracts](handling-nullability-in-api-contracts.md)** - Null handling strategies

## Related Documentation

- [API Design Conceptual Guide](../../conceptual/api-design.md)
- [Endpoint-Centric API ADR](../../conceptual/architectural-decision-records/approved/0003-endpoint-centric-api-with-interface-based-validation.md)