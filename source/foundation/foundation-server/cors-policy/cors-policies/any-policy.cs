#region Purpose
// CorsPolicy member that disables cross-origin restrictions entirely.
#endregion

#region Design
// Exists for development and same-trust-boundary deployments where origin lists add friction
// without security value. Deliberately omits AllowCredentials — the browser forbids combining
// wildcard origins with credentials; use ExamplePolicy's shape when credentials are needed.
// Apply(serviceCollection, exposedHeaders) is the gRPC-Web path: same wildcard policy plus
// WithExposedHeaders for Grpc-Status / Grpc-Message / Grpc-Encoding / Grpc-Accept-Encoding.
#endregion

namespace TimeWarp.Foundation.CorsPolicies;

public partial class CorsPolicy
{
  /// <summary>
  /// Permissive CORS policy (any origin/method/header) for development and same-trust boundaries.
  /// </summary>
  /// <example>
  /// `CorsPolicy.Any.Apply(serviceCollection);`
  /// ...
  /// `webApplication.UseCors(CorsPolicy.Any.ToString());`
  /// </example>
  public class AnyPolicy : CorsPolicy
  {
    /// <summary>
    /// Creates the permissive <c>Any</c> CORS policy member.
    /// </summary>
    public AnyPolicy() : base(value: 0, name: "Any") { }

    /// <summary>
    /// Registers an allow-any-origin/method/header CORS policy, optionally exposing response headers.
    /// </summary>
    public override void Apply(IServiceCollection serviceCollection, params string[] exposedHeaders)
    {
      serviceCollection.AddCors
      (
        options =>
          options.AddPolicy
          (
            CorsPolicy.Any.Name,
            builder =>
            {
              builder
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();

              if (exposedHeaders is { Length: > 0 })
              {
                builder.WithExposedHeaders(exposedHeaders);
              }
            }
          )
      );
    }
  }
}
