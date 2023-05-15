namespace TimeWarp.Architecture.Features.Applications.Spa;

[StateAccessMixin]
internal partial class ApplicationState : State<ApplicationState>
{
  private List<string> _ProcessingList;

  public string ActiveModalId { get; private set; }
  public bool IsMenuExpanded { get; private set; }
  public string Logo { get; private set; }
  public string Name { get; private set; }
  public string Version => GetType().Assembly.GetName().Version.ToString();
  public bool IsProcessing => _ProcessingList.Count > 0;

  #region snippet_IsProcessAny
  public bool IsProcessingAny(params string[] aActions) => aActions.Intersect(_ProcessingList).Any();
  #endregion

  public IReadOnlyList<string> ProcessingList => _ProcessingList.AsReadOnly();

  public ApplicationState() { }

  public override void Initialize()
  {
    _ProcessingList = new List<string>();
    IsMenuExpanded = true;
    Name = "TimeWarp.Architecture";
    Logo = "/images/logo.png";
  }
}
