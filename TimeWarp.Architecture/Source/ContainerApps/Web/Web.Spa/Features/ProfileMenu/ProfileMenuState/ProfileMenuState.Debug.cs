namespace TimeWarp.Architecture.Features.ProfileMenus;

partial class ProfileMenuState
{
  public override ProfileMenuState Hydrate(IDictionary<string, object> keyValuePairs)
  {
    return new ProfileMenuState
    {
      Guid = new Guid(keyValuePairs[CamelCase.MemberNameToCamelCase(nameof(Guid))].ToString() ?? throw new InvalidOperationException()),

      MenuState =
        (MenuStates)Enum.Parse
        (
          typeof(MenuStates),
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
