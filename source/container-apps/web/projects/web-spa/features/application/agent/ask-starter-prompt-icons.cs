#region Purpose
// Maps a starter prompt's configured icon name to the FluentUI icon Ask renders (task 348).
#endregion

#region Design
// Configuration carries a name, not a type. The map is a fixed, curated set so the icon package is
// never searched by reflection (trimming would drop unreferenced icons in WebAssembly). Names are
// case-insensitive; a blank or unknown name gets DefaultName (Sparkle) rather than no icon, so a
// typo in XAI:StarterPrompts still renders a card. Add a case here to offer another icon.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>Icon names a starter prompt may use.</summary>
public static class AskStarterPromptIcons
{
  /// <summary>Used for a blank or unknown name.</summary>
  public const string DefaultName = "Sparkle";

  /// <summary>Every supported name, in the casing the template documents.</summary>
  public static IReadOnlyList<string> Names { get; } =
  [
    DefaultName,
    "Navigation",
    "Question",
    "Flash",
    "ShieldCheckmark",
    "Search",
    "Lightbulb",
    "Document",
    "Edit",
    "Info",
  ];

  /// <summary>The 20px regular icon for <paramref name="name"/>, or the default icon.</summary>
  public static Icon For(string? name) => name?.Trim().ToUpperInvariant() switch
  {
    "NAVIGATION" => new Icons.Regular.Size20.Navigation(),
    "QUESTION" => new Icons.Regular.Size20.Question(),
    "FLASH" => new Icons.Regular.Size20.Flash(),
    "SHIELDCHECKMARK" => new Icons.Regular.Size20.ShieldCheckmark(),
    "SEARCH" => new Icons.Regular.Size20.Search(),
    "LIGHTBULB" => new Icons.Regular.Size20.Lightbulb(),
    "DOCUMENT" => new Icons.Regular.Size20.Document(),
    "EDIT" => new Icons.Regular.Size20.Edit(),
    "INFO" => new Icons.Regular.Size20.Info(),
    _ => new Icons.Regular.Size20.Sparkle(),
  };
}
