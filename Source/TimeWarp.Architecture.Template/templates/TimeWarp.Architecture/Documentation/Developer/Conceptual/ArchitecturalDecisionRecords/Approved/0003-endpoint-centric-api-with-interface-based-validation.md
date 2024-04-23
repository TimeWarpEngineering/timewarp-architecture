# Endpoint-Centric API Design Using Interfaces for Validation

* Status: proposed
* Deciders: Amina, Development Team, API Architect
* Date: 2024-04-23

Technical Story: Rethink API structure to enhance maintainability and data binding without compromising endpoint specificity.

## Context and Problem Statement

We have struggled with data binding complexities and redundant validation logic in our current API designs. How can we reduce these issues while preserving or enhancing the flexibility and specificity of our APIs?

## Decision Drivers

* Need to minimize redundant validation logic across multiple endpoints.
* Requirement for improved data binding compatibility with UI components.
* Desire to maintain or enhance flexibility and specificity for each API endpoint.

## Considered Options

* **Endpoint-Centric API without Interface Validation**: Continue the current approach but without any improvement in validation or data binding.
* **Entity-Centric API**: Consolidate DTOs across endpoints, improving validation and data binding at the risk of losing endpoint specificity.
* **Endpoint-Centric API with Interface Validation**: Implement an interface-based validation approach for endpoint-specific DTOs.

## Decision Outcome

Chosen option: "Endpoint-Centric API with Interface Validation", because it provides the necessary improvements in data binding and validation without compromising the API's flexibility and specificity. The introduction of interfaces removes the need for duplicate validation logic, streamlining development while maintaining strict control over each endpoint's data contract.

### Positive Consequences

* Elimination of duplicate validation logic, centralizing it in common interfaces.
* Simplification of UI form bindings, improving consistency across different endpoints.
* Retention of endpoint-specific data contracts, enabling precise control and maintenance.

### Negative Consequences

* Slight increase in initial setup complexity due to the need for defining and managing interfaces.
* Potential need for ongoing management to ensure interfaces accurately reflect evolving business logic and API requirements.

## Pros and Cons of the Options

### Endpoint-Centric API without Interface Validation

* Good, because it provides strict separation of concerns and full control over each endpoint.
* Bad, because it maintains redundancy in validation logic.
* Bad, because it complicates UI data binding in complex forms.

### Entity-Centric API

* Good, because it reduces duplication and simplifies the API surface.
* Bad, because it can lead to over-fetching or under-fetching of data and reduced flexibility.
* Bad, because it often results in a generic approach that might not suit all endpoint-specific requirements.

### Endpoint-Centric API with Interface Validation

* Good, because it maintains the flexibility and specificity of endpoint-centric designs while reducing validation redundancy.
* Good, because it significantly improves UI form data binding through standardized interfaces.
* Bad, because it introduces some complexity in defining and managing interfaces.

## Links

* Comparison of API design patterns discussed in [API Design Patterns Book](https://example.com/api-design-patterns).
