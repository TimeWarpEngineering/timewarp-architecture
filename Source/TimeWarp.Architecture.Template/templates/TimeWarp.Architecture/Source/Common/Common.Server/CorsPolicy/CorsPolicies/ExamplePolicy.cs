namespace TimeWarp.Architecture.CorsPolicies;

using Microsoft.Extensions.DependencyInjection;

public partial class CorsPolicy
{
  private class ExamplePolicy : CorsPolicy
  {
    public ExamplePolicy() : base(0, "Example.id") { }

    public override void Apply(IServiceCollection aServiceCollection)
    {
      aServiceCollection.AddCors
      (
        aOptions =>
        {
          aOptions.AddPolicy
          (
            CorsPolicy.Example.Name,
            aBuilder =>
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

              aBuilder
                .WithOrigins(allowedDomains)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
            }
          );
        }
      );
    }
  }
}
