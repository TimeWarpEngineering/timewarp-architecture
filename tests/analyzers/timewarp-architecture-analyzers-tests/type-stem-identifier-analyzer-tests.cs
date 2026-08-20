#region Purpose
// TWA0023: identifiers of named types must end with the type stem (interfaces drop a leading I).
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

public class Should_Enforce_Type_Stem_Identifiers
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Should_Enforce_Type_Stem_Identifiers>();

  private const string EnableTwa0023GlobalConfig =
    """
    is_global = true
    dotnet_diagnostic.TWA0023.severity = warning
    """;

  private const string AttributeStub =
    """
    namespace TimeWarp.Architecture.Attributes
    {
      [System.AttributeUsage(
        System.AttributeTargets.Field | System.AttributeTargets.Property | System.AttributeTargets.Parameter,
        AllowMultiple = false,
        Inherited = false)]
      public sealed class TypeStemIdentifierAttribute : System.Attribute
      {
        public string Reason { get; }
        public TypeStemIdentifierAttribute(string reason) => Reason = reason;
      }
    }
    """;

  private static CSharpAnalyzerTest<TypeStemIdentifierAnalyzer, RoslynTestVerifier> Test(string source)
  {
    CSharpAnalyzerTest<TypeStemIdentifierAnalyzer, RoslynTestVerifier> test = new();
    test.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", EnableTwa0023GlobalConfig));
    test.TestState.Sources.Add(("TypeStemIdentifierAttribute.cs", AttributeStub));
    test.TestState.Sources.Add(("Feature.cs", source));
    return test;
  }

  private static DiagnosticResult Flag(string path, int line, int startColumn, int endColumn, string identifier, string stem) =>
    new DiagnosticResult(id: "TWA0023", DiagnosticSeverity.Warning)
      .WithSpan(path, line, startColumn, line, endColumn)
      .WithArguments(identifier, stem);

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

  public static async Task Given_Reasoned_TypeStemIdentifier_Opt_Out_Is_Clean()
  {
    const string source =
      """
      #region Purpose
      // Vendor-prefix clip is attribute-only.
      #endregion
      using TimeWarp.Architecture.Attributes;

      public class TimeWarpTerminal { }

      public class Sample
      {
        [TypeStemIdentifier("global collision with System.Terminal")]
        private readonly TimeWarpTerminal Terminal;
      }
      """;

    await Test(source).RunAsync();
  }

  public static async Task Given_Empty_Or_Whitespace_Reason_Still_Flags()
  {
    const string source =
      """
      #region Purpose
      // Empty or whitespace reason is not an opt-out.
      #endregion
      using System.Net.Http;
      using TimeWarp.Architecture.Attributes;

      public class Sample
      {
        [TypeStemIdentifier("")]
        private readonly HttpClient EmptyReason;

        [TypeStemIdentifier("   ")]
        private readonly HttpClient WhitespaceReason;
      }
      """;

    CSharpAnalyzerTest<TypeStemIdentifierAnalyzer, RoslynTestVerifier> test = Test(source);
    test.ExpectedDiagnostics.Add(Flag("Feature.cs", 10, 31, 42, "EmptyReason", "HttpClient"));
    test.ExpectedDiagnostics.Add(Flag("Feature.cs", 13, 31, 47, "WhitespaceReason", "HttpClient"));
    await test.RunAsync();
  }

  public static async Task Given_Mismatch_Without_Attribute_Flags()
  {
    const string source =
      """
      #region Purpose
      // TimeWarpTerminal → Terminal is not inferred without the attribute.
      #endregion

      public class TimeWarpTerminal { }

      public class Sample
      {
        private readonly TimeWarpTerminal Terminal;
      }
      """;

    CSharpAnalyzerTest<TypeStemIdentifierAnalyzer, RoslynTestVerifier> test = Test(source);
    test.ExpectedDiagnostics.Add(Flag("Feature.cs", 9, 37, 45, "Terminal", "TimeWarpTerminal"));
    await test.RunAsync();
  }

  public static async Task Given_Foreach_Named_As_Type_Is_Clean()
  {
    const string source =
      """
      #region Purpose
      // Foreach variable uses the HttpClient stem.
      #endregion
      using System.Net.Http;

      public class Sample
      {
        public void Run(HttpClient[] list)
        {
          foreach (HttpClient httpClient in list) { }
        }
      }
      """;

    await Test(source).RunAsync();
  }

  public static async Task Given_Foreach_Mismatch_Flags()
  {
    const string source =
      """
      #region Purpose
      // Foreach variable c does not end with HttpClient.
      #endregion
      using System.Net.Http;

      public class Sample
      {
        public void Run(HttpClient[] list)
        {
          foreach (HttpClient c in list) { }
        }
      }
      """;

    CSharpAnalyzerTest<TypeStemIdentifierAnalyzer, RoslynTestVerifier> test = Test(source);
    test.ExpectedDiagnostics.Add(Flag("Feature.cs", 10, 25, 26, "c", "HttpClient"));
    await test.RunAsync();
  }

  public static async Task Given_Discard_Is_Clean()
  {
    const string source =
      """
      #region Purpose
      // Discard locals are out of scope.
      #endregion
      using System.Net.Http;

      public class Sample
      {
        public void Run(HttpClient[] list)
        {
          HttpClient _ = list[0];
        }
      }
      """;

    await Test(source).RunAsync();
  }

  public static async Task Given_Array_Is_Skipped()
  {
    const string source =
      """
      #region Purpose
      // Arrays are skip-set; name the meaning.
      #endregion

      public class Sample
      {
        public void Use(int[] counts) { }
      }
      """;

    await Test(source).RunAsync();
  }

  public static async Task Given_Override_And_Explicit_Interface_Members_Are_Not_Flagged()
  {
    const string source =
      """
      #region Purpose
      // Override and explicit interface names are not free; the base/interface still is.
      #endregion
      using System.Net.Http;

      public abstract class Base
      {
        public abstract HttpClient Client { get; }
        public abstract void Run(HttpClient client);
      }

      public class Derived : Base
      {
        public override HttpClient Client => null!;
        public override void Run(HttpClient client) { }
      }

      public interface IHasClient
      {
        HttpClient Client { get; }
      }

      public class Explicit : IHasClient
      {
        HttpClient IHasClient.Client => null!;
      }
      """;

    CSharpAnalyzerTest<TypeStemIdentifierAnalyzer, RoslynTestVerifier> test = Test(source);
    test.ExpectedDiagnostics.Add(Flag("Feature.cs", 8, 30, 36, "Client", "HttpClient"));
    test.ExpectedDiagnostics.Add(Flag("Feature.cs", 9, 39, 45, "client", "HttpClient"));
    test.ExpectedDiagnostics.Add(Flag("Feature.cs", 20, 14, 20, "Client", "HttpClient"));
    await test.RunAsync();
  }

  public static async Task Given_Pragma_Disable_Is_Clean()
  {
    const string source =
      """
      #region Purpose
      // Standard Roslyn valve for locals and other sites without the attribute.
      #endregion
      using System.Net.Http;

      public class Sample
      {
      #pragma warning disable TWA0023
        private readonly HttpClient CatalogClient;
      #pragma warning restore TWA0023
      }
      """;

    await Test(source).RunAsync();
  }

  public static async Task Given_IHttpClientFactory_Factory_Flags()
  {
    const string source =
      """
      #region Purpose
      // IHttpClientFactory is not in the skip set; factory is a true positive.
      #endregion

      public interface IHttpClientFactory { }

      public class Sample
      {
        public void Use(IHttpClientFactory factory) { }
      }
      """;

    CSharpAnalyzerTest<TypeStemIdentifierAnalyzer, RoslynTestVerifier> test = Test(source);
    test.ExpectedDiagnostics.Add(Flag("Feature.cs", 9, 38, 45, "factory", "HttpClientFactory"));
    await test.RunAsync();
  }

  public static async Task Given_Named_Role_Types_Are_Not_Skipped()
  {
    const string source =
      """
      #region Purpose
      // True positives: these types name the role and are not in the skip set.
      #endregion
      using System;
      using System.Net;
      using System.Threading;

      namespace Microsoft.Extensions.Logging
      {
        public interface ILogger<TCategoryName> { }
      }

      public class Widget { }

      public class Sample
      {
        public void Use(
          Microsoft.Extensions.Logging.ILogger<Widget> log,
          DateTime dt,
          Guid id,
          TimeSpan ts,
          CancellationToken ct,
          HttpStatusCode code)
        { }
      }
      """;

    CSharpAnalyzerTest<TypeStemIdentifierAnalyzer, RoslynTestVerifier> test = Test(source);
    test.ExpectedDiagnostics.Add(Flag("Feature.cs", 18, 50, 53, "log", "Logger"));
    test.ExpectedDiagnostics.Add(Flag("Feature.cs", 19, 14, 16, "dt", "DateTime"));
    test.ExpectedDiagnostics.Add(Flag("Feature.cs", 20, 10, 12, "id", "Guid"));
    test.ExpectedDiagnostics.Add(Flag("Feature.cs", 21, 14, 16, "ts", "TimeSpan"));
    test.ExpectedDiagnostics.Add(Flag("Feature.cs", 22, 23, 25, "ct", "CancellationToken"));
    test.ExpectedDiagnostics.Add(Flag("Feature.cs", 23, 20, 24, "code", "HttpStatusCode"));
    await test.RunAsync();
  }

  public static async Task Given_Enum_Members_Are_Skipped()
  {
    const string source =
      """
      #region Purpose
      // Enum members are named values, not a role of the enum type.
      #endregion

      public enum Color
      {
        Red,
        Blue
      }
      """;

    await Test(source).RunAsync();
  }
}
