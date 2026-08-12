#region Purpose
// Tests for TWA0007: AddProject resource names must be ServiceNames constant values.
#endregion

// ReSharper disable InconsistentNaming
namespace AspireResourceNameAnalyzer_;

using Microsoft.CodeAnalysis.CSharp.Testing;
using TimeWarp.Architecture.Analyzers;
using TimeWarp.Architecture.Analyzers.Tests;

public class Should_Enforce_Aspire_Resource_Names
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Should_Enforce_Aspire_Resource_Names>();

  private const string Stubs =
    """
    #region Purpose
    // Test stubs.
    #endregion
    namespace TimeWarp.Foundation.Configuration
    {
      public static class ServiceNames
      {
        public const string WebServiceName = "web-server";
        public const string ApiServiceName = "api-server";
      }
    }
    namespace Aspire
    {
      public class Builder
      {
        public void AddProject<T>(string name) { }
      }
    }
    """;

  private static CSharpAnalyzerTest<AspireResourceNameAnalyzer, RoslynTestVerifier> Test(string source) =>
    new() { TestState = { Sources = { ("Stubs.cs", Stubs), ("AppHost.cs", source) } } };

  public static async Task Given_Name_Matching_ServiceNames_IsClean()
  {
    const string source =
      """
      #region Purpose
      // Test app host.
      #endregion
      public static class AppHost
      {
        public static void Configure(Aspire.Builder builder)
        {
          builder.AddProject<object>("web-server");
          builder.AddProject<object>(TimeWarp.Foundation.Configuration.ServiceNames.ApiServiceName);
        }
      }
      """;

    await Test(source).RunAsync();
  }

  public static async Task Given_Aliased_Constant_IsClean()
  {
    // The AppHost pattern: local constants aliasing ServiceNames evaluate to allowed values.
    const string source =
      """
      #region Purpose
      // Test app host.
      #endregion
      public static class AppHost
      {
        private const string WebName = TimeWarp.Foundation.Configuration.ServiceNames.WebServiceName;

        public static void Configure(Aspire.Builder builder)
        {
          builder.AddProject<object>(WebName);
        }
      }
      """;

    await Test(source).RunAsync();
  }

  public static async Task Given_Unknown_Name_Flags_TWA0007()
  {
    const string source =
      """
      #region Purpose
      // Test app host.
      #endregion
      public static class AppHost
      {
        public static void Configure(Aspire.Builder builder)
        {
          builder.AddProject<object>({|TWA0007:"webserver"|});
        }
      }
      """;

    await Test(source).RunAsync();
  }

  public static async Task Given_NonConstant_Name_IsSkipped()
  {
    const string source =
      """
      #region Purpose
      // Test app host.
      #endregion
      public static class AppHost
      {
        public static void Configure(Aspire.Builder builder, string dynamicName)
        {
          builder.AddProject<object>(dynamicName);
        }
      }
      """;

    await Test(source).RunAsync();
  }

  public static async Task Given_No_ServiceNames_Type_IsSilent()
  {
    const string source =
      """
      #region Purpose
      // Test app host without ServiceNames in scope.
      #endregion
      namespace Aspire2
      {
        public class Builder
        {
          public void AddProject<T>(string name) { }
        }
      }
      public static class AppHost
      {
        public static void Configure(Aspire2.Builder builder)
        {
          builder.AddProject<object>("anything-goes");
        }
      }
      """;

    var test = new CSharpAnalyzerTest<AspireResourceNameAnalyzer, RoslynTestVerifier>
    {
      TestState = { Sources = { ("AppHost.cs", source) } }
    };

    await test.RunAsync();
  }
}
