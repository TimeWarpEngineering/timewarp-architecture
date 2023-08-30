namespace TimeWarp.Architecture.Features.ProfileMenus;

internal partial class ProfileMenuState : State<ProfileMenuState>
{
  public override ProfileMenuState Hydrate(IDictionary<string, object> aKeyValuePairs)
  {
    return new ProfileMenuState
    {
      Guid = new System.Guid(aKeyValuePairs[CamelCase.MemberNameToCamelCase(nameof(Guid))].ToString()),
      IsOpen = bool.Parse(aKeyValuePairs[CamelCase.MemberNameToCamelCase(nameof(IsOpen))].ToString()),
    };
  }

  internal void Initialize(bool isOpen)
  {
    ThrowIfNotTestAssembly(Assembly.GetCallingAssembly());
    IsOpen = isOpen;
  }
}
