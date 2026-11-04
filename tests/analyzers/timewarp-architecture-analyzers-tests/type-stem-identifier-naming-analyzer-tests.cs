#region Purpose
// TWA0023 naming cases: descriptor, clean names, and flagged mismatches.
#endregion

#region Design
// The rule ships isEnabledByDefault: false, so the harness must enable it via globalconfig or
// every case is a no-op. Descriptor assertions (Id / IsEnabledByDefault / DefaultSeverity) are
// the load-bearing default-off proof even if Microsoft.CodeAnalysis.Testing enables analyzers.
// TypeStemIdentifierAttribute is stubbed in test source and matched by simple name — this project
// does not ProjectReference Architecture.Attributes.
#endregion

namespace TimeWarp.Architecture.Analyzers.Tests;

using Microsoft.CodeAnalysis.CSharp.Testing;
using static TimeWarp.Architecture.Analyzers.Tests.TypeStemIdentifierTestFixture;

public class Should_Enforce_Type_Stem_Identifiers_Naming
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Should_Enforce_Type_Stem_Identifiers_Naming>();

  public static Task Given_Descriptor_Is_Default_Off_Warning()
  {
    TypeStemIdentifierAnalyzer typeStemIdentifierAnalyzer = new();
    DiagnosticDescriptor diagnosticDescriptor = typeStemIdentifierAnalyzer.SupportedDiagnostics[0];
    diagnosticDescriptor.Id.ShouldBe("TWA0023");
    diagnosticDescriptor.IsEnabledByDefault.ShouldBeFalse();
    diagnosticDescriptor.DefaultSeverity.ShouldBe(DiagnosticSeverity.Warning);
    diagnosticDescriptor.Category.ShouldBe("Naming");
    return Task.CompletedTask;
  }

  public static async Task Given_Field_Named_As_Type_Is_Clean()
  {
    const string source =
      """
      #region Purpose
      // Exact type-stem field.
      #endregion
      using System.Net.Http;

      public class Sample
      {
        private readonly HttpClient HttpClient;
      }
      """;

    await Test(source).RunAsync();
  }

  public static async Task Given_Parameter_Named_As_Type_Is_Clean()
  {
    const string source =
      """
      #region Purpose
      // Exact type-stem parameter.
      #endregion
      using System.Net.Http;

      public class Sample
      {
        public void Process(HttpClient httpClient) { }
      }
      """;

    await Test(source).RunAsync();
  }

  public static async Task Given_Local_Named_As_Type_Is_Clean()
  {
    const string source =
      """
      #region Purpose
      // Exact type-stem local.
      #endregion
      using System.Net.Http;

      public class Sample
      {
        public void Run()
        {
          HttpClient httpClient = new HttpClient();
        }
      }
      """;

    await Test(source).RunAsync();
  }

  public static async Task Given_Shortened_Type_Name_Flags()
  {
    const string source =
      """
      #region Purpose
      // Discovery is not the OriginHomeDiscovery stem.
      #endregion

      public class OriginHomeDiscovery { }

      public class Sample
      {
        private readonly OriginHomeDiscovery Discovery;
      }
      """;

    CSharpAnalyzerTest<TypeStemIdentifierAnalyzer, RoslynTestVerifier> test = Test(source);
    test.ExpectedDiagnostics.Add(Flag("Feature.cs", 9, 40, 49, "Discovery", "OriginHomeDiscovery"));
    await test.RunAsync();
  }

  public static async Task Given_Qualifier_Dropping_Type_Head_Flags()
  {
    const string source =
      """
      #region Purpose
      // CatalogClient drops the HttpClient head.
      #endregion
      using System.Net.Http;

      public class Sample
      {
        private readonly HttpClient CatalogClient;
      }
      """;

    CSharpAnalyzerTest<TypeStemIdentifierAnalyzer, RoslynTestVerifier> test = Test(source);
    test.ExpectedDiagnostics.Add(Flag("Feature.cs", 8, 31, 44, "CatalogClient", "HttpClient"));
    await test.RunAsync();
  }

  public static async Task Given_Qualified_Type_Head_Is_Clean()
  {
    const string source =
      """
      #region Purpose
      // Two of the same type keep HttpClient as the head.
      #endregion
      using System.Net.Http;

      public class Sample
      {
        private readonly HttpClient CatalogHttpClient;
        private readonly HttpClient BillingHttpClient;
      }
      """;

    await Test(source).RunAsync();
  }

  public static async Task Given_ReceivingPerson_Qualifier_Is_Clean()
  {
    const string source =
      """
      #region Purpose
      // Person stem with a qualifying prefix.
      #endregion

      public class Person { }

      public class Sample
      {
        private readonly Person ReceivingPerson;
      }
      """;

    await Test(source).RunAsync();
  }

  public static async Task Given_Interface_I_Strip_Exact_Stem_Is_Clean()
  {
    const string source =
      """
      #region Purpose
      // IFileSystem strips to FileSystem.
      #endregion

      public interface IFileSystem { }

      public class Sample
      {
        private readonly IFileSystem FileSystem;
      }
      """;

    await Test(source).RunAsync();
  }

  public static async Task Given_Interface_I_Strip_Qualified_Stem_Is_Clean()
  {
    const string source =
      """
      #region Purpose
      // Two file systems qualify the FileSystem stem.
      #endregion

      public interface IFileSystem { }

      public class Sample
      {
        private readonly IFileSystem LocalFileSystem;
      }
      """;

    await Test(source).RunAsync();
  }

  public static async Task Given_Interface_I_Strip_Mismatch_Flags()
  {
    const string source =
      """
      #region Purpose
      // fs does not end with FileSystem.
      #endregion

      public interface IFileSystem { }

      public class Sample
      {
        public void Use(IFileSystem fs) { }
      }
      """;

    CSharpAnalyzerTest<TypeStemIdentifierAnalyzer, RoslynTestVerifier> test = Test(source);
    test.ExpectedDiagnostics.Add(Flag("Feature.cs", 9, 31, 33, "fs", "FileSystem"));
    await test.RunAsync();
  }

  public static async Task Given_ILogger_Stem_Is_Logger()
  {
    const string source =
      """
      #region Purpose
      // ILogger<T> stem is Logger after I-strip; not in the skip set.
      #endregion
      namespace Microsoft.Extensions.Logging
      {
        public interface ILogger<TCategoryName> { }
      }

      public class Widget { }

      public class Sample
      {
        public void Use(
          Microsoft.Extensions.Logging.ILogger<Widget> logger,
          Microsoft.Extensions.Logging.ILogger<Widget> catalogLogger)
        { }
      }
      """;

    await Test(source).RunAsync();
  }

  public static async Task Given_Primitives_Are_Skipped()
  {
    const string source =
      """
      #region Purpose
      // string / int / bool are SpecialType skips — name the meaning, not the type.
      #endregion

      public class Sample
      {
        public void Use(string title, int count, bool ready) { }
      }
      """;

    await Test(source).RunAsync();
  }

  public static async Task Given_Untyped_Boxes_Are_Skipped()
  {
    const string source =
      """
      #region Purpose
      // List / Dictionary are untyped boxes.
      #endregion
      using System.Collections.Generic;
      using System.Net.Http;

      public class Sample
      {
        public void Use(List<HttpClient> pending, Dictionary<string, int> map) { }
      }
      """;

    await Test(source).RunAsync();
  }

  public static async Task Given_IEnumerable_Of_Task_Is_Skipped()
  {
    const string source =
      """
      #region Purpose
      // IEnumerable`1 and Task are both skip-set boxes.
      #endregion
      using System.Collections.Generic;
      using System.Threading.Tasks;

      public class Sample
      {
        public void Use(IEnumerable<Task> items) { }
      }
      """;

    await Test(source).RunAsync();
  }
}
