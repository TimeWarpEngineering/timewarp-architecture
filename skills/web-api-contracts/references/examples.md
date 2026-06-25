# Reference Implementations

This skill is **repo-agnostic**. Do not assume file paths from any particular solution.
Always discover conventions in the **current repo** first.

## Discover conventions in the current repo

```bash
# Find the contracts project
find . -name '*Contracts*.csproj' -not -path '*/obj/*' -not -path '*/Tests/*'

# Find existing contracts to mirror
rg -l 'public static partial class' --glob '**/Features/**/*.cs'
rg -l 'RouteMixin' --glob '**/Features/**/*.cs'

# Find the contracts test project
find . -path '*Contracts.Tests*' -name '*.csproj' -not -path '*/obj/*'

# Find mock service registration
rg -l 'GetMockResponseFactory|Mock.*ApiService' --glob '**/*.cs'
```

Read 2–3 files from the same feature area before scaffolding a new contract.

## Tier 1 — Simple query (route-only)

Empty `Query` body; route params from `RouteMixin`; empty validator.

```csharp
namespace MyApp.Features.Announcements;

public static partial class GetAnnouncementsForCurrentUser
{
  [RouteMixin("api/Users/Current/Announcements", HttpVerb.Get)]
  public sealed partial class Query : IApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>;

  public sealed class Validator : AbstractValidator<Query>;

  public sealed class Response : ListResponse<AnnouncementDto>
  {
    public Response(int totalCount, AnnouncementDto[] items) : base(totalCount, items) { }
  }

  public sealed class AnnouncementDto
  {
    public string? Text { get; init; }
    public string? Link { get; init; }
  }

  public static MockResponseFactory<Response> GetMockResponseFactory()
  {
    AnnouncementDto[] items = [new() { Text = "Sample", Link = "/home" }];
    return _ => new Response(totalCount: items.Length, items);
  }
}
```

## Tier 2 — CRUD with bindable interface

Folder `Features/Admin/SecurityRole/` · namespace `MyApp.Features.Admin.SecurityRoles`.

**Shared shape + validator** (`SecurityRoleDetails.cs`):

```csharp
namespace MyApp.Features.Admin.SecurityRoles;

public interface ISecurityRoleDetails
{
  public ApplicationId ApplicationId { get; set; }
  public string Name { get; set; } = null!;
  public string? Description { get; set; }
  public string Code { get; set; } = null!;
}

public sealed class SecurityRoleDetailsValidator : AbstractValidator<ISecurityRoleDetails>
{
  public SecurityRoleDetailsValidator()
  {
    RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
    RuleFor(x => x.Description).MaximumLength(255).When(x => x.Description != null);
    RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
  }
}
```

**Create command** — implements interface, composes validator:

```csharp
public static partial class CreateSecurityRole
{
  [RouteMixin("api/SecurityRoles", HttpVerb.Post)]
  public sealed partial class Command
    : IApiRequest, ISecurityRoleDetails, IRequest<OneOf<Response, SharedProblemDetails>>;

  public sealed class Validator : AbstractValidator<Command>
  {
    public Validator() => RuleFor(x => x).SetValidator(new SecurityRoleDetailsValidator());
  }

  public sealed class Response
  {
    public required int SecurityRoleId { get; init; }
  }
}
```

**Get for edit** — Response implements same interface:

```csharp
public static partial class GetSecurityRole
{
  [RouteMixin("api/SecurityRoles/{SecurityRoleId:min(1)}", HttpVerb.Get)]
  public sealed partial class Query : IApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>;

  public sealed class Validator : AbstractValidator<Query>;

  public sealed class Response : ISecurityRoleDetails
  {
    public int SecurityRoleId { get; }
    public ApplicationId ApplicationId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string Code { get; set; } = null!;

    public Response(int securityRoleId, ApplicationId applicationId, string name, string code)
    {
      SecurityRoleId = Guard.Against.NegativeOrZero(securityRoleId);
      ApplicationId = applicationId;
      Name = Guard.Against.NullOrWhiteSpace(name);
      Code = Guard.Against.NullOrWhiteSpace(code);
    }
  }
}
```

## Tier 3 — Filterable list + query string

Optional nullable filters; no unconditional required rules on filters.

```csharp
public static partial class GetAccountOwnedPolicies
{
  [RouteMixin("api/Accounts/{AccountId:int}/Policies/Owned", HttpVerb.Get)]
  public sealed partial class Query : IQueryStringRouteProvider, IRequest<OneOf<Response, SharedProblemDetails>>
  {
    public string? State { get; set; }

    public string GetRouteWithQueryString()
    {
      var parameters = new NameValueCollection { { nameof(State), State } };
      return $"{GetRoute()}?{this.GetQueryString(parameters)}";
    }
  }

  public sealed class Validator : AbstractValidator<Query>
  {
    public Validator()
    {
      RuleFor(x => x.State).Length(2).When(x => x.State != null);
    }
  }
}
```

## Tier 4 — Contract test (serialization)

```csharp
public class Command_Should_
{
  public static void SerializeAndDeserialize()
  {
    JsonSerializerOptions options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    CreateSecurityRole.Command command = new() { Name = "Admin", Code = "ADMIN" };
    string json = JsonSerializer.Serialize(command, options);
    CreateSecurityRole.Command? parsed = JsonSerializer.Deserialize<CreateSecurityRole.Command>(json, options);

    parsed.Should().BeEquivalentTo(command);
  }
}
```

## Namespace vs folder (intentional mismatch)

| Folder (singular) | Namespace (plural) |
|-------------------|-------------------|
| `Features/Admin/SecurityRole/` | `MyApp.Features.Admin.SecurityRoles` |
| `Features/Incident/` | `MyApp.Features.Incidents` |

Plural namespace avoids collisions with `SecurityRole`, `User`, `Policy` classes.

## Legacy code warning

Older production repos may contain `string?` + unconditional `NotEmpty()` — a
nullability contradiction. **Do not copy when writing new contracts.** Apply the rules
in [nullability.md](nullability.md).