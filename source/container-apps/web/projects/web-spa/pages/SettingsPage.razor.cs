#region Purpose
// Code-behind that binds the /Settings route to the Settings placeholder page.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

[Page("/Settings", Policy = Policies.CanViewSettings)]
[Authorize(Policy = Policies.CanViewSettings)]
partial class SettingsPage;
