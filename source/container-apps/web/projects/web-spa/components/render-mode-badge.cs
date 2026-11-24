#region Purpose
// What the shell footer shows for the current Blazor render mode: an icon, a tooltip heading,
// a plain sentence, and the accessible label built from both.
#endregion

#region Design
// Keyed on RendererInfo.Name. Static, Server, and WebAssembly map to FluentUI Regular Size16
// icons (Document, Server, DeveloperBoard). Under InteractiveAuto a cold load moves through all
// three, so none of them is a warning and none carries a status colour. Any other name (a future
// renderer, a test renderer) has no icon and no sentence; the footer prints the name as text.
// Label is the aria-label and the tooltip's words, so assistive tech and hover read the same.
#endregion

namespace TimeWarp.Architecture.Components;

/// <summary>Footer render-mode slot content for one <c>RendererInfo.Name</c>.</summary>
/// <param name="Name">The renderer name, also the slot's <c>data-mode</c>.</param>
/// <param name="Icon">The icon, or null when the name is not a known mode.</param>
/// <param name="Description">One plain sentence about the mode, or null for an unknown name.</param>
public sealed record RenderModeBadge(string Name, Icon? Icon, string? Description)
{
  /// <summary>The tooltip heading: "Rendering: &lt;name&gt;".</summary>
  public string Heading => $"Rendering: {Name}";

  /// <summary>The accessible label: the heading and the sentence, or the name alone.</summary>
  public string Label => Description is null ? Name : $"{Heading}. {Description}";

  /// <summary>Badge for a <c>RendererInfo.Name</c>.</summary>
  public static RenderModeBadge For(string name) => name switch
  {
    "Static" => new(name, new Icons.Regular.Size16.Document(), "Pre-rendered page, not yet interactive."),
    "Server" => new(name, new Icons.Regular.Size16.Server(), "Running on the server over a live connection."),
    "WebAssembly" => new(name, new Icons.Regular.Size16.DeveloperBoard(), "Running in your browser."),
    _ => new(name, null, null),
  };
}
