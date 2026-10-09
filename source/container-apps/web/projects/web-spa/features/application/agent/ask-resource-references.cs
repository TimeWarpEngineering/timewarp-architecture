#region Purpose
// Reads typed @ tokens out of a page_context JSON document.
#endregion

#region Design
// The page facts are already the catalog's parameter source. Credentials contribute
// @credential:{id}, a profile alias contributes @profile:{alias}, and site settings contribute
// @siteSettingsVersion:{n}. Pages with none of those yield an empty menu. Malformed JSON yields
// an empty menu rather than a throw on the UI thread.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>Turns page_context JSON into mention tokens.</summary>
public static class AskResourceReferences
{
  public static IReadOnlyList<AskResourceReference> FromPageContext(string? json)
  {
    if (string.IsNullOrWhiteSpace(json))
    {
      return [];
    }

    try
    {
      using var document = JsonDocument.Parse(json);
      JsonElement root = document.RootElement;
      List<AskResourceReference> references = [];
      AddCredentials(root, references);
      AddProfile(root, references);
      AddSiteSettings(root, references);
      return references;
    }
    catch (JsonException)
    {
      return [];
    }
  }

  private static void AddCredentials(JsonElement root, List<AskResourceReference> references)
  {
    if (!root.TryGetProperty("credentials", out JsonElement credentials)
      || credentials.ValueKind != JsonValueKind.Array)
    {
      return;
    }

    foreach (JsonElement credential in credentials.EnumerateArray())
    {
      if (!credential.TryGetProperty("id", out JsonElement idElement))
      {
        continue;
      }

      string? id = idElement.GetString();
      if (string.IsNullOrEmpty(id))
      {
        continue;
      }

      string? nickname = credential.TryGetProperty("nickname", out JsonElement nicknameElement)
        && nicknameElement.ValueKind == JsonValueKind.String
          ? nicknameElement.GetString()
          : null;
      string label = string.IsNullOrWhiteSpace(nickname) ? id : nickname;
      references.Add(new AskResourceReference(label, "@credential:" + id));
    }
  }

  private static void AddProfile(JsonElement root, List<AskResourceReference> references)
  {
    if (!root.TryGetProperty("profile", out JsonElement profile)
      || profile.ValueKind != JsonValueKind.Object
      || !profile.TryGetProperty("alias", out JsonElement aliasElement)
      || aliasElement.ValueKind != JsonValueKind.String)
    {
      return;
    }

    string? alias = aliasElement.GetString();
    if (string.IsNullOrWhiteSpace(alias))
    {
      return;
    }

    references.Add(new AskResourceReference(alias, "@profile:" + alias));
  }

  private static void AddSiteSettings(JsonElement root, List<AskResourceReference> references)
  {
    if (!root.TryGetProperty("siteSettings", out JsonElement settings)
      || settings.ValueKind != JsonValueKind.Object
      || !settings.TryGetProperty("version", out JsonElement versionElement))
    {
      return;
    }

    string? version = null;
    if (versionElement.ValueKind == JsonValueKind.Number)
    {
      version = versionElement.GetRawText();
    }
    else if (versionElement.ValueKind == JsonValueKind.String)
    {
      version = versionElement.GetString();
    }

    if (string.IsNullOrWhiteSpace(version))
    {
      return;
    }

    references.Add(new AskResourceReference("Site settings v" + version, "@siteSettingsVersion:" + version));
  }
}
