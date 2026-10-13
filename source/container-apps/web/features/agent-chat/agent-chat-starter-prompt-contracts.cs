#region Purpose
// The Ask panel's empty-state starter prompts (task 348): the model, the template defaults, and the
// rule that turns a configured list into the list Ask shows.
#endregion

#region Design
// A prompt is a value record: an icon name, a title, a one-line subtitle, and the text Ask sends
// on click. The icon is a name, not a FluentUI type: contracts and appsettings cannot carry a
// component. The SPA maps the name (AskStarterPromptIcons); an unknown name gets the default icon.
// XAI:StarterPrompts replaces the whole list. Normalize drops entries without a title or prompt,
// trims every field, and keeps at most MaxCount, so a long list cannot push the composer off the
// panel. When nothing usable is left (unset, empty, or all blank) Ask shows Defaults.
// The defaults are the template's primary uses of Ask: find a page, explain this page, run an
// action, and check what needs approval. They describe what every generated app has, not a
// product catalog the template does not ship.
#endregion

namespace TimeWarp.Architecture.Features.AgentChats;

/// <summary>One empty-state starter prompt in Ask.</summary>
public sealed record AgentChatStarterPrompt
{
  /// <summary>Icon name (see AskStarterPromptIcons in the SPA). Blank or unknown uses the default icon.</summary>
  public string? Icon { get; set; }

  /// <summary>Short card title, for example "Explain this page".</summary>
  public string Title { get; set; } = string.Empty;

  /// <summary>One-line description under the title.</summary>
  public string? Subtitle { get; set; }

  /// <summary>Text sent to the model when the card is clicked.</summary>
  public string Prompt { get; set; } = string.Empty;
}

/// <summary>Template defaults and normalization for <see cref="AgentChatStarterPrompt"/>.</summary>
public static class AgentChatStarterPrompts
{
  /// <summary>Most prompts Ask shows. Extra configured entries are dropped.</summary>
  public const int MaxCount = 6;

  /// <summary>Template defaults, shown when XAI:StarterPrompts has no usable entry.</summary>
  public static IReadOnlyList<AgentChatStarterPrompt> Defaults { get; } =
  [
    new()
    {
      Icon = "Navigation",
      Title = "Find a page",
      Subtitle = "Take me where I need to go",
      Prompt = "Which pages can I open in this app, and what is each one for? Offer to take me to the one I need."
    },
    new()
    {
      Icon = "Question",
      Title = "Explain this page",
      Subtitle = "What it shows and what I can do here",
      Prompt = "Explain this page: what it shows and which actions I can run here."
    },
    new()
    {
      Icon = "Flash",
      Title = "Run an action",
      Subtitle = "Do a task on this page for me",
      Prompt = "List the actions you can run for me on this page, then ask me which one I want."
    },
    new()
    {
      Icon = "ShieldCheckmark",
      Title = "Check approvals",
      Subtitle = "What needs my sign-off before it runs",
      Prompt = "Which of the actions you can run here would need my approval first, and why?"
    },
  ];

  /// <summary>The usable configured prompts, trimmed and capped; <see cref="Defaults"/> when none are usable.</summary>
  public static IReadOnlyList<AgentChatStarterPrompt> Normalize(IEnumerable<AgentChatStarterPrompt?>? configured)
  {
    List<AgentChatStarterPrompt> prompts = [];
    foreach (AgentChatStarterPrompt? prompt in configured ?? [])
    {
      if (prompt is null || string.IsNullOrWhiteSpace(prompt.Title) || string.IsNullOrWhiteSpace(prompt.Prompt))
      {
        continue;
      }

      prompts.Add
      (
        new AgentChatStarterPrompt
        {
          Icon = string.IsNullOrWhiteSpace(prompt.Icon) ? null : prompt.Icon.Trim(),
          Title = prompt.Title.Trim(),
          Subtitle = string.IsNullOrWhiteSpace(prompt.Subtitle) ? null : prompt.Subtitle.Trim(),
          Prompt = prompt.Prompt.Trim()
        }
      );
      if (prompts.Count == MaxCount)
      {
        break;
      }
    }

    return prompts.Count == 0 ? Defaults : prompts;
  }
}
