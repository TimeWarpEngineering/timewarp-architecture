namespace Microsoft.Extensions.DependencyInjection;

using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using TimeWarp.Architecture.Configuration;

public static partial class ServiceCollectionExtensions
{
  public static IServiceCollection ConfigureOptions<TOptions, TOptionsValidator>
  (
    this IServiceCollection aServiceCollection,
    IConfiguration aConfiguration
  )
    where TOptions : class
    where TOptionsValidator : AbstractValidator<TOptions>
  {
    Type type = typeof(TOptions);
    var sectionNameAttribute = (SectionNameAttribute?)type.GetCustomAttributes(typeof(SectionNameAttribute), false).FirstOrDefault();
    string sectionName = sectionNameAttribute?.SectionName ?? type.Name;
    IConfigurationSection configurationSection = aConfiguration.GetSection(sectionName);

    aServiceCollection.Configure<TOptions>(configurationSection);
    return RegisterOptionsValidator<TOptions, TOptionsValidator>(aServiceCollection);
  }

  public static IServiceCollection ConfigureOptions<TOptions, TOptionsValidator>(this IServiceCollection aServiceCollection, Action<TOptions> aOptionsAction)
        where TOptions : class
        where TOptionsValidator : AbstractValidator<TOptions>
  {
    aServiceCollection.Configure(aOptionsAction);
    return RegisterOptionsValidator<TOptions, TOptionsValidator>(aServiceCollection);
  }

  private static IServiceCollection RegisterOptionsValidator<TOptions, TOptionsValidator>(IServiceCollection aServiceCollection)
    where TOptions : class
    where TOptionsValidator : AbstractValidator<TOptions>
  {
    aServiceCollection.TryAddSingleton<TOptionsValidator>();

    aServiceCollection.TryAddEnumerable
    (
      ServiceDescriptor.Singleton<IValidateOptions<TOptions>,
      OptionsValidation<TOptions, TOptionsValidator>>()
    );

    return aServiceCollection;
  }
}
