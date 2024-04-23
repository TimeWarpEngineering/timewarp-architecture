# Choosing an Endpoint-Centric API Design Enhanced by Interface-Based Validation

* Status: proposed
* Deciders: Architect
* Consulted: Amina
* Date: 2024-04-23

## Context and Problem Statement

When designing a new API, a critical decision is the selection of a design pattern that will govern our approach to creating API Contracts, which include Commands, Queries, data transfer objects (DTOs) and validation. The choice between an endpoint-centric and entity-centric design impacts coupling, flexibility, maintenance, and the ease of integration with front-end systems. Which pattern should we adopt to optimize these aspects?

## Decision Drivers

* Requirement to maintain high flexibility and specificity for each endpoint.
* Minimize coupling between endpoints to make api more resilient.
* Avoid over-fetching or under-fetching of data.
* Need for efficient data validation and binding mechanisms.
* Desire to ensure ease of maintenance and understandability of the API.

## Considered Options

* **Entity-Centric API Design**: Utilizes shared DTOs across multiple endpoints.
* **Endpoint-Centric API Design without Interface Validation**: Uses separate DTOs for each endpoint without shared validation.
* **Endpoint-Centric API Design with Interface Validation**: Uses separate DTOs for each endpoint but with shared validation logic through interfaces.

## Decision Outcome

Chosen option: "Endpoint-Centric API Design with Interface Validation", because it aligns best with our goals of maintaining endpoint specificity and flexibility while also centralizing and simplifying validation logic. This approach facilitates easier data binding with UI components and reduces redundant code.

### Positive Consequences

* Precise control over the data structure and validation rules for each endpoint.
* Reduced duplication in validation logic through the use of shared interfaces.
* Improved data binding compatibility with UI components, simplifying front-end integrations.

### Negative Consequences

* Initial complexity in setting up and managing the interface structures.
* Requirement for careful documentation and understanding of interface inheritance and its implications on API behavior.

## Pros and Cons of the Options

### Entity-Centric API Design

* Good, because it simplifies the API model by reducing the number of DTOs.
* Bad, because it couples the DTO across multiple endpoints, potentially leading to less flexibility.
* Bad, because it may lead to less flexibility in tailoring data structures to specific endpoint needs.
* Bad, because it can result in either over-fetching or under-fetching of data.

### Endpoint-Centric API Design without Interface Validation

* Good, because it ensures maximum flexibility and control over each endpoint's data contracts.
* Bad, because it leads to potential redundancy in validation logic across similar DTOs.
* Bad, because it complicates consistency in form data binding across the API.

### Endpoint-Centric API Design with Interface Validation

* Good, because it maintains the flexibility of endpoint-specific DTOs while centralizing common validation logic.
* Good, because it streamlines integration with UI by standardizing data binding practices.
* Bad, because it adds complexity in managing and evolving interface definitions over time.

## Links

* [API Design Best Practices](https://example.com/api-design-best-practices)
