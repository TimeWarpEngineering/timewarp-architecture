#region Purpose
// TWA0023 exemption cases: opt-out attribute, skipped shapes, overrides, pragma, and role types.
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

public class Should_Enforce_Type_Stem_Identifiers_Exemptions
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Should_Enforce_Type_Stem_Identifiers_Exemptions>();

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
