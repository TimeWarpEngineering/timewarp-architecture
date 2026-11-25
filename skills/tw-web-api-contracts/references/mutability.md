# Mutability in API Contracts

Mutability is a **purpose signal**, not a convenience default.

## Why this matters

Default .NET API guidance (immutable records everywhere) forces Blazor apps to duplicate
every editable shape as a separate view model. Endpoint-centric contracts avoid that:

- **Immutable** members → read/display path; user cannot bind and edit.
- **Mutable** members on `I*Details` → the `Create*` / `Update*` Command implements them, and
  that Command is the `EditForm` model. No view model sits between the form and the request
  it sends.

The `Get*` Response implements the same `I*Details` so the generated `From` can copy it into
the Command. The form never binds the Response itself. See
[Edit forms: store snapshot and working copy](#edit-forms-store-snapshot-and-working-copy).

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

- `Create*` / `Update*` **Command implements** `I*Details`. The form binds that Command.
- `Get*` **Response implements** `I*Details` when an edit form loads it, so
  `Update*.Command.From(response)` can copy it.
- Blazor components bind against `I*Details` over a Command instance, never over the
  Response held in the store.

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

## Edit forms: store snapshot and working copy

An edit form has two objects: the Response in the state store and the Command the form edits.

1. **The store holds the whole fetched `Get*.Response`.** For the query an edit form loads, a
   state property is the Response object itself, not flattened copies of its fields. A store
   holds no unsaved form draft.
2. **A mutable details Response gets a real `Clone`.** TimeWarp.State's transaction behavior
   clones state before every handler. It calls `ICloneable.Clone()` when the object implements
   it. `Clone() => this` shares the instance, so it is right only for an immutable contract. For
   a `partial` Response whose `I*Details` has a `set` (not `init`), the contracts generator emits
   `ICloneable.Clone()`: a `MemberwiseClone`, then new `List<T>`, `HashSet<T>`,
   `Dictionary<TKey, TValue>`, and rank-1 arrays (and their interfaces). Elements are shared,
   except a reference type other than `string` that implements `ICloneable`: each such element
   is cloned. A null nullable collection stays null; a null non-nullable collection becomes
   empty. Any other collection type is assigned as-is, so it stays shared. Get-only and `init`
   members are copied without running the constructor. When an object does not implement
   `ICloneable`, TimeWarp.State builds a new instance and copies its fields (auto-property
   backing fields included) itself.
3. **`I*Details` drives a generated `Update*.Command.From(I*Details)`.** Every interface property
   is copied, and collections follow the same rules as `Clone` (new collection, shared elements
   unless `ICloneable`). The form edits that Command as a
   working copy outside the store. The copy runs on load, on Cancel, and after the page's own
   successful save. It does not run on every `OnParametersSet` or render. A later store
   response is reconciled where the component sees it, and the two places use different rules:
   - **A page that reads the store** reconciles in `ShouldReRender` (a state re-render does not
     call `OnParametersSet`). A clean Command is replaced with `From` on the new response. A
     dirty Command keeps its field edits, and copies only the new `Version` when the new
     response's form fields still match the snapshot the Command was copied from
     (`AuthenticationPage.razor`).
   - **A child form that takes the Response as a parameter** reconciles in `OnParametersSet`.
     It calls `From` when the response's key fields (for a role: id, name, description) differ
     from the values it last copied, which replaces the working copy and drops its edits. While
     those fields match, it keeps the Command as it is and does not patch `Version` onto it
     (`RoleEditForm.razor`).
4. **`Version` stays off the `I*Details` interface.** It is a concurrency token, not a field the
   user edits. `From` ends by calling `static partial void CopyBeyondDetails(Command command,
   I*Details source)`. The Command implements it and copies `Version` (or a route id such as
   `RoleId`) from the concrete Response. Without a body the call is erased. A `required` member
   cannot be set there, because the hook runs after `new()`.
5. **View-only display does not bind the store object to inputs.** Read the Response for display
   text. Anything with an input binds the Command.

```csharp
public interface ISiteSettingsDetails
{
  bool EntraSignInEnabled { get; set; }
  List<EntraAcceptedTenant> EntraAcceptedTenants { get; set; }
}

public static partial class GetSiteSettings
{
  // partial: the generator adds ICloneable and a deep Clone (the list is copied).
  public sealed partial class Response : ISiteSettingsDetails
  {
    public bool EntraSignInEnabled { get; set; }
    public List<EntraAcceptedTenant> EntraAcceptedTenants { get; set; } = [];
    public long Version { get; }
    public Response(bool entraSignInEnabled, long version) { … }
  }
}

public static partial class UpdateSiteSettings
{
  // partial with a parameterless constructor: the generator adds
  // static Command From(ISiteSettingsDetails source).
  public sealed partial class Command : IApiRequest, ISiteSettingsDetails, ICommand<…>
  {
    public bool EntraSignInEnabled { get; set; }
    public List<EntraAcceptedTenant> EntraAcceptedTenants { get; set; } = [];
    public long Version { get; set; }

    static partial void CopyBeyondDetails(Command command, ISiteSettingsDetails source)
    {
      if (source is GetSiteSettings.Response loaded)
      {
        command.Version = loaded.Version;
      }
    }
  }
}

// In the page: copy on load, Cancel, and after a successful save.
Command = UpdateSiteSettings.Command.From(SiteSettingsState.SiteSettings);
```

| Id | Reports |
| --- | --- |
| TWA0047 | A mutable `I*Details` Response whose `Clone` returns `this`. |
| TWA0048 | A Command or mutable details Response, or a type containing it, that is not `partial`. |
| TWA0049 | A `partial` Command with no parameterless instance constructor, so `From` is not generated. |

Living references: `AuthenticationPage.razor` (`web-spa/features/admin/site-settings/pages/`,
reconciles in `ShouldReRender`) and `RoleEditForm.razor` (`web-spa/features/admin/roles/components/`,
takes `GetRole.Response` as a parameter).

## Binding the interface in Blazor

The Blazor side is validation-library-dependent. Binding `EditForm` to the *interface* (over the
Command working copy) and running the shared validator requires a library that accepts an
**explicit validator instance** — **Blazilla**:
`<FluentValidator Validator="@(new RoleDetailsValidator())" />`. Libraries that resolve
validators by the model's *runtime type* (Morris.Blazor.FluentValidation) can never find an
`AbstractValidator<I*Details>` for a `Command` model; Blazored.FluentValidation is deprecated.
Living references: `RoleForm.razor` (create) and `RoleEditForm.razor` (edit) in
`web-spa/features/admin/roles/components/`.
