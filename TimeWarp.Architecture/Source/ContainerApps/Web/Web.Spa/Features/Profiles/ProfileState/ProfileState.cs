namespace TimeWarp.Architecture.Features.Profiles;

[StateAccessMixin]
public sealed partial class ProfileState: State<ProfileState>
{
  public string? Alias { get; private set; }
  public string? Avatar { get; private set; }

  public override void Initialize()
  {
    Alias = null;
    Avatar = null;
  }
}
