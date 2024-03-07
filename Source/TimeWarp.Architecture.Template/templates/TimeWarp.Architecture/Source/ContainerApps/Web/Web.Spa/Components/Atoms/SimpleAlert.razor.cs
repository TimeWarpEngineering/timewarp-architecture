namespace TimeWarp.Architecture.Components;

public partial class SimpleAlert : ParentComponent
{
  [Parameter, EditorRequired] public string Title { get; set; } = default!;

  private readonly string BaseClasses =
    "bg-positive-100 border border-positive-400 text-positive-700 px-4 py-3 rounded relative";

  protected override void OnParametersSet()
  {
    base.OnParametersSet();
    if (Title is null) throw new ArgumentException("Title is required", nameof(Title));
  }

  private CssBuilder CssBuilder =>
    new CssBuilder(BaseClasses)
      .AddClassFromAttributes(Attributes);
}
