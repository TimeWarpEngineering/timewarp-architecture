#region Purpose
// Redux DevTools rehydration and test-only initialization for ProfileMenuState.
#endregion

#region Design
// Hydrate rebuilds the state from DevTools' camelCase JSON keys (hence the CamelCase name
// mapping) so time-travel debugging can restore MenuState.
// Initialize lets tests set an arbitrary MenuState without dispatching actions; it is
// gated by ThrowIfNotTestAssembly so production code cannot bypass the action pipeline.
#endregion

namespace TimeWarp.Architecture.Features.ProfileMenus;

partial class ProfileMenuState
{
  public override ProfileMenuState Hydrate(IDictionary<string, object> keyValuePairs)
  {
    return new ProfileMenuState
    {
      Guid = new Guid(keyValuePairs[CamelCase.MemberNameToCamelCase(nameof(Guid))].ToString() ?? throw new InvalidOperationException()),

      MenuState =
        Enum.Parse<MenuStates>
        (
          keyValuePairs[CamelCase.MemberNameToCamelCase(nameof(MenuState))].ToString() ?? throw new InvalidOperationException()
        ),
    };
  }

  internal void Initialize(MenuStates menuState)
  {
    ThrowIfNotTestAssembly(Assembly.GetCallingAssembly());
    MenuState = menuState;
  }
}
