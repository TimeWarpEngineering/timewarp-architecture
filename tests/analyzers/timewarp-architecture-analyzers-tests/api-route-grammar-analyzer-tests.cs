#region Purpose
// Tests for TWA0051: an [ApiRoute] template that breaks the route grammar (api/ first, lowercase
// kebab literal segments, whole-segment parameter tokens).
#endregion

// ReSharper disable InconsistentNaming
namespace ApiRouteGrammarAnalyzer_;

using Microsoft.CodeAnalysis.CSharp.Testing;
using TimeWarp.Architecture.Analyzers;
using TimeWarp.Architecture.Analyzers.Tests;

public class Should_Enforce_Route_Grammar
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Should_Enforce_Route_Grammar>();

  // ApiRouteAttribute is matched by FQN (HostedRouteDiscovery).
  private const string Stubs =
    """
    #region Purpose
    // Test stubs.
    #endregion
    namespace TimeWarp.Foundation.Features
    {
      public enum HttpVerb { Get, Post, Delete, Put, Patch, Head, Options }
      public sealed class ApiRouteAttribute : System.Attribute
      {
        public ApiRouteAttribute(string routeTemplate, HttpVerb httpVerb) { }
      }
      public sealed class ClientOnlyContractAttribute : System.Attribute
      {
        public ClientOnlyContractAttribute(string reason) { }
      }
    }
    namespace Other
    {
      public sealed class ApiRouteAttribute : System.Attribute
      {
        public ApiRouteAttribute(string routeTemplate) { }
      }
    }
    """;

  private static CSharpAnalyzerTest<ApiRouteGrammarAnalyzer, RoslynTestVerifier> Test(string source) =>
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

  private static string Feature(string attribute) =>
    $$"""
    #region Purpose
    // Test feature.
    #endregion
    namespace App.Contracts
    {
      using TimeWarp.Foundation.Features;

      public static class GetWidget
      {
        public const string Route = "api/Widgets";

        {{attribute}}
        public sealed class Query { }
      }
    }
    """;

  public static async Task Given_Kebab_Route_With_Tokens_IsClean() =>
    await Test(Feature("""[ApiRoute("api/admin/widget-groups/{WidgetGroupId:guid}/items/{Name}", HttpVerb.Get)]""")).RunAsync();

  public static async Task Given_Digits_In_Segment_IsClean() =>
    await Test(Feature("""[ApiRoute("api/v2/oauth2-tokens", HttpVerb.Get)]""")).RunAsync();

  public static async Task Given_PascalCase_Segment_Reports_TWA0051() =>
    await Test(Feature("""[ApiRoute({|TWA0051:"api/Widgets/{WidgetId:guid}"|}, HttpVerb.Get)]""")).RunAsync();

  public static async Task Given_Route_Outside_Api_Reports_TWA0051() =>
    await Test(Feature("""[ApiRoute({|TWA0051:"analytics/events"|}, HttpVerb.Post)]""")).RunAsync();

  public static async Task Given_Leading_Slash_Reports_TWA0051() =>
    await Test(Feature("""[ApiRoute({|TWA0051:"/api/widgets"|}, HttpVerb.Get)]""")).RunAsync();

  public static async Task Given_Bare_Api_Reports_TWA0051() =>
    await Test(Feature("""[ApiRoute({|TWA0051:"api"|}, HttpVerb.Get)]""")).RunAsync();

  public static async Task Given_Trailing_Slash_Reports_TWA0051() =>
    await Test(Feature("""[ApiRoute({|TWA0051:"api/widgets/"|}, HttpVerb.Get)]""")).RunAsync();

  public static async Task Given_Double_Hyphen_Reports_TWA0051() =>
    await Test(Feature("""[ApiRoute({|TWA0051:"api/widget--groups"|}, HttpVerb.Get)]""")).RunAsync();

  public static async Task Given_Underscore_Reports_TWA0051() =>
    await Test(Feature("""[ApiRoute({|TWA0051:"api/widget_groups"|}, HttpVerb.Get)]""")).RunAsync();

  public static async Task Given_Token_Inside_Literal_Segment_Reports_TWA0051() =>
    await Test(Feature("""[ApiRoute({|TWA0051:"api/widget{WidgetId}"|}, HttpVerb.Get)]""")).RunAsync();

  public static async Task Given_Query_String_Reports_TWA0051() =>
    await Test(Feature("""[ApiRoute({|TWA0051:"api/widgets?Top=10"|}, HttpVerb.Get)]""")).RunAsync();

  public static async Task Given_Const_Route_Reports_TWA0051_On_The_Argument() =>
    await Test(Feature("""[ApiRoute({|TWA0051:Route|}, HttpVerb.Get)]""")).RunAsync();

  public static async Task Given_ClientOnly_Contract_Still_Reports_TWA0051() =>
    await Test(Feature("""[ApiRoute({|TWA0051:"api/GetWidget"|}, HttpVerb.Get), ClientOnlyContract("Mock only.")]""")).RunAsync();

  public static async Task Given_Foreign_ApiRouteAttribute_IsClean() =>
    await Test(Feature("""[Other.ApiRoute("Widgets")]""")).RunAsync();

  public static Task FindGrammarBreak_Names_The_First_Break()
  {
    ApiRouteGrammarAnalyzer.FindGrammarBreak("api/agent-links/{LinkId:guid}/approve").ShouldBeNull();
    ApiRouteGrammarAnalyzer.FindGrammarBreak("Analytics/TrackEvent").ShouldBe("the first segment is not 'api'");
    ApiRouteGrammarAnalyzer.FindGrammarBreak("api/Feedback").ShouldBe("segment 'Feedback' is not lowercase kebab");
    ApiRouteGrammarAnalyzer.FindGrammarBreak("api//widgets").ShouldBe("it has an empty segment");
    ApiRouteGrammarAnalyzer.FindGrammarBreak("api/-widgets").ShouldBe("segment '-widgets' is not lowercase kebab");
    ApiRouteGrammarAnalyzer.FindGrammarBreak("api/widgets-").ShouldBe("segment 'widgets-' is not lowercase kebab");
    ApiRouteGrammarAnalyzer.FindGrammarBreak("api/{}").ShouldBe("segment '{}' is not lowercase kebab");
    ApiRouteGrammarAnalyzer.FindGrammarBreak("").ShouldBe("the template is empty");
    return Task.CompletedTask;
  }
}
