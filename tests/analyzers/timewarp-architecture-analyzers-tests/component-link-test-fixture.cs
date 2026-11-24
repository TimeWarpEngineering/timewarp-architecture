#region Purpose
// Shared harness (stubs, config, Test and Flag builders) for the TWA0050 component link tests.
#endregion

#region Design
// The stubs mirror the render-tree calls Razor emits for markup: OpenElement / OpenComponent<T>,
// AddAttribute and AddComponentParameter (name, value), AddMarkupContent for a static subtree,
// and RuntimeHelpers.TypeCheck<T> around component parameter values. Framework and FluentUI types
// sit under their real namespaces because the analyzer matches RenderTreeBuilder and ComponentBase
// by full name and names the component by its simple name.
#endregion

namespace TimeWarp.Architecture.Analyzers.Tests;

using Microsoft.CodeAnalysis.CSharp.Testing;

internal static class ComponentLinkTestFixture
{
  internal const string SpaGlobalConfig =
    """
    is_global = true
    build_property.UsingMicrosoftNETSdkBlazorWebAssembly = true
    """;

  internal const string Stubs =
    """
    namespace Microsoft.AspNetCore.Components
    {
      public abstract class ComponentBase
      {
        protected virtual void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder) { }
      }

      public sealed class NavLink : ComponentBase { }

      public readonly struct EventCallback { }
    }

    namespace Microsoft.AspNetCore.Components.Rendering
    {
      public sealed class RenderTreeBuilder
      {
        public void OpenElement(int sequence, string elementName) { }
        public void OpenComponent<TComponent>(int sequence) { }
        public void AddAttribute(int sequence, string name, string? value) { }
        public void AddAttribute(int sequence, string name, object? value) { }
        public void AddAttribute(int sequence, string name, Microsoft.AspNetCore.Components.EventCallback value) { }
        public void AddComponentParameter(int sequence, string name, object? value) { }
        public void AddMarkupContent(int sequence, string? markupContent) { }
        public void AddContent(int sequence, string? textContent) { }
        public void CloseElement() { }
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

    namespace Microsoft.FluentUI.AspNetCore.Components
    {
      public enum LinkTarget { Self, Blank }

      public sealed class FluentAnchorButton : Microsoft.AspNetCore.Components.ComponentBase
      {
        public string? Href { get; set; }
        public LinkTarget? Target { get; set; }
      }

      public sealed class FluentAnchor : Microsoft.AspNetCore.Components.ComponentBase
      {
        public string? Href { get; set; }
      }

      public sealed class FluentButton : Microsoft.AspNetCore.Components.ComponentBase
      {
        public string? Href { get; set; }
        public Microsoft.AspNetCore.Components.EventCallback OnClick { get; set; }
      }
    }

    namespace TimeWarp.Architecture.Attributes
    {
      [System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = false, Inherited = false)]
      public sealed class DirectComponentSideEffectAttribute : System.Attribute
      {
        public DirectComponentSideEffectAttribute(string reason) => Reason = reason;
        public string Reason { get; }
      }
    }
    """;

  internal static CSharpAnalyzerTest<ComponentLinkAnalyzer, RoslynTestVerifier> Test
  (
    string source,
    string sourcePath = "Page_razor.g.cs",
    string? globalConfig = SpaGlobalConfig
  )
  {
    CSharpAnalyzerTest<ComponentLinkAnalyzer, RoslynTestVerifier> test = new();
    if (globalConfig is not null)
    {
      test.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", globalConfig));
    }

    test.TestState.Sources.Add(("Stubs.cs", Stubs));
    test.TestState.Sources.Add((sourcePath, source));
    return test;
  }

  internal static DiagnosticResult Flag(int location, string subject) =>
    new DiagnosticResult(id: "TWA0050", DiagnosticSeverity.Error)
      .WithLocation(location)
      .WithArguments(subject);
}
