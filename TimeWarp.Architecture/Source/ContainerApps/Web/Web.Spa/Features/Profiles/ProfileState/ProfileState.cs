namespace TimeWarp.Architecture.Features.Profiles;

[StateAccessMixin]
public sealed partial class ProfileState: State<ProfileState>
{
  public string? Alias { get; private set; }
  public string? Avatar { get; private set; }

  public override void Initialize()
  {
    Alias = null;
    Avatar = new Icons.Regular.Size48.Person().ToDataUri(size: "25px", color: "white");
  }
}
