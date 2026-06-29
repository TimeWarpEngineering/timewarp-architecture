namespace TimeWarp.Foundation.CorsPolicies;

/// <summary>
/// This is the enumeration object for CORS Policies
/// </summary>
public partial class CorsPolicy : Enumeration
{
  /// <summary>
  /// Allows for Any Origin
  /// </summary>
  public static readonly CorsPolicy Any = new AnyPolicy();

  /// <summary>
  /// Allows for https://*.Example.id"
  /// </summary>
  public static readonly CorsPolicy Example = new ExamplePolicy();

  private CorsPolicy(int value, string name, List<string>? alternateCodes = null)
    : base(value, name, alternateCodes) { }

  /// <summary>
  /// Apply the particular Cors Policy
  /// </summary>
  /// <remarks>Override in the instances of CorsPolicy</remarks>
  /// <param name="serviceCollection"></param>
  public virtual void Apply(IServiceCollection serviceCollection)
  {
    throw new InvalidOperationException();
  }
}
