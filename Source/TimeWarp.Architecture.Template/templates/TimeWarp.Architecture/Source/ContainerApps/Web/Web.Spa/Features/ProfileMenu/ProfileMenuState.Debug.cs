namespace TimeWarp.Architecture.Features.ProfileMenus;

internal partial class ProfileMenuState : State<ProfileMenuState>
{
  public override ProfileMenuState Hydrate(IDictionary<string, object> aKeyValuePairs)
  {
    return new ProfileMenuState
    {
      Guid = new System.Guid(aKeyValuePairs[CamelCase.MemberNameToCamelCase(nameof(Guid))].ToString()),

      MenuState =
        (MenuStates)Enum.Parse
        (
          typeof(MenuStates),
          aKeyValuePairs[CamelCase.MemberNameToCamelCase(nameof(MenuState))].ToString()
        ),
      
    };
  }

  internal void Initialize(bool isOpen)
  {
    ThrowIfNotTestAssembly(Assembly.GetCallingAssembly());
    MenuState = MenuStates.Closed;
  }
}
