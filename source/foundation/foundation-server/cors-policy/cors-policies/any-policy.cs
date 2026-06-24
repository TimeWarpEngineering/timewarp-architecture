namespace TimeWarp.Architecture.CorsPolicies;

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
