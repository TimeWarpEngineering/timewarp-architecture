#region Purpose
// The operation catalog reports Group once per operation and omits it when the operation is ungrouped.
#endregion

namespace OperationCatalog_;

using System.Text.Json;
using System.Text.Json.Serialization;
using TimeWarp.Contracts;

public class Group_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Group_Given_>();

  private static readonly JsonSerializerOptions CatalogJson = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
  };

  public static Task A_Grouped_Operation_Should_Report_The_Group_Beside_Its_Name()
  {
    OperationAttribute attribute = new("get_symbol")
    {
      Description = "Resolve one symbol.",
      Group = "symbols"
    };
    OperationCatalog catalog = Catalog(attribute, "get_symbol {id}");

    string json = JsonSerializer.Serialize(catalog, CatalogJson);

    json.ShouldContain("\"name\":\"get_symbol\"");
    json.ShouldContain("\"group\":\"symbols\"");
    json.ShouldContain("\"pattern\":\"get_symbol {id}\"");
    json.ShouldNotContain("symbols get_symbol");
    json.ShouldNotContain("symbols/get_symbol");
    catalog.Operations[0].Name.ShouldBe("get_symbol");
    catalog.Operations[0].Group.ShouldBe("symbols");
    catalog.Operations[0].Bindings!.Cli!.Pattern.ShouldBe("get_symbol {id}");
    return Task.CompletedTask;
  }

  public static Task An_Omitted_Group_Should_Be_Absent_From_Catalog_Output()
  {
    OperationAttribute attribute = new("multi_query");
    string json = JsonSerializer.Serialize(Catalog(attribute, "multi_query"), CatalogJson);

    attribute.Group.ShouldBeNull();
    json.ShouldContain("\"name\":\"multi_query\"");
    json.ShouldNotContain("\"group\"");
    json.ShouldNotContain("symbols ");
    return Task.CompletedTask;
  }

  public static Task A_Blank_Group_Should_Be_Absent_From_Catalog_Output()
  {
    OperationAttribute attribute = new("find_references") { Group = "   " };
    OperationCatalog catalog = Catalog(attribute, "find_references {id}");
    string json = JsonSerializer.Serialize(catalog, CatalogJson);

    attribute.Group.ShouldBeNull();
    catalog.Operations[0].Group.ShouldBeNull();
    catalog.Operations[0].Name.ShouldBe("find_references");
    json.ShouldContain("\"name\":\"find_references\"");
    json.ShouldContain("\"pattern\":\"find_references {id}\"");
    json.ShouldNotContain("\"group\"");
    return Task.CompletedTask;
  }

  private static OperationCatalog Catalog(OperationAttribute attribute, string cliPattern) =>
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
          Kind = OperationKind.Query,
          Members = [],
          ReturnType = "Response",
          Bindings = new OperationBindings
          {
            Cli = new CliOperationBinding { Pattern = cliPattern }
          }
        }
      ]
    };
}
