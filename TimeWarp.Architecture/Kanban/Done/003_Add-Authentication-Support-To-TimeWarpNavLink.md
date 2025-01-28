# Add Authentication Support to TimeWarpNavLink Component

## Description
Modify the existing TimeWarpNavLink component to support authentication by adding an optional Policy parameter and AuthorizeView wrapper.

## Acceptance Criteria
- [ ] Add optional Policy parameter to TimeWarpNavLink
- [ ] Add AuthorizeView wrapper when Policy is provided
- [ ] Maintain existing functionality when Policy is not provided
- [ ] Keep existing type constraints (TPage : INavigableComponent, IStaticRoute)
- [ ] Update XML documentation to reflect new authentication capability

## Technical Details
- File: Source/ContainerApps/Web/Web.Spa/Components/Elements/TimeWarpNavLink.razor
- New Parameter: Policy (string, optional)
- Reference Implementation: AuthorizedFluentNavLink.razor

## Implementation Notes
- When Policy is null/not provided, render without AuthorizeView (maintain current behavior)
- When Policy is provided, wrap content in AuthorizeView with specified policy
- Follow pattern from AuthorizedFluentNavLink but make it optional
