# Adoption of Endpoint-Centric API Design with Interface-Based Validation

* Status: proposed
* Deciders: Amina, Architect
* Date: 2024-04-23

Technical Story: Improve API maintainability and usability by reducing validation redundancy and improving data binding in the user interface layer.

## Context and Problem Statement

We have encountered issues with data validation redundancy and data binding difficulties in our current API design. How can we streamline API operations and improve maintainability without compromising flexibility and ease of use?

## Decision Drivers

* Need for a more maintainable API with less duplicate validation logic.
* Requirement to improve data binding between the UI forms and the API.
* Desire to keep API flexibility and responsiveness to changes in business logic.

## Considered Options

* Continue using separate DTOs for each endpoint without interfaces.
* Use shared DTOs across multiple endpoints.
* Adopt endpoint-specific DTOs with shared interfaces for common properties and validation.

## Decision Outcome

Chosen option: "Adopt endpoint-specific DTOs with shared interfaces for common properties and validation", because it offers the best balance between maintainability and flexibility. It simplifies validation logic by centralizing it in interfaces and enhances UI binding compatibility.

### Positive Consequences

* Reduction in duplicated validation code across different DTOs.
* Improved consistency in data binding from the UI to the API.
* Maintains the flexibility to customize DTOs for specific endpoints without compromising the reusability of the validation logic.

### Negative Consequences

* Requires initial effort to redesign DTOs and implement interfaces.
* Developers must understand interface inheritance and its impact on API design.

## Pros and Cons of the Options

### Continue using separate DTOs for each endpoint without interfaces

* Good, because it maintains strict separation of concerns.
* Bad, because it leads to significant duplication in validation logic.
* Bad, because it complicates the data binding process in complex UIs.

### Use shared DTOs across multiple endpoints

* Good, because it reduces the need to duplicate code and validation.
* Bad, because it can lead to over-fetching or under-fetching of data.
* Bad, because it can reduce the flexibility to tailor data structures to specific endpoint needs.

### Adopt endpoint-specific DTOs with shared interfaces for common properties and validation

* Good, because it reduces code duplication while maintaining endpoint flexibility.
* Good, because it streamlines UI data binding through consistent interfaces.
* Bad, because it requires careful design to ensure interface definitions are comprehensive and future-proof.

## Links

* Further discussion on the topic can be found in the internal wiki.
