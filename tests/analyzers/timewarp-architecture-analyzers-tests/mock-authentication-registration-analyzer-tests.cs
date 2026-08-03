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
    // Minimal DI stubs for TWA0021 tests.
    #endregion
    namespace Microsoft.Extensions.DependencyInjection
    {
      public interface IServiceCollection { }
      public static class ServiceCollectionServiceExtensions
      {
        public static IServiceCollection AddScoped<TService, TImplementation>(this IServiceCollection services)
          where TImplementation : class, TService => services;
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
