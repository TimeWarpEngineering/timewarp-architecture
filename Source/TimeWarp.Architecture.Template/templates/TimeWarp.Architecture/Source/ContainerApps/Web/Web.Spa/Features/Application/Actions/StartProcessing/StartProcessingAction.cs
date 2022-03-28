namespace TimeWarp.Architecture.Features.Applications;

using TimeWarp.Architecture.Features.Bases;

internal partial class ApplicationState
{
  public class StartProcessingAction : BaseAction
  {
    public string ActionName { get; set; }
  }
}
