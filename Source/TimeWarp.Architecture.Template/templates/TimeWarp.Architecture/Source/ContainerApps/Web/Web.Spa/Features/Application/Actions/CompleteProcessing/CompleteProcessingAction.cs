namespace TimeWarp.Architecture.Features.Applications;

using TimeWarp.Architecture.Features.Bases;

internal partial class ApplicationState
{
  internal class CompleteProcessingAction : BaseAction
  {
    public string ActionName { get; set; }
  }
}
