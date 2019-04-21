namespace BlazorHosted_CSharp.Client.Features.Application
{
  using BlazorState;

  public partial class ApplicationState
  {
    public string Name { get; private set; }

    public string Version => GetType().Assembly.GetName().Version.ToString();
  }
}