namespace Microsoft.Extensions.DependencyInjection;

using FluentValidation;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Run Validation on all the IOptions that have validation
/// This will itterate through all the IConfigureOptions in the IServiceCollection
/// Then it will access each of those which will trigger the validation.
/// </summary>
public static partial class ServiceCollectionExtensions
{
  public static void ValidateOptions(this IServiceProvider aServiceProvider, IServiceCollection aServiceCollection)
  {
    IEnumerable<Type> optionTypes =
    aServiceCollection
      .Where
      (
        aServiceDescriptor =>
          aServiceDescriptor.ServiceType.IsGenericType &&
          aServiceDescriptor.ServiceType.GetGenericTypeDefinition() == typeof(IConfigureOptions<>)
      )
      .Select
      (
        aServiceDescriptor => aServiceDescriptor.ServiceType.GetGenericArguments()[0]
      ).Distinct();

    var originalDisplayNameResolver = ValidatorOptions.Global.DisplayNameResolver;

    ValidatorOptions.Global.DisplayNameResolver =
      (aType, aMemberInfo, aLambdaExpression) =>
        aType != null && aMemberInfo != null ? $"{aType.Name}:{aMemberInfo.Name}" : null;


    foreach (Type optionType in optionTypes)
    {
      Type optionsAccessorType = typeof(IOptions<>).MakeGenericType(new Type[] { optionType });
      object? optionsAccessor = aServiceProvider.GetService(optionsAccessorType);
      // Accessing the value triggers the validation.
      object? value = optionsAccessor?.GetType().GetProperty(nameof(IOptions<Object>.Value))?.GetValue(optionsAccessor);
    }

    ValidatorOptions.Global.DisplayNameResolver = originalDisplayNameResolver;
  }
}
