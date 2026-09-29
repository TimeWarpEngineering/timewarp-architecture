#region Purpose
// Debug/test support for ApplicationState: Redux DevTools rehydration and test-only seeding.
#endregion

#region Design
// Hydrate restores only the fields time-travel needs (Guid, Name) from the camelCased
// key/value payload Redux DevTools round-trips.
// The Initialize overload bypasses the action pipeline so tests can seed state directly.
// State's ThrowIfNotTestAssembly blocks production callers (case-insensitive since
// 12.0.0-beta.5, so kebab *-tests assemblies pass — timewarp-state#607).
#endregion

namespace TimeWarp.Architecture.Features.Applications;

partial class ApplicationState
{
  public override ApplicationState Hydrate(IDictionary<string, object> keyValuePairs)
  {
    return new ApplicationState
    {
      Guid = new Guid(keyValuePairs[JsonNamingPolicy.CamelCase.ConvertName(nameof(Guid))].ToString() ?? throw new InvalidOperationException()),
      Name = keyValuePairs[JsonNamingPolicy.CamelCase.ConvertName(nameof(Name))].ToString() ?? throw new InvalidOperationException(),
    };
  }

  internal void Initialize(string name, string logo, bool isMenuExpanded)
  {
    ThrowIfNotTestAssembly(Assembly.GetCallingAssembly());
    Name = name;
    Logo = logo;
    IsMenuExpanded = isMenuExpanded;
  }
}
