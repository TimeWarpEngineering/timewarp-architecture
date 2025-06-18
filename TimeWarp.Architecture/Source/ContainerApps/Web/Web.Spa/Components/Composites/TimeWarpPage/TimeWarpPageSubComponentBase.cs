namespace TimeWarp.Architecture.Components.Composites.TimeWarpPage;

public abstract class TimeWarpPageSubComponentBase : BaseComponent
{
  [CascadingParameter] public TimeWarpPage? TimeWarpPage { get; set; }

  protected override void OnInitialized()
  {
    base.OnInitialized();
    BaseCssBuilder.AddClass(Id);
    if (TimeWarpPage?.ShowPlaceholders == true)
    {
      BaseCssBuilder.AddClass("placeholder");
    }
  }
  protected override void OnParametersSet()
  {
    base.OnParametersSet();
    Guard.Against.Null(TimeWarpPage, nameof(TimeWarpPage));
  }
}
