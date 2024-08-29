namespace TimeWarp.Architecture.Features;

partial class BaseComponent
{
    [Parameter]
    public string? Class { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();

    protected string? BaseClass { get; set; }
    protected CssBuilder BaseCssBuilder { get; } = new();

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        BaseCssBuilder.AddClass(BaseClass);
        if (string.IsNullOrWhiteSpace(Class)) return;

        string[] classesToAdd = Class.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (string classToAdd in classesToAdd)
        {
            BaseCssBuilder.AddClass(classToAdd);
        }
    }

    // Method to get the class attribute from the Attributes dictionary
    protected string? GetClassFromAttributes()
    {
        return Attributes.TryGetValue("class", out object? classValue) ?
            classValue as string :
            null;
    }
}
