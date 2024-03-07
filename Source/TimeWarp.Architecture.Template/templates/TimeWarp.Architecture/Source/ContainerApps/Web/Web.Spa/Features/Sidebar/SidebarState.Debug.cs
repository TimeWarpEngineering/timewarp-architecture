namespace TimeWarp.Architecture.Features.Sidebars;

internal partial class SidebarState
{
  public override SidebarState Hydrate(IDictionary<string, object> keyValuePairs)
  {
    return new SidebarState
    {
      Guid = new Guid(keyValuePairs[CamelCase.MemberNameToCamelCase(nameof(Guid))].ToString() ?? throw new InvalidOperationException()),
      IsOpen = bool.Parse(keyValuePairs[CamelCase.MemberNameToCamelCase(nameof(IsOpen))].ToString() ?? throw new InvalidOperationException()),
    };
  }

  internal void Initialize(bool isOpen)
  {
    ThrowIfNotTestAssembly(Assembly.GetCallingAssembly());
    IsOpen = isOpen;
  }
}

