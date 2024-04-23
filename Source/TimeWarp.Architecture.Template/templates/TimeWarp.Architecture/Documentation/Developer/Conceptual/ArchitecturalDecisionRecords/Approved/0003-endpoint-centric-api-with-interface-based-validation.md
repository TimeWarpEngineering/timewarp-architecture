# Choosing an Endpoint-Centric API Design Enhanced by Interface-Based Validation

* Status: proposed
* Deciders: Architect
* Consulted: Amina
* Date: 2024-04-23

## Context and Problem Statement

When designing a new API, a critical decision involves selecting a design pattern that will shape our approach to creating API Contracts (Commands, Queries, DTOs, and validation). The choice between endpoint-centric and entity-centric designs significantly affects aspects such as coupling, flexibility, maintenance, and the ease of integration with front-end systems. We aim to select a pattern that optimizes these aspects while aligning with our strategic goals.

## Decision Drivers

* Requirement to maintain high flexibility and specificity for each endpoint.
* Minimize coupling between endpoints to enhance API resilience and scalability.
* Prevent over-fetching or under-fetching of data to optimize performance and resource usage.
* Requirement for consistent, functional data validation mechanisms across server-side contracts.
* Need for data binding strategies that ensure functional binding of Blazor forms to the Commands and Queries of the API Contracts.
* Desire to ensure ease of maintenance and comprehensibility of the API.

## Considered Options

* **Entity-Centric API Design**: Utilizes shared DTOs across multiple endpoints.
* **Endpoint-Centric API Design without Interface Validation**: Uses separate DTOs for each endpoint without shared validation.
* **Endpoint-Centric API Design with Interface Validation**: Uses separate DTOs for each endpoint but with shared structure and validation logic through interfaces.

## Decision Outcome

Chosen option: 'Endpoint-Centric API Design with Interface Validation.' This approach best aligns with our goals by maintaining endpoint specificity and flexibility while centralizing and simplifying validation logic. It also supports decoupling of endpoints, allowing for modifications to one endpoint without impacting others, thereby enhancing system robustness. Furthermore, this design facilitates more consistent data binding with UI components, reducing redundant code and improving maintainability.

### Positive Consequences

* **Enhanced Endpoint Flexibility**: Each endpoint can be independently developed and modified without risk of affecting others, thanks to the decoupling provided by interface-based validation. This flexibility allows the API to evolve more dynamically and responsively to changing business needs.
* **Reduced Code Redundancy**: By using shared interfaces for common validation rules, the API reduces the need for repeated code across different endpoints, leading to cleaner, more maintainable codebases.
* **Improved Data Integrity and Validation Efficiency**: Interfaces ensure that validation logic is consistently applied across all relevant endpoints, enhancing data integrity and reducing the likelihood of errors. This structured approach also speeds up the validation process, as it leverages a unified set of rules.
* **Streamlined Client Integration**: The consistent data structure and validation rules facilitated by interfaces make it easier for client-side applications, particularly those using Blazor, to bind forms to API commands and queries. This results in a smoother development experience and faster feature integration.

### Negative Consequences

* **Pattern Familiarization**: The primary challenge lies in understanding and adopting the endpoint-centric design pattern with interface validation. While this approach offers significant architectural benefits, it demands proficiency in its principles and practical applications. This learning curve is mitigated by the examples and documentation included in the TimeWarp Architecture.

## Pros and Cons of the Options

### Entity-Centric API Design

* Good, because it simplifies the API model by reducing the number of DTOs.
* Bad, because it couples the DTOs across multiple endpoints, potentially leading to less flexibility.
* Bad, because it may lead to less flexibility in tailoring data structures to specific endpoint needs.
* Bad, because it can result in over-fetching of data.

### Endpoint-Centric API Design without Interface Validation

* Good, because it ensures maximum flexibility and control over each endpoint's data contracts.
* Bad, because it leads to potential redundancy in validation logic across similar DTOs.
* Bad, because it complicates form data binding in the UX.

### Endpoint-Centric API Design with Interface Validation

* Good, because it maintains the flexibility of endpoint-specific DTOs while centralizing common validation logic.
* Good, because it streamlines integration with UI by standardizing data binding practices.

## Links

* [API Design](../ApiDesign.md)
