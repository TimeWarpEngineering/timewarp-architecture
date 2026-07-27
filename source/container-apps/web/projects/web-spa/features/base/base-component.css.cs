#region Purpose
// BaseComponent partial: merges component-owned CSS classes with consumer-supplied ones.
#endregion

#region Design
// A component sets its structural classes via BaseCssClass/BaseCssBuilder while consumers append
// through the CssClass parameter — the component keeps final say over class composition instead
// of consumers overwriting the class attribute wholesale.
// CssClass is split on whitespace so one parameter can carry several classes.
// Unmatched-attribute capture (IAttributeComponent) lets wrapper components forward a consumer's
// "class" and other attributes to the element they render.
#endregion

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
