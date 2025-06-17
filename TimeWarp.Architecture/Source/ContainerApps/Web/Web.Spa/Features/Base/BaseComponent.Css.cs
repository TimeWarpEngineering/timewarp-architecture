namespace TimeWarp.Architecture.Features;

partial class BaseComponent
{
    [Parameter]
    public string? CssClass { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();

    protected string? BaseCssClass { get; set; }
    protected CssBuilder BaseCssBuilder { get; } = new();

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        BaseCssBuilder.AddClass(BaseCssClass);
        if (string.IsNullOrWhiteSpace(CssClass)) return;

        string[] classesToAdd = CssClass.Split(separator: ' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (string classToAdd in classesToAdd)
        {
            BaseCssBuilder.AddClass(classToAdd);
        }
    }

    // Method to get the class attribute from the Attributes dictionary
    protected string? GetClassFromAttributes()
    {
        return Attributes.TryGetValue(key: "class", out object? classValue) ?
            classValue as string :
            null;
    }
}
