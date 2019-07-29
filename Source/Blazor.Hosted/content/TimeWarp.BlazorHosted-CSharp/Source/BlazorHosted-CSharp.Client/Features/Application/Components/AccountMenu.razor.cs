namespace BlazorHosted_CSharp.Client.Features.Application.Components
{
  using BlazorHosted_CSharp.Client.Features.Base.Components;

  public class AccountMenuBase : BaseComponent
  {
    protected void ButtonClick() => Show = !Show;
    protected bool Show { get; set; }
    protected string ShowCssClass => Show ? "show" : null;
  }
}
