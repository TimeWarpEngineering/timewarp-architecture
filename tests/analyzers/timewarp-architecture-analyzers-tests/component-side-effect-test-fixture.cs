#region Purpose
// Shared harness (stubs, config, Test, Flag, and EmptyReason builders) for the TWA0026/TWA0027 tests.
#endregion

namespace TimeWarp.Architecture.Analyzers.Tests;

using Microsoft.CodeAnalysis.CSharp.Testing;

internal static class ComponentSideEffectTestFixture
{
  internal const string SpaGlobalConfig =
    """
    is_global = true
    build_property.UsingMicrosoftNETSdkBlazorWebAssembly = true
    """;

  internal const string Stubs =
    """
    using System.Threading;
    using System.Threading.Tasks;

    namespace Microsoft.AspNetCore.Components
    {
      public abstract class ComponentBase
      {
        protected virtual void OnInitialized() { }
        protected virtual Task OnInitializedAsync() => Task.CompletedTask;
        protected virtual Task OnAfterRenderAsync(bool firstRender) => Task.CompletedTask;
      }

      public abstract class NavigationManager
      {
        public string Uri => "";
        public void NavigateTo(string uri, bool forceLoad = false) { }
        public void Refresh(bool forceReload = false) { }
        public string ToBaseRelativePath(string uri) => uri;
      }
    }

    namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication
    {
      public static class NavigationManagerExtensions
      {
        public static void NavigateToLogin(this Microsoft.AspNetCore.Components.NavigationManager manager, string loginPath) { }
      }
    }

    namespace Microsoft.JSInterop
    {
      public interface IJSRuntime
      {
        ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args);
      }

      public interface IJSObjectReference : System.IAsyncDisposable
      {
        ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args);
      }

      public static class JSRuntimeExtensions
      {
        public static ValueTask InvokeVoidAsync(this IJSRuntime jsRuntime, string identifier, params object[] args) => default;
      }

      public static class JSObjectReferenceExtensions
      {
        public static ValueTask InvokeVoidAsync(this IJSObjectReference reference, string identifier, params object[] args) => default;
      }
    }

    namespace TimeWarp.Foundation
    {
      public interface IApiService
      {
        Task<object> GetResponse<TResponse>(object request, CancellationToken cancellationToken);
      }
    }

    namespace App.Services
    {
      public interface IWebServerApiService : TimeWarp.Foundation.IApiService { }
    }

    namespace Blazored.SessionStorage
    {
      public interface ISessionStorageService
      {
        ValueTask SetItemAsync<T>(string key, T data, CancellationToken cancellationToken = default);
        ValueTask RemoveItemAsync(string key, CancellationToken cancellationToken = default);
        ValueTask ClearAsync(CancellationToken cancellationToken = default);
        ValueTask<T> GetItemAsync<T>(string key, CancellationToken cancellationToken = default);
        ValueTask<bool> ContainKeyAsync(string key, CancellationToken cancellationToken = default);
      }
    }

    namespace Blazored.LocalStorage
    {
      public interface ILocalStorageService
      {
        ValueTask SetItemAsStringAsync(string key, string data, CancellationToken cancellationToken = default);
        ValueTask RemoveItemAsync(string key, CancellationToken cancellationToken = default);
        ValueTask<string> GetItemAsStringAsync(string key, CancellationToken cancellationToken = default);
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

      [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Interface)]
      public sealed class SideEffectServiceAttribute : System.Attribute { }
    }

    namespace App.Services
    {
      [TimeWarp.Architecture.Attributes.SideEffectService]
      public sealed class PasskeyCeremonyClient
      {
        public Task<bool> AuthenticateAsync(CancellationToken cancellationToken) => Task.FromResult(true);
      }

      public sealed class PlainHelper
      {
        public string Format(string value) => value;
      }
    }
    """;

  internal static CSharpAnalyzerTest<ComponentSideEffectAnalyzer, RoslynTestVerifier> Test
  (
    string source,
    string sourcePath = "Feature.cs",
    string? globalConfig = SpaGlobalConfig
  )
  {
    CSharpAnalyzerTest<ComponentSideEffectAnalyzer, RoslynTestVerifier> test = new();
    if (globalConfig is not null)
    {
      test.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", globalConfig));
    }

    test.TestState.Sources.Add(("Stubs.cs", Stubs));
    test.TestState.Sources.Add((sourcePath, source));
    return test;
  }

  internal static DiagnosticResult Flag(int location, string member) =>
    new DiagnosticResult(id: "TWA0026", DiagnosticSeverity.Warning)
      .WithLocation(location)
      .WithArguments(member);

  internal static DiagnosticResult EmptyReason(int location, string symbol) =>
    new DiagnosticResult(id: "TWA0027", DiagnosticSeverity.Warning)
      .WithLocation(location)
      .WithArguments(symbol);
}
