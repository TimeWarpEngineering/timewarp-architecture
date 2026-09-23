#region Purpose
// Debug/test support for ApplicationState: Redux DevTools rehydration and test-only seeding.
#endregion

#region Design
// Hydrate restores only the fields time-travel needs (Guid, Name) from the camelCased
// key/value payload Redux DevTools round-trips.
// The Initialize overload bypasses the action pipeline so tests can seed state directly.
// TestCaller.Ensure blocks production callers. State's ThrowIfNotTestAssembly rejects
// kebab *-tests names (timewarp-state#607).
#endregion

namespace TimeWarp.Architecture.Features.Applications;

partial class ApplicationState
{
  public override ApplicationState Hydrate(IDictionary<string, object> keyValuePairs)
  {
    return new ApplicationState
    {
      Guid = new Guid(keyValuePairs[CamelCase.MemberNameToCamelCase(nameof(Guid))].ToString() ?? throw new InvalidOperationException()),
      Name = keyValuePairs[CamelCase.MemberNameToCamelCase(nameof(Name))].ToString() ?? throw new InvalidOperationException(),
    };
  }

  internal void Initialize(string name, string logo, bool isMenuExpanded)
  {
    TimeWarp.Architecture.TestCaller.Ensure(Assembly.GetCallingAssembly());
    Name = name;
    Logo = logo;
    IsMenuExpanded = isMenuExpanded;
  }
}
