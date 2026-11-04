#region Purpose
// Shared harness (stubs, config, Test and Flag builders) for the TWA0025 page-local message bar tests.
#endregion

namespace TimeWarp.Architecture.Analyzers.Tests;

using Microsoft.CodeAnalysis.CSharp.Testing;

internal static class PageLocalMessageBarTestFixture
{
  internal const string BlazorWasmGlobalConfig =
    """
    is_global = true
    build_property.UsingMicrosoftNETSdkBlazorWebAssembly = true
    """;

  internal const string FluentUIStubs =
    """
    #region Purpose
    // Minimal FluentUI and Blazor stubs for TWA0025 tests.
    #endregion
    namespace Microsoft.FluentUI.AspNetCore.Components
    {
      public enum MessageBarIntent
      {
        Info = 0,
        Warning = 1,
        Error = 2,
        Success = 3
      }

      public class FluentMessageBar
      {
        public MessageBarIntent? Intent { get; set; }
        public string? Title { get; set; }
      }
    }

    namespace TimeWarp.Architecture.Components
    {
      public class MessageBar
      {
        public Microsoft.FluentUI.AspNetCore.Components.MessageBarIntent Intent { get; set; }
        public string? Title { get; set; }
      }
    }

    namespace TimeWarp.Architecture.Features.Notes
    {
      public class MessageBar
      {
        public Microsoft.FluentUI.AspNetCore.Components.MessageBarIntent Intent { get; set; }
      }
    }

    namespace Microsoft.AspNetCore.Components.Rendering
    {
      public class RenderTreeBuilder
      {
        public void OpenComponent<TComponent>(int sequence) { }
        public void AddComponentParameter(int sequence, string parameterName, object? parameterValue) { }
        public void AddAttribute(int sequence, string attributeName, object? attributeValue) { }
        public void CloseComponent() { }
      }
    }

    namespace Microsoft.AspNetCore.Components.CompilerServices
    {
      public static class RuntimeHelpers
      {
        public static T TypeCheck<T>(T value) => value;
      }
    }

    namespace TimeWarp.Architecture.Attributes
    {
      [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
      public sealed class PageLocalMessageBarAttribute : System.Attribute
      {
        public string Reason { get; }
        public PageLocalMessageBarAttribute(string reason) => Reason = reason;
      }
    }
    """;

  internal static CSharpAnalyzerTest<PageLocalMessageBarAnalyzer, RoslynTestVerifier> Test
  (
    string source,
    string sourcePath = "Feature_razor.g.cs",
    string? globalConfig = BlazorWasmGlobalConfig
  )
  {
    CSharpAnalyzerTest<PageLocalMessageBarAnalyzer, RoslynTestVerifier> test = new();
    if (globalConfig is not null)
    {
      test.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", globalConfig));
    }

    test.TestState.Sources.Add(("Stubs.cs", FluentUIStubs));
    test.TestState.Sources.Add((sourcePath, source));
    return test;
  }

  internal static DiagnosticResult Flag(string path, int line, int startColumn, int endColumn, string intentMember) =>
    new DiagnosticResult(id: "TWA0025", DiagnosticSeverity.Warning)
      .WithSpan(path, line, startColumn, line, endColumn)
      .WithArguments(intentMember);
}
