namespace TimeWarp.Architecture.CorsPolicies;


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

  private CorsPolicy(int aValue, string aName, List<string>? aAlternateCodes = null)
    : base(aValue, aName, aAlternateCodes) { }

  /// <summary>
  /// Apply the particular Cors Policy
  /// </summary>
  /// <remarks>Override in the instances of CorsPolicy</remarks>
  /// <param name="aServiceCollection"></param>
  public virtual void Apply(IServiceCollection aServiceCollection)
  {
    throw new InvalidOperationException();
    ;
  }
}
