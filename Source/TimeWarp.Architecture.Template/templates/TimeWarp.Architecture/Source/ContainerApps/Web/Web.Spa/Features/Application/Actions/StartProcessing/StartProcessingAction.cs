namespace TimeWarp.Architecture.Features.Applications;
internal partial class ApplicationState
{
  public record StartProcessingAction : BaseAction
  {
    public string ActionName { get; set; }
  }
}
