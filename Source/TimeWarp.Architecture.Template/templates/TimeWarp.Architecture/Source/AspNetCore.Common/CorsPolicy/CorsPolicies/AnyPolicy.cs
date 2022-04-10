namespace TimeWarp.Architecture.CorsPolicies;

public partial class CorsPolicy
{
  /// <summary>
  /// 
  /// </summary>
  /// <example>
  /// `CorsPolicy.Any.Apply(aServiceCollection);`
  /// ...
  /// `aWebApplication.UseCors(CorsPolicy.Any.ToString());`
  /// </example>
  public class AnyPolicy : CorsPolicy
  {
    public AnyPolicy() : base(aValue: 0, aName: "Any") { }

    public override void Apply(IServiceCollection aServiceCollection)
    {
      aServiceCollection.AddCors
      (
        aOptions =>
          aOptions.AddPolicy
          (
            CorsPolicy.Any.Name,
            aBuilder => aBuilder
              .AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader()
          )
      );
    }
  }
}
