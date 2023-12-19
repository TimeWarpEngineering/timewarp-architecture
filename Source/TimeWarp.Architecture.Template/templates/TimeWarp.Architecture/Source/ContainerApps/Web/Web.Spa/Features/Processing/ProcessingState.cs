namespace TimeWarp.Architecture.Features.Processing;

[StateAccessMixin]
internal partial class ProcessingState : State<ProcessingState>
{
  private List<string> _ProcessingList;

  public bool IsProcessing => _ProcessingList.Count > 0;

  #region snippet_IsProcessAny
  public bool IsProcessingAny(params string[] aActions) => aActions.Intersect(_ProcessingList).Any();
  #endregion

  public IReadOnlyList<string> ProcessingList => _ProcessingList.AsReadOnly();

  public ProcessingState() { }

  public override void Initialize()
  {
    _ProcessingList = new List<string>();
  }
}
