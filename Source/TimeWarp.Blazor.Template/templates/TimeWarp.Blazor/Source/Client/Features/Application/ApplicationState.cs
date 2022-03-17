namespace TimeWarp.Architecture.Features.Applications
{
  using BlazorState;
  using System.Collections.Generic;
  using System.Linq;

  internal partial class ApplicationState : State<ApplicationState>
  {
    private List<string> _ProcessingList;

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
      Name = "TimeWarp.Blazor";
      Logo = "/images/logo.png";
    }
  }
}
