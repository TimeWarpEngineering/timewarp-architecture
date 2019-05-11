namespace BlazorHosted_CSharp.Client.Features.Application
{
  using BlazorState;

  internal partial class ApplicationState : State<ApplicationState>
  {
    public string Name { get; private set; }

    public string Version => GetType().Assembly.GetName().Version.ToString();
    
    public ApplicationState() { }
    
    protected override void Initialize() => Name = "BlazorHosted_CSharp";
  }
}