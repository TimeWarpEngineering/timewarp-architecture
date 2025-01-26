# TimeWarpNavLink Component Implementation

## Description
Create a new TimeWarpNavLink component to standardize navigation link implementation across the application. This will replace the current static NavLink property pattern with a more strongly-typed and maintainable approach.

## Tasks
- [ ] Create ITimeWarpPage interface with required static properties:
  - GetPageUrl
  - Title
  - Icon
- [ ] Create TimeWarpNavLink component that accepts a generic type parameter TPage where TPage implements ITimeWarpPage
- [ ] Update existing pages to implement ITimeWarpPage interface
- [ ] Refactor LeftPane_Main.razor to use the new TimeWarpNavLink component
- [ ] Test implementation with multiple pages

## Implementation Details
### ITimeWarpPage Interface
```csharp
public interface ITimeWarpPage
{
    static abstract string GetPageUrl();
    static abstract string Title { get; }
    static abstract RenderFragment NavigationIconContent { get; }
}
```

### TimeWarpNavLink Component Usage
```razor
<TimeWarpNavLink TPage="TestPage" />
```

### Example Page Implementation
```csharp
public partial class TestPage : ITimeWarpPage
{
    public static string Title => "Test";
    public static RenderFragment NavigationIconContent => @<Icons.Regular.Size20.Question />;
    public static string GetPageUrl() => "/Debugger/Test";
}
```

## Definition of Done
- [ ] TimeWarpNavLink component implemented and working
- [ ] All existing pages updated to use new pattern
- [ ] LeftPane_Main.razor refactored to use new component
- [ ] Code reviewed and approved
- [ ] Documentation updated
