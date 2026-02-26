# 001: NavLink Encapsulation Implementation

## Description
Refactor navigation system to enable page-specific NavLink definitions through TimeWarpPage component parameters.

## Requirements
- Add NavLink RenderFragment to TimeWarpPage
- Update LeftPane_Main.razor to use cascaded NavLinks
- Migrate all pages
- Maintain accessibility features (ARIA labels, keyboard nav)

## Checklist

### Implementation  
- [ ] Add RenderFragment parameter
- [ ] Refactor LeftPane navigation
- [ ] Update page implementations
- [ ] Verify Functionality

### Review
- [ ] Accessibility check
- [ ] Security review (link validation)
- [ ] Performance impact analysis
- [ ] Code Review

## Notes
- Uses existing FluentNavLink component

## Implementation Notes
```razor
// TimeWarpPage.razor addition
[Parameter] public RenderFragment? NavLink { get; set; }

// LeftPane_Main.razor update
<CascadingValue Value="this">
  <FluentNavMenu>
    @TimeWarpPageInstance?.NavLink
  </FluentNavMenu>
</CascadingValue>
