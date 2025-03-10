namespace TimeWarp.Architecture.Features.Applications;

[StateAccessMixin]
public sealed partial class ApplicationState : State<ApplicationState>
{
  public string? ActiveModalId { get; private set; }
  public bool IsMenuExpanded { get; private set; }
  public string? Logo { get; private set; }
  public string Name { get; private set; } = null!;
  public string? Version => GetType().Assembly.GetName().Version?.ToString();

  public ApplicationState() { }

  public override void Initialize()
  {
    IsMenuExpanded = true;
    Name = "TimeWarp.Architecture";
    Logo = "/images/logo.png";
  }
}
