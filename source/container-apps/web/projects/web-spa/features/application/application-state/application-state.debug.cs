#region Purpose
// Test-only seeding for ApplicationState.
#endregion

#region Design
// The Initialize overload bypasses the action pipeline so tests can seed state directly.
// State's ThrowIfNotTestAssembly blocks production callers (open only after the test host calls StateTestOptions.Enable(), 12.0.0-beta.11).
#endregion

namespace TimeWarp.Architecture.Features.Applications;

partial class ApplicationState
{
  internal void Initialize(string name, string logo, bool isMenuExpanded)
  {
    ThrowIfNotTestAssembly(Assembly.GetCallingAssembly());
    Name = name;
    Logo = logo;
    IsMenuExpanded = isMenuExpanded;
  }
}
