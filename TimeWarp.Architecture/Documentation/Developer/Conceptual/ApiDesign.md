# API Design: Endpoint-Centric vs Entity-Centric

In API design, there are two common approaches to designing Data Transfer Objects (DTOs): the Endpoint-Centric approach and the Entity-Centric approach. Both approaches have their pros and cons, and the best choice often depends on your specific situation and needs.

## Endpoint-Centric Design

The Endpoint-Centric approach, also known as the "isolated" approach, involves creating unique command and response DTOs for each endpoint. 

This can be thought of as tailoring the structure of the DTOs to exactly what each endpoint needs, without being constrained by the needs of other endpoints.

Here's an example in C#:

```csharp
public class CreateAddressCommand
{
    // Properties specific to creating an address...
}

public class CreateAddressResponse
{
    // Properties specific to the response from creating an address...
}

// Repeat for each endpoint...
```

### Pros
- Gives you precise control over the data contract for each endpoint.
- Can make your code easier to understand and maintain.
    
### Cons
- Can lead to a lot of similar or duplicated code.
- Can make your API harder to use.

## Entity-Centric Design

The Entity-Centric approach, also known as the "shared DTO" or "common DTO" approach, involves creating DTOs that are shared across different endpoints.

This can be thought of as designing the DTOs around specific entities (e.g., an `AddressDto` used across multiple address-related endpoints) or operations (e.g., CRUD operations).

Here's an example in C#:

```csharp
public class AddressDto
{
    // Properties for an address...
}

public class CreateAddressCommand
{
    public AddressDto Address { get; set; }
}

public class UpdateAddressCommand
{
    public AddressDto Address { get; set; }
}
```

### Pros
- Emphasizes reusability and consistency.
- Can make the API easier to understand and use.
- Can reduce code duplication.
    
### Cons
- Can lead to less flexibility.
- Can result in over-fetching or under-fetching of data.

---

In practice, many APIs use a mix of both approaches, with shared DTOs for common or simple operations, and endpoint-specific DTOs for more complex or unique operations. The best choice depends on your specific situation, including the complexity of your system, the needs of your API clients, and your team's preferences.
