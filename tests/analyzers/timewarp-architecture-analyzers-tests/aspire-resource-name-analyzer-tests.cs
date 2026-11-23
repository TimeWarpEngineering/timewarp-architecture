#region Purpose
// Tests for TWA0007: AddDotnetProject resource names must be ServiceNames constant values.
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
        public void AddDotnetProject(string name, string path) { }
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
          builder.AddDotnetProject("web-server", "../app/app.csproj");
          builder.AddDotnetProject(TimeWarp.Foundation.Configuration.ServiceNames.ApiServiceName, "../app/app.csproj");
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
          builder.AddDotnetProject(WebName, "../app/app.csproj");
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
          builder.AddDotnetProject({|TWA0007:"webserver"|}, "../app/app.csproj");
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
          builder.AddDotnetProject(dynamicName, "../app/app.csproj");
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
          public void AddDotnetProject(string name, string path) { }
        }
      }
      public static class AppHost
      {
        public static void Configure(Aspire2.Builder builder)
        {
          builder.AddDotnetProject("anything-goes", "../app/app.csproj");
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
