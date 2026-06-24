# Add Authentication Support to TimeWarpNavLink Component

## Description
Modify the existing TimeWarpNavLink component to support authentication by adding an optional Policy parameter and AuthorizeView wrapper.

## Acceptance Criteria
- [x] Create AuthorizationConstants with Anonymous policy
- [x] Add Policy property to INavigableComponent interface
- [x] Add Policy support to Page attribute
- [x] Modify Page.mixin to generate Policy property
- [x] Update TimeWarpNavLink to use AuthorizeView consistently
- [x] Register Anonymous policy in startup
- [x] Keep existing type constraints (TPage : INavigableComponent, IStaticRoute)
- [x] Update documentation to reflect new authentication approach

## Technical Details
- Files to modify:
  - Source/ContainerApps/Web/Web.Spa/Components/Elements/TimeWarpNavLink.razor
  - Source/ContainerApps/Web/Web.Spa/Components/Interfaces/INavigableComponent.cs
  - Source/ContainerApps/Web/Web.Spa/Mixins/Page.mixin
  - Source/ContainerApps/Web/Web.Spa/Features/Authorization/AuthorizationConstants.cs
  - Source/ContainerApps/Web/Web.Spa/Features/Authorization/PolicyRegistration.cs
- Reference Implementation: AuthorizedFluentNavLink.razor

## Implementation Notes

1. Add AuthorizationConstants:
```csharp
public static class AuthorizationConstants
{
  public static class Policies
  {
    public const string Anonymous = "Anonymous";
  }
}
```

2. Register Anonymous Policy:
```csharp
builder.Services.AddAuthorization(options =>
{
  options.AddPolicy(AuthorizationConstants.Policies.Anonymous, 
    policy => policy.RequireAssertion(context => true));
});
```

3. Update Page Attribute:
```csharp
[Page("/path", Policy = "PolicyName")]  // Specific policy
[Page("/public")]                       // Defaults to Anonymous
```

4. Modify TimeWarpNavLink:
```csharp
<AuthorizeView Policy="@TPage.Policy">
  <Authorized>
    <FluentNavLink
      Icon=TPage.NavIcon
      Href=@TPage.GetPageUrl()
      Match=NavLinkMatch.All>
      @TPage.Title
    </FluentNavLink>
  </Authorized>
</AuthorizeView>
```

Key Design Decisions:
- Policy is part of INavigableComponent contract
- Use AuthorizeView consistently for all routes
- Default to Anonymous policy when not specified
- Anonymous policy allows all requests
- Single authorization path eliminates conditional logic
