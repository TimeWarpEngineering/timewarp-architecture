namespace BlazorHosted_CSharp.Client.Features.Application.Components
{
  using BlazorHosted_CSharp.Client.Features.Base.Components;

  public class FooterModel: BaseComponent
  {
    protected string Version => ApplicationState.Version;
  }
}
