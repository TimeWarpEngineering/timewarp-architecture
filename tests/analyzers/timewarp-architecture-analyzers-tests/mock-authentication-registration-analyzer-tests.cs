#region Purpose
// TWA0021: mock auth DI types may only register inside MockAuthenticationRegistration.
#endregion

namespace TimeWarp.Architecture.Analyzers.Tests;

using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

public class Should_Restrict_Mock_Auth_Registration
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Should_Restrict_Mock_Auth_Registration>();

  private const string Stubs =
    """
    #region Purpose
    // Minimal DI stubs for TWA0021 tests — covers all three registration shapes the analyzer
    // must catch: generic type-argument, non-generic typeof(...), and factory-delegate (round-2
    // review, task 145-009 R2-2, proved the first version only checked the generic-argument shape).
    #endregion
    using System;
    namespace Microsoft.Extensions.DependencyInjection
    {
      public interface IServiceCollection { }
      public static class ServiceCollectionServiceExtensions
      {
        public static IServiceCollection AddScoped<TService, TImplementation>(this IServiceCollection services)
          where TImplementation : class, TService => services;

        public static IServiceCollection AddScoped(this IServiceCollection services, Type serviceType, Type implementationType) =>
          services;

        public static IServiceCollection AddScoped<TService>(this IServiceCollection services, Func<IServiceProvider, TService> factory) =>
          services;
      }
    }
    """;

  private static CSharpAnalyzerTest<MockAuthenticationRegistrationAnalyzer, RoslynTestVerifier> Test(string source) =>
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

  public static async Task Flag_AddScoped_Outside_Registration_Type()
  {
    const string Source =
      """
      #region Purpose
      // Bad call site.
      #endregion
      using Microsoft.Extensions.DependencyInjection;

      public class Program
      {
        public static void Configure(IServiceCollection services)
        {
          services.AddScoped<AuthState, MockAuthenticationStateProvider>();
        }
      }

      public class AuthState { }
      public class MockAuthenticationStateProvider : AuthState { }
      """;

    CSharpAnalyzerTest<MockAuthenticationRegistrationAnalyzer, RoslynTestVerifier> test = Test(Source);
    test.ExpectedDiagnostics.Add
    (
      DiagnosticResult.CompilerWarning("TWA0021")
        .WithSpan("Feature.cs", 10, 35, 10, 66)
        .WithArguments("MockAuthenticationStateProvider")
    );
    await test.RunAsync();
  }

  public static async Task Flag_AddScoped_NonGeneric_TypeOf_Evasion()
  {
    // Round-2 review (145-009 R2-2), empirically proven: the generic-only checker reported zero
    // diagnostics for this shape even though it registers the same mock type.
    const string Source =
      """
      #region Purpose
      // Bad call site (non-generic typeof(...) evasion).
      #endregion
      using System;
      using Microsoft.Extensions.DependencyInjection;

      public class Program
      {
        public static void Configure(IServiceCollection services)
        {
          services.AddScoped(typeof(AuthState), typeof(MockAuthenticationStateProvider));
        }
      }

      public class AuthState { }
      public class MockAuthenticationStateProvider : AuthState { }
      """;

    CSharpAnalyzerTest<MockAuthenticationRegistrationAnalyzer, RoslynTestVerifier> test = Test(Source);
    test.ExpectedDiagnostics.Add
    (
      DiagnosticResult.CompilerWarning("TWA0021")
        .WithSpan("Feature.cs", 11, 50, 11, 81)
        .WithArguments("MockAuthenticationStateProvider")
    );
    await test.RunAsync();
  }

  public static async Task Flag_AddScoped_FactoryDelegate_Evasion()
  {
    // Round-2 review (145-009 R2-2), empirically proven: a factory delegate that constructs the
    // mock type also reported zero diagnostics under the generic-only checker.
    const string Source =
      """
      #region Purpose
      // Bad call site (factory-delegate evasion).
      #endregion
      using System;
      using Microsoft.Extensions.DependencyInjection;

      public class Program
      {
        public static void Configure(IServiceCollection services)
        {
          services.AddScoped<IToken>(_ => new MockAccessTokenProvider());
        }
      }

      public interface IToken { }
      public class MockAccessTokenProvider : IToken { }
      """;

    CSharpAnalyzerTest<MockAuthenticationRegistrationAnalyzer, RoslynTestVerifier> test = Test(Source);
    test.ExpectedDiagnostics.Add
    (
      DiagnosticResult.CompilerWarning("TWA0021")
        .WithSpan("Feature.cs", 11, 37, 11, 66)
        .WithArguments("MockAccessTokenProvider")
    );
    await test.RunAsync();
  }

  public static async Task Allow_Registration_Inside_MockAuthenticationRegistration()
  {
    const string Source =
      """
      #region Purpose
      // Sole allowed registration site.
      #endregion
      using Microsoft.Extensions.DependencyInjection;

      public static class MockAuthenticationRegistration
      {
        public static void TryAdd(IServiceCollection services)
        {
          services.AddScoped<AuthState, MockAuthenticationStateProvider>();
          services.AddScoped<IToken, MockAccessTokenProvider>();
        }
      }

      public class AuthState { }
      public interface IToken { }
      public class MockAuthenticationStateProvider : AuthState { }
      public class MockAccessTokenProvider : IToken { }
      """;

    CSharpAnalyzerTest<MockAuthenticationRegistrationAnalyzer, RoslynTestVerifier> test = Test(Source);
    await test.RunAsync();
  }
}
