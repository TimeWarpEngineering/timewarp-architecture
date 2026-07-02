#region Purpose
// Template exemplar of a restricted-origin CORS policy for template consumers to copy and adapt.
#endregion

#region Design
// Shows the shape a production policy needs: explicit origin allowlist per environment plus
// AllowCredentials (which legally requires named origins, unlike AnyPolicy).
// The class is private — consumers reach it only through CorsPolicy.Example — so the policy
// surface stays the enumeration, not a zoo of public subclasses.
// Origins are placeholders; replace them when instantiating the template.
#endregion

namespace TimeWarp.Foundation.CorsPolicies;
public partial class CorsPolicy
{
  private class ExamplePolicy : CorsPolicy
  {
    public ExamplePolicy() : base(0, "Example.id") { }

    public override void Apply(IServiceCollection serviceCollection)
    {
      serviceCollection.AddCors
      (
        options =>
          options.AddPolicy
          (
            CorsPolicy.Example.Name,
            builder =>
            {
              // #TODO add all of your domains we are using localhost here 
              string[] allowedDomains = new[]
              {
                // Development 
                "https://localhost:5060", // Example.Studio.Server
                "http://localhost:5061", // Example.Api.Server

                // Staging
                "https://example.azurewebsites.net",

                // Production
                "https://YourApp.Example.com"
              };

              builder
                .WithOrigins(allowedDomains)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
            }
          )
      );
    }
  }
}
