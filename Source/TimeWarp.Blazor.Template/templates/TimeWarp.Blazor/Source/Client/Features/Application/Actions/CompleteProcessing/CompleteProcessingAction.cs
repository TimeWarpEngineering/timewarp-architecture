namespace TimeWarp.Blazor.Features.Applications
{
  using TimeWarp.Blazor.Features.Bases;

  internal partial class ApplicationState
  {
    internal class CompleteProcessingAction : BaseAction
    {
      public string ActionName { get; set; }
    }
  }
}
