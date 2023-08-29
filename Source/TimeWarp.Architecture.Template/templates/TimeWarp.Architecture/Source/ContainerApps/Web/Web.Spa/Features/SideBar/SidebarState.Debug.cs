namespace TimeWarp.Architecture.Features.Sidebars;

internal partial class SidebarState : State<SidebarState>
{
  public override SidebarState Hydrate(IDictionary<string, object> aKeyValuePairs)
  {
    return new SidebarState
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
