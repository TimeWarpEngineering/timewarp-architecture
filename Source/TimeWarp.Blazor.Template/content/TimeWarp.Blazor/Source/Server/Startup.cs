namespace TimeWarp.Blazor.Server
{
  using BooksApi.Models;
  using BooksApi.Services;
  using MediatR;
  using Microsoft.AspNetCore.Builder;
  using Microsoft.AspNetCore.Hosting;
  using Microsoft.AspNetCore.Mvc;
  using Microsoft.AspNetCore.ResponseCompression;
  using Microsoft.Extensions.Configuration;
  using Microsoft.Extensions.DependencyInjection;
  using Microsoft.Extensions.Hosting;
  using Microsoft.Extensions.Options;
  using System.Configuration;
  using System.Linq;
  using System.Net.Mime;
  using System.Reflection;

  public class Startup
  {
    public IConfiguration Configuration { get; }

    public Startup(IConfiguration aConfiguration)
    {
      Configuration = aConfiguration;
    }

    public void Configure
    (
      IApplicationBuilder aApplicationBuilder,
      IWebHostEnvironment aWebHostEnvironment
    )
    {
      aApplicationBuilder.UseResponseCompression();

      if (aWebHostEnvironment.IsDevelopment())
      {
        aApplicationBuilder.UseDeveloperExceptionPage();
        aApplicationBuilder.UseBlazorDebugging();
      }

      aApplicationBuilder.UseRouting();
      aApplicationBuilder.UseEndpoints
      (
        aEndpointRouteBuilder =>
        {
          aEndpointRouteBuilder.MapControllers();
          aEndpointRouteBuilder.MapBlazorHub();
          aEndpointRouteBuilder.MapFallbackToPage("/_Host");
        }
      );
      aApplicationBuilder.UseStaticFiles();
      aApplicationBuilder.UseClientSideBlazorFiles<Client.Program>();
    }

    public void ConfigureServices(IServiceCollection aServiceCollection)
    {
#if UseMongo
      aServiceCollection.Configure<BookstoreDatabaseSettings>(Configuration.GetSection(nameof(BookstoreDatabaseSettings)));

      aServiceCollection.AddSingleton<IBookstoreDatabaseSettings>
      (
        aServiceProvider => aServiceProvider.GetRequiredService<IOptions<BookstoreDatabaseSettings>>().Value
      );

      aServiceCollection.AddSingleton<BookService>();
#endif
      aServiceCollection.AddRazorPages();
      aServiceCollection.AddServerSideBlazor();
      aServiceCollection.AddMvc();
      aServiceCollection.Configure<ApiBehaviorOptions>
      (
        aApiBehaviorOptions =>
        {
          aApiBehaviorOptions.SuppressInferBindingSourcesForParameters = true;
        }
      );

      aServiceCollection.AddResponseCompression
      (
        aResponseCompressionOptions =>
          aResponseCompressionOptions.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat
          (
            new[] { MediaTypeNames.Application.Octet }
          )
      );

      Client.Program.ConfigureServices(aServiceCollection);

      aServiceCollection.AddMediatR(typeof(Startup).GetTypeInfo().Assembly);

      aServiceCollection.Scan
      (
        aTypeSourceSelector => aTypeSourceSelector
          .FromAssemblyOf<Startup>()
          .AddClasses()
          .AsSelf()
          .WithScopedLifetime()
      );
    }
  }
}
