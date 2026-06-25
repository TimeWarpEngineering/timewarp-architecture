# Mutability in API Contracts

Mutability is a **purpose signal**, not a convenience default.

## Why this matters

Default .NET API guidance (immutable records everywhere) forces Blazor apps to duplicate
every editable shape as a separate view model. Endpoint-centric contracts avoid that:

- **Immutable** members → read/display path; user cannot bind and edit.
- **Mutable** members on `I*Details` → Blazor `EditForm` binds directly; same type goes
  back on Create/Update.

Endpoint-centric design keeps each endpoint's types specific. Shared **validation** (not
shared DTOs) flows through interfaces.

## Immutable (read / display)

```csharp
public int UserId { get; init; }
public string UserName { get; init; }
public IReadOnlyList<CoverageDisplayDto> Coverages { get; init; }
```

Use for query responses, nested display DTOs, and identity fields on otherwise-editable
entities.

## Mutable (bindable / edit)

```csharp
public interface ISecurityRoleDetails
{
  public string Name { get; set; }
  public string Description { get; set; }
  public ApplicationId ApplicationId { get; set; }
}
```

- `Create*` / `Update*` **Command implements** `I*Details`.
- `Get*` **Response implements** `I*Details` when loading data into an edit form.
- Blazor components bind against `I*Details`, not the concrete Response/Command type.

## Collections

| Role | Type |
|------|------|
| Display only | `IReadOnlyList<T>` with `init` or get-only |
| Editable in UX | `List<T>` with `set` on `I*Details` |

## Shared validation

```csharp
public sealed class SecurityRoleDetailsValidator : AbstractValidator<ISecurityRoleDetails>
{
  public SecurityRoleDetailsValidator()
  {
    RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
    RuleFor(x => x.Description).MaximumLength(255);
  }
}

// In CreateSecurityRole.Validator:
RuleFor(x => x).SetValidator(new SecurityRoleDetailsValidator());
```

One validator, many endpoints, one bindable shape.

## Read-only display interfaces (not bindable)

Get-only interfaces (e.g. `IPolicyDto`) shared across list endpoints: **no `set`**, not
for `EditForm`, not `I*Details`.