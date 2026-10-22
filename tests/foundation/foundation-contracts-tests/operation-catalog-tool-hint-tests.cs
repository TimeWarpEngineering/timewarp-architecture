#region Purpose
// The operation catalog reports tool hints and omits a hint the request did not state.
#endregion

namespace OperationCatalog_;

using System.Text.Json;
using System.Text.Json.Serialization;
using TimeWarp.Contracts;

public class ToolHints_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<ToolHints_Given_>();

  private static readonly JsonSerializerOptions CatalogJson = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
  };

  public static Task Stated_Hints_Should_Appear_Beside_The_Name()
  {
    OperationAttribute attribute = new("notes.append")
    {
      Description = "Append a note to the log.",
      Title = "Append note"
    };
    OperationCatalog catalog = Catalog(attribute, new DestructiveAttribute(false), new OpenWorldAttribute(false));
    string json = JsonSerializer.Serialize(catalog, CatalogJson);

    json.ShouldContain("\"name\":\"notes.append\"");
    json.ShouldContain("\"title\":\"Append note\"");
    json.ShouldContain("\"destructiveHint\":false");
    json.ShouldContain("\"openWorldHint\":false");
    json.ShouldContain("\"pattern\":\"notes.append {id}\"");
    json.ShouldNotContain("Append note notes.append");
    catalog.Operations[0].Title.ShouldBe("Append note");
    catalog.Operations[0].DestructiveHint.ShouldBe(false);
    catalog.Operations[0].OpenWorldHint.ShouldBe(false);
    catalog.Operations[0].Bindings!.Cli!.Pattern.ShouldBe("notes.append {id}");
    return Task.CompletedTask;
  }

  public static Task Bare_Hint_Attributes_Should_Report_True()
  {
    OperationAttribute attribute = new("purge_cache") { Title = "Purge cache" };
    OperationCatalog catalog = Catalog(attribute, new DestructiveAttribute(), new OpenWorldAttribute());
    string json = JsonSerializer.Serialize(catalog, CatalogJson);

    catalog.Operations[0].DestructiveHint.ShouldBe(true);
    catalog.Operations[0].OpenWorldHint.ShouldBe(true);
    json.ShouldContain("\"destructiveHint\":true");
    json.ShouldContain("\"openWorldHint\":true");
    return Task.CompletedTask;
  }

  public static Task Omitted_Hints_Should_Be_Absent_From_Catalog_Output()
  {
    OperationAttribute attribute = new("multi_query");
    OperationCatalog catalog = Catalog(attribute, destructive: null, openWorld: null);
    string json = JsonSerializer.Serialize(catalog, CatalogJson);

    attribute.Title.ShouldBeNull();
    catalog.Operations[0].Title.ShouldBeNull();
    catalog.Operations[0].DestructiveHint.ShouldBeNull();
    catalog.Operations[0].OpenWorldHint.ShouldBeNull();
    json.ShouldContain("\"name\":\"multi_query\"");
    json.ShouldNotContain("\"title\"");
    json.ShouldNotContain("\"destructiveHint\"");
    json.ShouldNotContain("\"openWorldHint\"");
    return Task.CompletedTask;
  }

  public static Task A_Blank_Title_Should_Be_Absent_From_Catalog_Output()
  {
    OperationAttribute attribute = new("notes.append") { Title = "   " };
    OperationCatalog catalog = Catalog(attribute, new DestructiveAttribute(false), openWorld: null);
    string json = JsonSerializer.Serialize(catalog, CatalogJson);

    attribute.Title.ShouldBeNull();
    catalog.Operations[0].Title.ShouldBeNull();
    catalog.Operations[0].DestructiveHint.ShouldBe(false);
    catalog.Operations[0].OpenWorldHint.ShouldBeNull();
    json.ShouldContain("\"destructiveHint\":false");
    json.ShouldNotContain("\"title\"");
    json.ShouldNotContain("\"openWorldHint\"");
    return Task.CompletedTask;
  }

  private static OperationCatalog Catalog(
    OperationAttribute attribute,
    DestructiveAttribute? destructive,
    OpenWorldAttribute? openWorld) =>
    new()
    {
      Name = "roslynk",
      Operations =
      [
        new OperationDescriptor
        {
          Name = attribute.Name,
          Description = attribute.Description,
          Group = attribute.Group,
          Title = attribute.Title,
          DestructiveHint = destructive?.IsDestructive,
          OpenWorldHint = openWorld?.IsOpenWorld,
          Kind = OperationKind.Command,
          Members = [],
          ReturnType = "Response",
          Bindings = new OperationBindings
          {
            Cli = new CliOperationBinding { Pattern = attribute.Name + " {id}" }
          }
        }
      ]
    };
}
