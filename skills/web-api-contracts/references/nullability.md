# Nullability in API Contracts

Types declare intent. Validators enforce intent at runtime. **They must agree.**

## Principle

A nullable annotation (`T?`) is a **contract promise**: null is a valid, meaningful state
that consuming code must handle.

A non-nullable annotation (`T`) is a **contract promise**: null is never a valid
semantic state. The value must be present before the request is considered valid.

Validators do not redefine this — they enforce what the type already promised.

## Requests and commands

### Required property (validator will reject missing/empty)

```csharp
public string MandatoryEmail { get; set; } = null!;

// Validator
RuleFor(x => x.MandatoryEmail).NotEmpty();
```

Use `= null!` to satisfy the compiler at construction/deserialization. The value is
unset until JSON binds or the user fills the form; the validator is the gate before
handler execution.

### Optional property (absence is valid)

```csharp
public string? OptionalNickname { get; set; }

// Validator — only when present
RuleFor(x => x.OptionalNickname).MaximumLength(50).When(x => x.OptionalNickname != null);
```

No unconditional `NotEmpty()`, `NotNull()`, or `Must(x => x != null)` on optional properties.

### Contradiction — never do this

```csharp
// WRONG: type says null is OK, validator says it is not
public string? Name { get; set; }
RuleFor(x => x.Name).NotEmpty();
```

Fix: `public string Name { get; set; } = null!` + `NotEmpty()`.

### Forbidden initializers on required reference types

| Forbidden | Why |
|-----------|-----|
| `= string.Empty` | JSON omits property → stays `""` → `NotEmpty()` passes → silent wrong data |
| `= default!` (non-generic) | Misleading; use `null!` |
| `= new()` on required nested object when emptiness must fail validation | Empty shell may pass `NotNull()` but fail business rules late; prefer `= null!` + `NotNull().SetValidator(...)` unless the nested type's default state is intentionally valid |

### Generics

`default!` is acceptable only for generic type parameters `<T>` where the default
depends on `T`.

## Responses

Responses are **server-authored**. Invariants belong in constructors, not FluentValidation.

```csharp
public sealed class Response
{
  public string Email { get; }
  public string Name { get; }

  public Response(string email, string name)
  {
    Email = Guard.Against.NullOrWhiteSpace(email);
    Name = Guard.Against.NullOrWhiteSpace(name);
  }
}
```

Optional response fields: nullable type or omitted from ctor requirements.

```csharp
public string? Specialty { get; init; }
```

**Why not FluentValidation on Response?** Validation gives user-facing feedback. Constructor
exceptions signal developer/handler bugs.

## Bindable interfaces (`I*Details`)

The same rules apply to interface properties:

- Required bindable field: `string Name { get; set; }` (non-nullable) + `NotEmpty()` in
  `AbstractValidator<I*Details>`
- Optional bindable field: `string? MiddleName { get; set; }` — no unconditional `NotEmpty()`

## Decision tree

```
Will null/absent ever be valid for this field after the request is accepted?
├── YES → T? (nullable). Validator rules only when value is present (.When).
└── NO  → T (non-nullable). Initialize with null! (refs) or default (value types).
          Validator enforces NotEmpty/NotNull/GreaterThan/etc.
```