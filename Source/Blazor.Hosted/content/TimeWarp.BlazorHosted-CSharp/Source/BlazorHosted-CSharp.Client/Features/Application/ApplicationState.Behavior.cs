namespace BlazorHosted_CSharp.Client.Features.Application
{
  using BlazorState;

  public partial class ApplicationState : State<ApplicationState>
  {
    public ApplicationState() { }

    protected ApplicationState(ApplicationState aState) : this()
    {
      Name = aState.Name;
    }

    public override object Clone() => new ApplicationState(this);

    protected override void Initialize() => Name = "BlazorHosted_CSharp";
  }
}