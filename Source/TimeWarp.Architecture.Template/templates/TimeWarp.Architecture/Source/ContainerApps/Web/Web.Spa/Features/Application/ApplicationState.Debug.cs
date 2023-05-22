namespace TimeWarp.Architecture.Features.Applications.Spa;

internal partial class ApplicationState : State<ApplicationState>
{
  public override ApplicationState Hydrate(IDictionary<string, object> aKeyValuePairs)
  {
    return new ApplicationState
    {
      Guid = new System.Guid(aKeyValuePairs[CamelCase.MemberNameToCamelCase(nameof(Guid))].ToString()),
      Name = aKeyValuePairs[CamelCase.MemberNameToCamelCase(nameof(Name))].ToString(),
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
