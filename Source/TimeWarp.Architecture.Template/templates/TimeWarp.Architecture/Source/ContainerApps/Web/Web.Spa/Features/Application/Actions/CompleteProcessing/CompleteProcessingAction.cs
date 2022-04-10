namespace TimeWarp.Architecture.Features.Applications;
internal partial class ApplicationState
{
  internal record CompleteProcessingAction : BaseAction
  {
    public string ActionName { get; set; }
  }
}
