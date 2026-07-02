#region Purpose
// CorsPolicy member that disables cross-origin restrictions entirely.
#endregion

#region Design
// Exists for development and same-trust-boundary deployments where origin lists add friction
// without security value. Deliberately omits AllowCredentials — the browser forbids combining
// wildcard origins with credentials; use ExamplePolicy's shape when credentials are needed.
#endregion

namespace TimeWarp.Foundation.CorsPolicies;

public partial class CorsPolicy
{
  /// <summary>
  /// 
  /// </summary>
  /// <example>
  /// `CorsPolicy.Any.Apply(serviceCollection);`
  /// ...
  /// `webApplication.UseCors(CorsPolicy.Any.ToString());`
  /// </example>
  public class AnyPolicy : CorsPolicy
  {
    public AnyPolicy() : base(value: 0, name: "Any") { }

    public override void Apply(IServiceCollection serviceCollection)
    {
      serviceCollection.AddCors
      (
        options =>
          options.AddPolicy
          (
            CorsPolicy.Any.Name,
            builder => builder
              .AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader()
          )
      );
    }
  }
}
