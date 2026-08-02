#region Purpose
// Shared SPA test-host surface: ServiceProvider for TimeWarp.State / mediator scopes.
// Implemented by suite-local AspireSpaTestApplication (closed-box Aspire ingress).
#endregion

namespace TimeWarp.Architecture.Testing;

public interface ISpaTestApplication
{
  IServiceProvider ServiceProvider { get; }
}
