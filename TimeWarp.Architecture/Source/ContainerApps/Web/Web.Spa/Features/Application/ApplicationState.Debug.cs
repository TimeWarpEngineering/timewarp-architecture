namespace TimeWarp.Architecture.Features.Applications;

internal partial class ApplicationState
{
  public override ApplicationState Hydrate(IDictionary<string, object> keyValuePairs)
  {
    return new ApplicationState
    {
      Guid = new Guid(keyValuePairs[CamelCase.MemberNameToCamelCase(nameof(Guid))].ToString() ?? throw new InvalidOperationException()),
      Name = keyValuePairs[CamelCase.MemberNameToCamelCase(nameof(Name))].ToString() ?? throw new InvalidOperationException(),
    };
  }

  internal void Initialize(string aName, string aLogo, bool aIsMenuExpanded)
  {
    ThrowIfNotTestAssembly(Assembly.GetCallingAssembly());
    Name = aName;
    Logo = aLogo;
    IsMenuExpanded = aIsMenuExpanded;
  }
}
