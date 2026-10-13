#region Purpose
// Tests for TWA0033 ([ReferenceableResource] contract shape) and TWA0034 (duplicate @ prefix).
#endregion

// ReSharper disable InconsistentNaming
namespace ReferenceableResourceAnalyzer_;

using Microsoft.CodeAnalysis.CSharp.Testing;
using TimeWarp.Architecture.Analyzers;
using TimeWarp.Architecture.Analyzers.Tests;

public class Should_Enforce_Referenceable_Resource_Shape
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Should_Enforce_Referenceable_Resource_Shape>();

  // Attributes match by simple name, so these stubs stand in for the attributes package and the
  // generated [TypedId] attribute.
  private const string Stubs =
    """
    #region Purpose
    // Test stubs.
    #endregion
    namespace TimeWarp.Architecture.Attributes
    {
      public sealed class ApiEndpointAttribute : System.Attribute { }

      public sealed class EndpointAuthorizeAttribute : System.Attribute
      {
        public string? Policy { get; set; }
        public string? Roles { get; set; }
      }

      public interface IApiRequest { }

      public sealed class ReferenceableResourceAttribute : System.Attribute
      {
        public ReferenceableResourceAttribute(string prefix, string displayName) { }
        public string IdProperty { get; set; } = "";
        public string LabelProperty { get; set; } = "";
        public string ItemsProperty { get; set; } = "Items";
      }

      public sealed class TypedIdAttribute : System.Attribute { }
    }
    namespace App.Ids
    {
      [TimeWarp.Architecture.Attributes.TypedId]
      public readonly partial record struct WidgetId(System.Guid Value);
    }
    """;

  private static CSharpAnalyzerTest<ReferenceableResourceAnalyzer, RoslynTestVerifier> Test(string source) =>
    new()
    {
      TestState =
      {
        Sources =
        {
          ("Stubs.cs", Stubs),
          ("Feature.cs", source)
        }
      }
    };

  private static string Contract
  (
    string attribute,
    string name = "ListWidgets",
    string search = "public string? Search { get; set; }",
    string id = "System.Guid",
    string label = "string",
    string authorize = "[EndpointAuthorize(Policy = \"widgets.read\")]",
    string queryMembers = "",
    string queryBase = " : IApiRequest",
    string response = "public sealed class Response"
  ) =>
    $$"""
    [ApiEndpoint]
    {{authorize}}
    {{attribute}}
    public static class {{name}}
    {
      public sealed class Query{{queryBase}}
      {
        {{search}}
        {{queryMembers}}
      }

      {{response}}
      {
        public System.Collections.Generic.IReadOnlyList<Item> Items { get; } = new Item[0];
      }

      public sealed class Item
      {
        public {{id}} WidgetId { get; }
        public {{label}} Name { get; } = default!;
      }
    }
    """;

  private static string Feature(params string[] contracts) =>
    $$"""
    #region Purpose
    // Test feature.
    #endregion
    namespace App.Contracts
    {
      using TimeWarp.Architecture.Attributes;

      {{string.Join("\n", contracts)}}
    }
    """;

  private const string Marker =
    "[ReferenceableResource(\"widget\", \"Widgets\", IdProperty = \"WidgetId\", LabelProperty = \"Name\")]";

  private static string Flagged(string id, string prefix = "widget") =>
    $"[{{|{id}:ReferenceableResource(\"{prefix}\", \"Widgets\", IdProperty = \"WidgetId\", LabelProperty = \"Name\")|}}]";

  public static async Task Given_Guid_Id_IsClean()
  {
    await Test(Feature(Contract(Marker))).RunAsync();
  }

  public static async Task Given_TypedId_IsClean()
  {
    await Test(Feature(Contract(Marker, id: "App.Ids.WidgetId"))).RunAsync();
  }

  public static async Task Given_No_Search_Flags_TWA0033()
  {
    await Test(Feature(Contract(Flagged("TWA0033"), search: ""))).RunAsync();
  }

  public static async Task Given_ReadOnly_Search_Flags_TWA0033()
  {
    await Test(Feature(Contract(Flagged("TWA0033"), search: "public string? Search { get; }"))).RunAsync();
  }

  public static async Task Given_String_Id_Flags_TWA0033()
  {
    await Test(Feature(Contract(Flagged("TWA0033"), id: "string"))).RunAsync();
  }

  public static async Task Given_NonString_Label_Flags_TWA0033()
  {
    await Test(Feature(Contract(Flagged("TWA0033"), label: "int"))).RunAsync();
  }

  public static async Task Given_Uppercase_Prefix_Flags_TWA0033()
  {
    await Test(Feature(Contract(Flagged("TWA0033", prefix: "Widget")))).RunAsync();
  }

  public static async Task Given_No_EndpointAuthorize_Flags_TWA0033()
  {
    await Test(Feature(Contract(Flagged("TWA0033"), authorize: ""))).RunAsync();
  }

  public static async Task Given_Roles_Only_EndpointAuthorize_Flags_TWA0033()
  {
    await Test(Feature(Contract(Flagged("TWA0033"), authorize: "[EndpointAuthorize(Roles = \"Administrator\")]"))).RunAsync();
  }

  public static async Task Given_Struct_Response_Flags_TWA0033()
  {
    await Test(Feature(Contract(Flagged("TWA0033"), response: "public record struct Response()"))).RunAsync();
  }

  public static async Task Given_Query_Extra_Required_Member_Flags_TWA0033()
  {
    await Test(Feature(Contract(Flagged("TWA0033"), queryMembers: "public required int Page { get; set; }"))).RunAsync();
  }

  public static async Task Given_Query_Not_IApiRequest_Flags_TWA0033()
  {
    await Test(Feature(Contract(Flagged("TWA0033"), queryBase: ""))).RunAsync();
  }

  public static async Task Given_Query_Without_Parameterless_Constructor_Flags_TWA0033()
  {
    await Test(Feature(Contract(Flagged("TWA0033"), queryMembers: "public Query(int page) { }"))).RunAsync();
  }

  public static async Task Given_Internal_Response_Flags_TWA0033()
  {
    await Test(Feature(Contract(Flagged("TWA0033"), response: "internal sealed class Response"))).RunAsync();
  }

  public static async Task Given_Duplicate_Prefix_Flags_TWA0034_On_Both()
  {
    await Test(Feature(
      Contract(Flagged("TWA0034"), name: "ListWidgets"),
      Contract(Flagged("TWA0034"), name: "SearchWidgets"))).RunAsync();
  }
}
